using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RestaurantManagementAPI.Services.Implements
{
    public class TcpSocketServer : BackgroundService
    {
        public static TcpSocketServer Instance { get; private set; }

        private TcpListener _listener;
        private readonly List<TcpClient> _clients = new List<TcpClient>();

        public TcpSocketServer()
        {
            Instance = this;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _listener = new TcpListener(IPAddress.Any, 9000);
                _listener.Start();
                Console.WriteLine("SOCKET SERVER STARTED ON PORT 9000");

                while (!stoppingToken.IsCancellationRequested)
                {
                    var client = await _listener.AcceptTcpClientAsync(stoppingToken);
                    lock (_clients) { _clients.Add(client); }
                    _ = HandleClientAsync(client);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Socket Error: {ex.Message}");
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            var buffer = new byte[4096];
            var stream = client.GetStream();

            try
            {
                while (client.Connected)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    if (message.StartsWith("CHAT|"))
                    {
                        await BroadcastAsync(message);
                    }
                }
            }
            catch { }
            finally
            {
                lock (_clients) { _clients.Remove(client); }
                client.Close();
            }
        }
        public async Task BroadcastAsync(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            if (!message.EndsWith("\n"))
            {
                message += "\n";
            }

            byte[] data = Encoding.UTF8.GetBytes(message);
            List<TcpClient> clientsCopy;

            lock (_clients) { clientsCopy = new List<TcpClient>(_clients); }

            foreach (var client in clientsCopy)
            {
                if (client.Connected)
                {
                    try
                    {
                        await client.GetStream().WriteAsync(data, 0, data.Length);
                        await client.GetStream().FlushAsync();
                    }
                    catch { }
                }
            }
        }
    }
}