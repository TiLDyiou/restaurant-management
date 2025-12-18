using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using RestaurantManagementAPI.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RestaurantManagementAPI.Infrastructure.Sockets
{
    public class TcpSocketServer : BackgroundService
    {
        public static TcpSocketServer Instance { get; private set; }
        private TcpListener _listener;
        private readonly int _port = 9000;
        private ConcurrentDictionary<string, TcpClient> _clients = new();
        private readonly IServiceProvider _serviceProvider;

        public TcpSocketServer(IServiceProvider serviceProvider)
        {
            Instance = this;
            _serviceProvider = serviceProvider;
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
                Console.WriteLine($"[SOCKET] Server started on port {_port}");

                while (!stoppingToken.IsCancellationRequested)
                {
                    var client = await _listener.AcceptTcpClientAsync(stoppingToken);
                    _ = HandleClientAsync(client, stoppingToken);
                }
            }
            catch (Exception ex) 
            { 
                Console.WriteLine($"[SOCKET ERROR] {ex.Message}"); 
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            string maNV = "";
            NetworkStream stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);

            try
            {
                while (client.Connected && !token.IsCancellationRequested)
                {
                    string message = await reader.ReadLineAsync();
                    if (message == null) 
                        break;

                    Console.WriteLine($"[RECV] {message}");

                    if (message.StartsWith("LOGIN|"))
                    {
                        var parts = message.Split('|');
                        if (parts.Length > 1)
                        {
                            maNV = parts[1].Trim();
                            _clients.AddOrUpdate(maNV, client, (k, v) => client);
                            Console.WriteLine($"-> User {maNV} Connected");
                            await UpdateUserStatusInDb(maNV, true);
                            await BroadcastAsync($"STATUS|{maNV}|TRUE");
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
            catch { }
            finally
            {
                if (!string.IsNullOrEmpty(maNV))
                {
                    _clients.TryRemove(maNV, out _);
                    Console.WriteLine($"-> User {maNV} Disconnected");
                    await UpdateUserStatusInDb(maNV, false);
                    await BroadcastAsync($"STATUS|{maNV}|FALSE");
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
                Console.WriteLine($"DB Error: {ex.Message}"); 
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
                    catch { }
                }
            }
        }
    }
}