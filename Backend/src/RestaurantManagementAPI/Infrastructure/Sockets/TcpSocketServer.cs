using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RestaurantManagementAPI.Data;
using RestaurantManagementAPI.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RestaurantManagementAPI.Infrastructure.Sockets
{
    public class TcpSocketServer : BackgroundService // Inherit from BackgroundService for background execution
    {
        public static TcpSocketServer? Instance { get; private set; }
        private TcpListener? _listener;
        private readonly int _port = 9000;
        private ConcurrentDictionary<string, TcpClient> _clients = new();
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TcpSocketServer> _logger;
        private readonly IRealtimeNotifier _notifier;

        public TcpSocketServer(IServiceProvider serviceProvider, ILogger<TcpSocketServer> logger, IRealtimeNotifier notifier)
        {
            Instance = this;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _notifier = notifier;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<QLNHDbContext>();
                    var users = await context.TAIKHOAN.Where(u => u.Online).ToListAsync();
                    users.ForEach(u => u.Online = false);
                    await context.SaveChangesAsync();
                }

                _listener = new TcpListener(IPAddress.Any, _port);
                _listener.Start();
                _logger.LogInformation("Server started on port {Port}", _port);

                while (!stoppingToken.IsCancellationRequested)
                {
                    var client = await _listener.AcceptTcpClientAsync(stoppingToken);
                    _ = HandleClientAsync(client, stoppingToken); // Fire-and-forget: run concurrently without blocking acceptance of other clients
                }
            }
            catch (Exception ex) 
            { 
                _logger.LogError(ex, "Socket server execution error"); 
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            string maNV = "";
            NetworkStream stream = client.GetStream(); // Retrieve client data stream
            using var reader = new StreamReader(stream, Encoding.UTF8);

            try
            {
                while (client.Connected && !token.IsCancellationRequested)
                {
                    string message = await reader.ReadLineAsync();
                    if (message == null) 
                        break;

                    _logger.LogInformation("Received message: {Message}", message);

                    if (message.StartsWith("LOGIN|")) // Format: LOGIN|MaNV
                    {
                        var parts = message.Split('|');
                        if (parts.Length > 1)
                        {
                            maNV = parts[1].Trim();
                            _clients.AddOrUpdate(maNV, client, (k, v) => client);
                            _logger.LogInformation("User {MaNV} Connected", maNV);
                            await UpdateUserStatusInDb(maNV, true); // Update user online status in database
                            await _notifier.NotifyUserStatusChangedAsync(maNV, true);
                        }
                    }
                    else if (message.StartsWith("LOGOUT"))
                    {
                        break;
                    }
                    else if (message.StartsWith("ORDER") || message.StartsWith("TABLE") || message.StartsWith("KITCHEN"))
                    {
                        await BroadcastAsync(message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling client");
            }
            finally
            {
                if (!string.IsNullOrEmpty(maNV))
                {
                    _clients.TryRemove(maNV, out _);
                    _logger.LogInformation("User {MaNV} Disconnected", maNV);
                    await UpdateUserStatusInDb(maNV, false);
                    await _notifier.NotifyUserStatusChangedAsync(maNV, false);
                }
                client.Close();
            }
        }

        private async Task UpdateUserStatusInDb(string maNV, bool isOnline)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<QLNHDbContext>();
                var user = await context.TAIKHOAN.FirstOrDefaultAsync(u => u.MaNV == maNV);
                if (user != null)
                {
                    user.Online = isOnline;
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            { 
                _logger.LogError(ex, "DB Error updating status for user {MaNV}", maNV); 
            }
        }

        public async Task BroadcastAsync(string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message + "\n");

            foreach (var client in _clients.Values)
            {
                if (client.Connected)
                {
                    try
                    {
                        _ = client.GetStream().WriteAsync(data);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error broadcasting message to client");
                    }
                }
            }
        }
    }
}