using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection; // Cần thiết để gọi Database
using RestaurantManagementAPI.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RestaurantManagementAPI.Services.Implements
{
    public class TcpSocketServer : BackgroundService
    {
        // Biến Instance để các Service khác (Order/Table) gọi được, tránh lỗi CS0117
        public static TcpSocketServer Instance { get; private set; }

        private TcpListener _listener;
        private readonly int _port = 9000;
        private ConcurrentDictionary<string, TcpClient> _clients = new();

        // Dùng để truy cập Database trong Background Service
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
                // --- 1. DỌN DẸP DỮ LIỆU CŨ KHI SERVER VỪA CHẠY ---
                // Mục đích: Reset tất cả ai đang bị kẹt 'Online' về 'Offline' hết
                await ResetAllUsersToOffline();
                // -------------------------------------------------

                _listener = new TcpListener(IPAddress.Any, _port);
                _listener.Start();
                Console.WriteLine($"[TCP SERVER] Đã khởi động tại Port {_port}");

                while (!stoppingToken.IsCancellationRequested)
                {
                    var client = await _listener.AcceptTcpClientAsync(stoppingToken);
                    _ = HandleClientAsync(client, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TCP ERROR] {ex.Message}");
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            string maNV = "";
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];

            try
            {
                while (client.Connected && !token.IsCancellationRequested)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                    if (bytesRead == 0) break; // Client ngắt kết nối

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

                    // Xử lý đăng nhập TCP
                    if (message.StartsWith("LOGIN|"))
                    {
                        var parts = message.Split('|');
                        if (parts.Length > 1)
                        {
                            maNV = parts[1];
                            _clients.AddOrUpdate(maNV, client, (k, v) => client);

                            Console.WriteLine($"-> User {maNV} đã kết nối (Online)");

                            // Báo cho mọi người biết
                            await BroadcastAsync($"STATUS|{maNV}|TRUE");

                            // Cập nhật Database: Online = true
                            await UpdateUserStatusInDb(maNV, true);
                        }
                    }
                }
            }
            catch
            {
                // Lỗi kết nối ngầm thì bỏ qua, xuống finally xử lý
            }
            finally
            {
                // --- 2. XỬ LÝ KHI MẤT KẾT NỐI / TẮT APP ---
                if (!string.IsNullOrEmpty(maNV))
                {
                    _clients.TryRemove(maNV, out _);
                    Console.WriteLine($"<- User {maNV} đã ngắt kết nối (Offline)");

                    // Báo cho App biết để đổi màu xám
                    _ = BroadcastAsync($"STATUS|{maNV}|FALSE");

                    // QUAN TRỌNG: Cập nhật Database về Offline ngay lập tức
                    await UpdateUserStatusInDb(maNV, false);
                }
                client.Close();
            }
        }

        // Hàm cập nhật trạng thái vào Database
        private async Task UpdateUserStatusInDb(string maNV, bool isOnline)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<QLNHDbContext>();
                    var user = await context.TAIKHOAN.FirstOrDefaultAsync(u => u.MaNV == maNV);
                    if (user != null)
                    {
                        user.Online = isOnline;
                        await context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB ERROR] Không thể update trạng thái: {ex.Message}");
            }
        }

        // Hàm Reset toàn bộ user về Offline (Chạy 1 lần lúc mở Server)
        private async Task ResetAllUsersToOffline()
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<QLNHDbContext>();
                    // Lấy tất cả user đang Online
                    var onlineUsers = await context.TAIKHOAN.Where(u => u.Online == true).ToListAsync();

                    if (onlineUsers.Count > 0)
                    {
                        foreach (var user in onlineUsers)
                        {
                            user.Online = false;
                        }
                        await context.SaveChangesAsync();
                        Console.WriteLine($"[CLEANUP] Đã reset {onlineUsers.Count} user bị kẹt về Offline.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CLEANUP ERROR] {ex.Message}");
            }
        }

        public async Task BroadcastAsync(string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            foreach (var client in _clients.Values)
            {
                if (client.Connected)
                {
                    try { await client.GetStream().WriteAsync(data); } catch { }
                }
            }
        }
    }
}