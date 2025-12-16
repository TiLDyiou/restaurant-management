using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using Microsoft.Maui.Storage;

namespace RestaurantManagementGUI.Services
{
    public class SocketListener
    {
        private static SocketListener _instance;
        public static SocketListener Instance => _instance ??= new SocketListener();

        private TcpClient _client;
        private NetworkStream _stream;
        private bool _isConnected;
        private CancellationTokenSource _cts;

        // Giữ nguyên các Event cũ của bạn
        public event Action<string> OnNewOrderReceived;
        public event Action<string> OnTableStatusChanged;
        public event Action<string> OnChatReceived;
        public event Action<string> OnDishDone;

#if ANDROID
        private const string SERVER_IP = "10.0.2.2";
#else
        private const string SERVER_IP = "localhost"; 
#endif
        private const int SERVER_PORT = 9000;

        // --- HÀM KẾT NỐI ---
        public async Task ConnectAsync()
        {
            if (_isConnected) return;
            _cts = new CancellationTokenSource();
            _ = Task.Run(() => ConnectLoop(_cts.Token));
        }

        private async Task ConnectLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (_client == null || !_client.Connected)
                    {
                        Debug.WriteLine($"[SOCKET] Đang kết nối tới {SERVER_IP}:{SERVER_PORT}...");
                        _client = new TcpClient();
                        await _client.ConnectAsync(SERVER_IP, SERVER_PORT);

                        _stream = _client.GetStream();
                        _isConnected = true;

                        // --- 1. GỬI LỆNH LOGIN NGAY KHI KẾT NỐI (QUAN TRỌNG) ---
                        // Phải gửi cái này thì Server mới biết bạn là ai để mà báo Offline khi bạn thoát
                        string maNV = await SecureStorage.GetAsync("current_ma_nv");
                        if (!string.IsNullOrEmpty(maNV))
                        {
                            byte[] loginData = Encoding.UTF8.GetBytes($"LOGIN|{maNV}");
                            await _stream.WriteAsync(loginData, 0, loginData.Length);
                            Debug.WriteLine($"[SOCKET] Đã gửi Login: {maNV}");
                        }
                        // --------------------------------------------------------

                        Debug.WriteLine("KẾT NỐI THÀNH CÔNG!");
                        await ListenLoop();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Lỗi kết nối: {ex.Message}. Thử lại sau 3s...");
                    _isConnected = false;
                }
                await Task.Delay(3000, token);
            }
        }

        // --- HÀM LẮNG NGHE (Dùng ReadAsync để khớp với Server) ---
        private async Task ListenLoop()
        {
            byte[] buffer = new byte[1024];
            try
            {
                while (_isConnected && _client.Connected)
                {
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

                    // --- 2. BẮN TIN NHẮN STATUS QUA MESSAGING CENTER ---
                    if (message.StartsWith("STATUS|"))
                    {
                        MessagingCenter.Send(this, "UpdateStatus", message);
                    }
                    // Các xử lý khác giữ nguyên
                    else if (message.StartsWith("ORDER|"))
                    {
                        var parts = message.Split('|', 2);
                        if (parts.Length > 1) OnNewOrderReceived?.Invoke(parts[1]);
                    }
                    else if (message.StartsWith("TABLE|"))
                    {
                        var parts = message.Split('|', 2);
                        if (parts.Length > 1) OnTableStatusChanged?.Invoke(parts[1]);
                    }
                    else if (message.StartsWith("CHAT|"))
                    {
                        var parts = message.Split('|', 2);
                        if (parts.Length > 1) OnChatReceived?.Invoke(parts[1]);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ngắt kết nối: {ex.Message}");
                _isConnected = false;
                _client?.Close();
            }
        }

        public async Task SendChatAsync(string message)
        {
            if (_isConnected && _stream != null)
            {
                byte[] data = Encoding.UTF8.GetBytes($"CHAT|{message}");
                await _stream.WriteAsync(data, 0, data.Length);
            }
        }

        // --- HÀM NGẮT KẾT NỐI (Gửi LOGOUT trước khi đóng) ---
        public async void Disconnect()
        {
            _cts?.Cancel();
            _isConnected = false;

            try
            {
                // Gửi lời chào tạm biệt server để nó update DB ngay
                if (_stream != null && _client.Connected)
                {
                    byte[] data = Encoding.UTF8.GetBytes("LOGOUT");
                    await _stream.WriteAsync(data, 0, data.Length);
                }
            }
            catch { }

            _client?.Close();
            _client = null;
            Debug.WriteLine("Đã ngắt kết nối thủ công.");
        }
    }
}