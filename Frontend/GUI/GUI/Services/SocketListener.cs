using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.Maui.Storage;
using RestaurantManagementGUI.Models;

namespace RestaurantManagementGUI.Services
{
    public class SocketListener
    {
        private static SocketListener _instance;
        public static SocketListener Instance => _instance ??= new SocketListener();

        private TcpClient _client;
        private StreamReader _reader;
        private StreamWriter _writer;
        private bool _isConnected;
        private CancellationTokenSource _cts;

        // Events
        public event Action<string> OnNewOrderReceived;
        public event Action<string> OnTableStatusChanged;
        public event Action<string> OnDishDone;
        public event Action<string> OnChatReceived;

#if ANDROID
        private const string SERVER_IP = "10.0.2.2";
#else
        private const string SERVER_IP = "localhost"; 
#endif
        private const int SERVER_PORT = 9000;

        public async Task ConnectAsync()
        {
            if (_isConnected) return;
            _cts = new CancellationTokenSource();
            _ = Task.Run(() => ConnectLoop(_cts.Token));
        }

        // Thay thế hàm LoginAsync cũ bằng hàm này:
        public async Task LoginAsync(string maNV)
        {
            // 1. Nếu chưa kết nối thì kích hoạt
            if (!_isConnected || _client == null || !_client.Connected)
            {
                await ConnectAsync();
            }

            // 2. QUAN TRỌNG: Vòng lặp chờ kết nối sẵn sàng (Timeout 5 giây)
            // Giúp đảm bảo không gửi tin khi Socket chưa handshake xong
            int retry = 0;
            while (!_isConnected && retry < 50)
            {
                await Task.Delay(100); // Chờ 100ms mỗi lần
                retry++;
            }

            // 3. Chỉ gửi khi đã kết nối thành công
            if (_isConnected)
            {
                await SendMessageAsync($"LOGIN|{maNV}");
                System.Diagnostics.Debug.WriteLine($"[SOCKET] Đã gửi LOGIN cho {maNV}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[SOCKET] Không thể kết nối để gửi Login.");
            }
        }
        private async Task ConnectLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (_client == null || !_client.Connected)
                    {
                        _client = new TcpClient();
                        await _client.ConnectAsync(SERVER_IP, SERVER_PORT);

                        var stream = _client.GetStream();
                        _reader = new StreamReader(stream, Encoding.UTF8);
                        _writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
                        _isConnected = true;
                        string maNV = await SecureStorage.GetAsync("current_ma_nv");
                        if (!string.IsNullOrEmpty(maNV))
                        {
                            await SendMessageAsync($"LOGIN|{maNV}");
                        }
                        await ListenLoop();
                    }
                }
                catch (Exception ex)
                {
                    _isConnected = false;
                }
                await Task.Delay(3000, token);
            }
        }

        private async Task ListenLoop()
        {
            try
            {
                while (_isConnected && _client.Connected)
                {
                    // Đọc từng dòng (Backend phải gửi kèm \n cuối mỗi tin)
                    string message = await _reader.ReadLineAsync();
                    if (message == null) break;

                    Debug.WriteLine($"[SOCKET RECV]: {message}");
                    ProcessMessage(message);
                }
            }
            catch
            {
                _isConnected = false;
                _client?.Close();
            }
        }

        private void ProcessMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            var parts = message.Split('|', 2);
            string header = parts[0];
            string content = parts.Length > 1 ? parts[1] : "";

            switch (header)
            {
                case "ORDER":
                    OnNewOrderReceived?.Invoke(content);
                    break;

                case "TABLE":
                    // Content là JSON: {"MaBan":"B01", "TrangThai":"Có khách"}
                    // Bắn nguyên chuỗi JSON ra cho ViewModel xử lý
                    OnTableStatusChanged?.Invoke(content);
                    break;

                case "KITCHEN_DONE":
                    OnDishDone?.Invoke(content);
                    break;

                case "CHAT":
                    OnChatReceived?.Invoke(content);
                    break;

                // Trường hợp STATUS (User Online/Offline) - UserPage sẽ hứng
                case "STATUS":
                    // Format: STATUS|NV001|TRUE -> Gửi nguyên message để UserPage tự parse
                    MessagingCenter.Send(this, "UpdateStatus", message);
                    break;
            }
        }

        public async Task SendChatAsync(string message)
        {
            await SendMessageAsync($"CHAT|{message}");
        }

        public async Task DisconnectAsync()
        {
            _cts?.Cancel();
            _isConnected = false;

            try
            {
                // Gửi tin nhắn báo server biết mình out
                // (Hàm SendMessageAsync này bạn đã thêm ở bước trước khi mình gửi code SocketListener mới)
                await SendMessageAsync("LOGOUT");

                // QUAN TRỌNG: Chờ 0.2 giây để tin nhắn kịp đi qua đường truyền trước khi cắt dây
                await Task.Delay(200);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi gửi Logout: {ex.Message}");
            }
            finally
            {
                // Đóng sạch sẽ các tài nguyên
                try
                {
                    _reader?.Close();
                    _writer?.Close();
                    _client?.Close();
                }
                catch { }

                _client = null;
                System.Diagnostics.Debug.WriteLine("Đã ngắt kết nối Socket an toàn.");
            }
        }

        private async Task SendMessageAsync(string msg)
        {
            if (_isConnected && _writer != null)
            {
                await _writer.WriteLineAsync(msg);
            }
        }
    }
}