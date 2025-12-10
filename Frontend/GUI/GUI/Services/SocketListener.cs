using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

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

        // Sự kiện bắn ra UI
        public event Action<string> OnNewOrderReceived;
        public event Action<string> OnTableStatusChanged;
        public event Action<string> OnChatReceived;

        // Cấu hình IP
#if ANDROID
        private const string SERVER_IP = "10.0.2.2";
#else
        private const string SERVER_IP = "127.0.0.1";
#endif
        private const int SERVER_PORT = 9000;

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

                        var stream = _client.GetStream();
                        _reader = new StreamReader(stream, Encoding.UTF8);
                        _writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
                        _isConnected = true;

                        Debug.WriteLine("✅ [SOCKET] KẾT NỐI THÀNH CÔNG!");

                        // Bắt đầu lắng nghe ngay khi kết nối được
                        await ListenLoop();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"❌ [SOCKET] Lỗi kết nối: {ex.Message}. Thử lại sau 3s...");
                    _isConnected = false;
                }

                // Nếu mất kết nối, chờ 3 giây rồi thử lại
                await Task.Delay(3000, token);
            }
        }

        private async Task ListenLoop()
        {
            try
            {
                while (_isConnected && _client.Connected)
                {
                    string line = await _reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    Debug.WriteLine($"📩 [SOCKET NHẬN]: {line}"); // In ra Output để kiểm tra

                    var parts = line.Split('|', 2);
                    if (parts.Length < 2) continue;

                    string type = parts[0];
                    string json = parts[1];

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (type == "ORDER") OnNewOrderReceived?.Invoke(json);
                        else if (type == "TABLE") OnTableStatusChanged?.Invoke(json);
                        else if (type == "CHAT") OnChatReceived?.Invoke(json);
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"⚠️ [SOCKET] Ngắt kết nối: {ex.Message}");
                _isConnected = false;
                _client?.Close();
            }
        }

        public async Task SendChatAsync(string message)
        {
            if (_isConnected && _writer != null)
            {
                await _writer.WriteLineAsync($"CHAT|{message}");
            }
        }

        public void Disconnect()
        {
            _cts?.Cancel();
            _isConnected = false;
            _client?.Close();
            _client = null;
            Debug.WriteLine("🛑 [SOCKET] Đã ngắt kết nối thủ công.");
        }
    }
}