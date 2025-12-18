using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.Maui.Storage;
using RestaurantManagementGUI.Models;

namespace RestaurantManagementGUI.Services
{
    public class TCPSocketClient
    {
        private static TCPSocketClient _instance;
        public static TCPSocketClient Instance => _instance ??= new TCPSocketClient();

        private TcpClient _client;
        private StreamReader _reader;
        private StreamWriter _writer;
        private bool _isConnected;
        private CancellationTokenSource _cts;

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

        public async Task LoginAsync(string maNV)
        {
            if (!_isConnected || _client == null || !_client.Connected)
            {
                await ConnectAsync();
            }

            int retry = 0;
            while (!_isConnected && retry < 50)
            {
                await Task.Delay(100);
                retry++;
            }

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
                    OnTableStatusChanged?.Invoke(content);
                    break;

                case "KITCHEN_DONE":
                    OnDishDone?.Invoke(content);
                    break;

                case "CHAT":
                    OnChatReceived?.Invoke(content);
                    break;
                case "STATUS":
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
                await SendMessageAsync("LOGOUT");
                await Task.Delay(200);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi gửi Logout: {ex.Message}");
            }
            finally
            {
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