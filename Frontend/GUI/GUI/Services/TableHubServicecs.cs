// Trong file Services/TableHubService.cs
using Microsoft.AspNetCore.SignalR.Client;
using System.Diagnostics;
using System.Net.Security;

namespace RestaurantManagementGUI.Services
{
    // ... (class TableStatusUpdateDto giữ nguyên) ...
    public class TableStatusUpdateDto
    {
        public string MaBan { get; set; }
        public string TrangThai { get; set; }
    }

    public class TableHubService
    {
        private HubConnection _hubConnection;
        public event Action<TableStatusUpdateDto> OnTableStatusChanged;

        // Tự động chọn URL dựa trên nền tảng
        private string GetHubUrl()
        {
            
            string port = "7004";

            // Nếu là Android Emulator
            if (DeviceInfo.Platform == DevicePlatform.Android)
                return $"https://10.0.2.2:{port}/banHub";

            // Nếu là Windows, iOS, Mac
            return $"https://localhost:{port}/banHub";
        }

        public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

        public async Task InitAsync()
        {
            if (_hubConnection != null && _hubConnection.State != HubConnectionState.Disconnected)
            {
                return;
            }

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(GetHubUrl(), options =>
                {
                    // Bỏ qua kiểm tra certificate CHỈ KHI DEBUG
#if DEBUG
                    options.HttpMessageHandlerFactory = (handler) =>
                    {
                        if (handler is HttpClientHandler clientHandler)
                        {
                            // Bỏ qua kiểm tra SSL certificate
                            clientHandler.ServerCertificateCustomValidationCallback =
                                (sender, certificate, chain, sslPolicyErrors) => true;
                        }
                        else if (handler.GetType().Name == "SocketsHttpHandler") // Dành cho .NET 7+
                        {
                            try
                            {
                                // Dùng reflection để set SslOptions
                                var sslOptions = new SslClientAuthenticationOptions
                                {
                                    RemoteCertificateValidationCallback =
                                        (sender, certificate, chain, sslPolicyErrors) => true
                                };
                                handler.GetType().GetProperty("SslOptions")?
                                       .SetValue(handler, sslOptions);
                            }
                            catch (Exception e)
                            {
                                Debug.WriteLine($"Không thể set SSLOptions: {e.Message}");
                            }
                        }
                        return handler;
                    };
#endif
                })
                .WithAutomaticReconnect()
                .Build();

            // Lắng nghe sự kiện "BanUpdated"
            _hubConnection.On<TableStatusUpdateDto>("BanUpdated", (updateInfo) =>
            {
                OnTableStatusChanged?.Invoke(updateInfo);
            });

            await StartConnectionAsync();
        }

        

        private async Task StartConnectionAsync()
        {
            if (_hubConnection.State == HubConnectionState.Disconnected)
            {
                try
                {
                    await _hubConnection.StartAsync();
                    Debug.WriteLine("SignalR Connected (HTTPS).");
                }
                catch (Exception ex)
                {
                    
                    Debug.WriteLine($"SignalR Connection Error: {ex.Message}");
                    await Task.Delay(5000);
                    await StartConnectionAsync();
                }
            }
        }

        public async Task DisconnectAsync()
        {
            if (_hubConnection != null && IsConnected)
            {
                await _hubConnection.StopAsync();
            }
        }
    }
}