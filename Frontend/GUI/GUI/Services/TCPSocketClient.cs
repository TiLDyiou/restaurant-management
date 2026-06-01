using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Maui.Storage;
using RestaurantManagementGUI.Helpers;
using System.Diagnostics;
using System.Text.Json;

namespace RestaurantManagementGUI.Services
{
    public class TCPSocketClient
    {
        private static TCPSocketClient _instance;
        public static TCPSocketClient Instance => _instance ??= new TCPSocketClient();

        private HubConnection? _hubConnection;
        private bool _isConnected;

        public event Action<string>? OnNewOrderReceived;
        public event Action<string>? OnTableStatusChanged;
        public event Action<string>? OnDishDone;
        public event Action<string>? OnChatReceived; // Kept for backward compatibility

        private TCPSocketClient()
        {
            InitializeSignalR();
        }

        private void InitializeSignalR()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(ApiConfig.RestaurantHubUrl, options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(UserState.AccessToken);
                    options.HttpMessageHandlerFactory = _ => handler;
                    options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets;
                    options.SkipNegotiation = true;
                })
                .WithAutomaticReconnect()
                .Build();

            RegisterHandlers();
        }

        private void RegisterHandlers()
        {
            if (_hubConnection == null) return;

            // 1. Table status changed event
            _hubConnection.On<object>("TableStatusChanged", (payload) =>
            {
                try
                {
                    string jsonStr = JsonSerializer.Serialize(payload);
                    Debug.WriteLine($"[SignalR RECV TableStatusChanged]: {jsonStr}");
                    OnTableStatusChanged?.Invoke(jsonStr);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error parsing TableStatusChanged: {ex.Message}");
                }
            });

            // 2. Order created event
            _hubConnection.On<string>("OrderCreated", (maHD) =>
            {
                Debug.WriteLine($"[SignalR RECV OrderCreated]: {maHD}");
                OnNewOrderReceived?.Invoke(maHD);
            });

            // 3. Kitchen item ready event
            _hubConnection.On<string>("KitchenItemReady", (msg) =>
            {
                Debug.WriteLine($"[SignalR RECV KitchenItemReady]: {msg}");
                OnDishDone?.Invoke(msg);
            });

            // 4. User status changed event
            _hubConnection.On<string, bool>("UserStatusChanged", (maNV, isOnline) =>
            {
                string statusStr = isOnline ? "TRUE" : "FALSE";
                string message = $"STATUS|{maNV}|{statusStr}";
                Debug.WriteLine($"[SignalR RECV UserStatusChanged]: {message}");
                MessagingCenter.Send(this, "UpdateStatus", message);
            });
        }

        public async Task ConnectAsync()
        {
            if (_isConnected || _hubConnection == null) return;

            if (_hubConnection.State == HubConnectionState.Disconnected)
            {
                try
                {
                    Debug.WriteLine($"Connecting SignalR RestaurantHub to {ApiConfig.RestaurantHubUrl}...");
                    await _hubConnection.StartAsync();
                    _isConnected = true;
                    Debug.WriteLine("SignalR RestaurantHub Connected Successfully!");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"SignalR RestaurantHub Connection Error: {ex.Message}");
                    _isConnected = false;
                }
            }
        }

        public async Task LoginAsync(string maNV)
        {
            // Backward compatibility helper
            // We just ensure ConnectAsync is executed, auth is automatically managed via JWT Token Provider
            await ConnectAsync();
        }

        public async Task SendChatAsync(string message)
        {
            // Chat is now fully processed through ChatService and RestaurantChatHub
            await Task.CompletedTask;
        }

        public async Task DisconnectAsync()
        {
            if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
            {
                try
                {
                    await _hubConnection.StopAsync();
                    Debug.WriteLine("SignalR RestaurantHub Disconnected Safely.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error stopping SignalR connection: {ex.Message}");
                }
                finally
                {
                    _isConnected = false;
                }
            }
        }
    }
}