using Microsoft.AspNetCore.SignalR.Client;
using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Models;
using System.Net.Http.Json;
using System.Net.Http.Headers;

namespace RestaurantManagementGUI.Services
{
    public class ChatService
    {
        private HubConnection _hubConnection;
        private readonly HttpClient _httpClient;
        public event Action<ChatMessage> OnMessageReceived;
        public event Action<ChatMessage> OnMessageSentConfirmed;
        public event Action<string, string> OnUserRead;

        public ChatService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            if (!_httpClient.DefaultRequestHeaders.Accept.Any(h => h.MediaType == "application/json"))
            {
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }

            InitializeSignalR();
        }

        private void InitializeSignalR()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(ApiConfig.ChatHubUrl, options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(UserState.AccessToken);
                    options.HttpMessageHandlerFactory = _ => handler;
                })
                .WithAutomaticReconnect()
                .Build();

            RegisterHandlers();
        }

        private void RegisterHandlers()
        {
            _hubConnection.On<ChatMessage>("ReceiveMessage", (msg) =>
            {
                OnMessageReceived?.Invoke(msg);
            });

            _hubConnection.On<ChatMessage>("MessageSentConfirmed", (msg) =>
            {
                OnMessageSentConfirmed?.Invoke(msg);
            });

            _hubConnection.On<string, string>("UserReadMessages", (convId, userId) =>
            {
                OnUserRead?.Invoke(convId, userId);
            });
        }

        public async Task Connect()
        {
            if (_hubConnection == null)
            {
                InitializeSignalR();
            }

            if (_hubConnection.State == HubConnectionState.Disconnected)
            {
                try
                {
                    Console.WriteLine($"Connecting SignalR to {ApiConfig.ChatHubUrl}...");
                    await _hubConnection.StartAsync();
                    Console.WriteLine("SignalR Connected Successfully!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"SignalR Connection Error: {ex.Message}");
                    // In chi tiết lỗi bên trong nếu có
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Inner: {ex.InnerException.Message}");
                    }
                }
            }
        }

        public async Task Disconnect()
        {
            if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
            {
                await _hubConnection.StopAsync();
            }
        }

        public async Task SendMessageAsync(ChatMessage message)
        {
            if (_hubConnection.State != HubConnectionState.Connected)
                await Connect();

            try
            {
                var messageEntity = new
                {
                    MaNV_Sender = message.MaNV_Sender,
                    SenderName = message.SenderName,
                    MaNV_Receiver = message.MaNV_Receiver,
                    ConversationId = message.ConversationId,
                    Content = message.Content,
                    Timestamp = message.Timestamp,
                    IsImage = message.IsImage,
                    IsRead = false
                };

                await _hubConnection.InvokeAsync("SendMessage", messageEntity);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Send message error: {ex.Message}");
                throw;
            }
        }

        public async Task JoinConversationAsync(string conversationId)
        {
            if (_hubConnection.State != HubConnectionState.Connected) return;
            try
            {
                await _hubConnection.InvokeAsync("JoinConversation", conversationId);
            }
            catch (Exception ex) { Console.WriteLine($"Join error: {ex.Message}"); }
        }

        public async Task LeaveConversationAsync(string conversationId)
        {
            if (_hubConnection.State != HubConnectionState.Connected) return;
            try
            {
                await _hubConnection.InvokeAsync("LeaveConversation", conversationId);
            }
            catch (Exception ex) { Console.WriteLine($"Leave error: {ex.Message}"); }
        }

        public async Task MarkAsReadAsync(string conversationId)
        {
            try
            {
                if (_hubConnection.State == HubConnectionState.Connected)
                {
                    await _hubConnection.InvokeAsync("MarkAsRead", conversationId, UserState.CurrentMaNV);
                }
                var url = $"{ApiConfig.MarkRead}?conversationId={conversationId}&userId={UserState.CurrentMaNV}";

                SetupAuthHeader();

                var response = await _httpClient.PostAsync(url, null);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Mark as read API failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Mark as read error: {ex.Message}");
            }
        }

        public async Task<List<ChatMessage>> GetHistoryAsync(string conversationId)
        {
            try
            {
                SetupAuthHeader();
                var res = await _httpClient.GetFromJsonAsync<ApiResponse<List<ChatMessage>>>(
                    ApiConfig.GetChatHistory(conversationId));

                return res?.Success == true ? res.Data : new List<ChatMessage>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Get history error: {ex.Message}");
                return new List<ChatMessage>();
            }
        }

        public async Task<List<ChatConversation>> GetInboxListAsync()
        {
            try
            {
                SetupAuthHeader();
                var res = await _httpClient.GetFromJsonAsync<ApiResponse<List<ChatConversation>>>(
                    ApiConfig.GetInboxList(UserState.CurrentMaNV));

                return res?.Success == true ? res.Data : new List<ChatConversation>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Get inbox error: {ex.Message}");
                return new List<ChatConversation>();
            }
        }

        public async Task<string?> UploadImageAsync(Stream imageStream, string fileName)
        {
            try
            {
                SetupAuthHeader();
                using var content = new MultipartFormDataContent();
                var streamContent = new StreamContent(imageStream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
                content.Add(streamContent, "file", fileName);

                var response = await _httpClient.PostAsync(ApiConfig.UploadChatImage, content);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<string>>();
                    return result?.Success == true ? result.Data : null;
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Upload image error: {ex.Message}");
                return null;
            }
        }

        private void SetupAuthHeader()
        {
            if (!string.IsNullOrEmpty(UserState.AccessToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", UserState.AccessToken);
            }
        }
    }
}