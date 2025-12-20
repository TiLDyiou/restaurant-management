using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantManagementGUI.Models;
using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Services;
using System.Collections.ObjectModel;

namespace RestaurantManagementGUI.ViewModels
{
    public partial class ChatViewModel : ObservableObject
    {
        private readonly ChatService _chatService;
        private List<ChatConversation> _allConversations = new();
        private string _currentConversationId = string.Empty;
        private bool _isHistoryLoading = false;
        private bool _isUpdatingList = false;

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private bool isDirectTab = false;
        [ObservableProperty] private string messageInput = string.Empty;
        [ObservableProperty] private bool isUploadingImage = false;

        public ObservableCollection<ChatConversation> FilteredConversations { get; } = new();
        public ObservableCollection<ChatMessage> CurrentMessages { get; } = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CurrentChatName))]
        private ChatConversation? selectedConversation;

        public string CurrentChatName => SelectedConversation?.Name ?? "Chọn cuộc hội thoại";

        public ChatViewModel(ChatService chatService)
        {
            _chatService = chatService;

            // Đăng ký sự kiện
            _chatService.OnMessageReceived += HandleIncomingMessage;
            _chatService.OnMessageSentConfirmed += HandleMessageSentConfirmed;
            _chatService.OnUserRead += HandleUserRead;

            // Gọi hàm khởi tạo
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            // 1. Tải dữ liệu API trước (Ưu tiên hiển thị để App không trống trơn)
            await LoadData();

            // 2. Kết nối SignalR sau (Tách biệt try/catch)
            try
            {
                await _chatService.Connect();
                Console.WriteLine("✅ SignalR Connected via ViewModel");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SignalR Connection Failed: {ex.Message}");
                // Có thể hiện Toast báo lỗi mạng nhẹ ở đây nếu muốn
            }
        }

        private async Task LoadData()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var data = await _chatService.GetInboxListAsync();
                _allConversations = data ?? new List<ChatConversation>();
                RefreshList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Load data error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void HandleIncomingMessage(ChatMessage msg)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    string incomingId = msg.ConversationId?.Trim() ?? "";
                    string currentId = _currentConversationId?.Trim() ?? "";
                    bool isChattingWithThisUser = !string.IsNullOrEmpty(currentId) &&
                                                  string.Equals(currentId, incomingId, StringComparison.OrdinalIgnoreCase);
                    var conv = _allConversations.FirstOrDefault(c => c.Id == incomingId);

                    if (conv == null)
                    {
                        await LoadData();
                        conv = _allConversations.FirstOrDefault(c => c.Id == incomingId);
                    }

                    if (conv != null)
                    {
                        conv.LastMessage = msg.IsImage ? "Hình ảnh" : msg.Content;
                        conv.LastMessageTime = msg.Timestamp;

                        if (isChattingWithThisUser)
                        {
                            var exists = CurrentMessages.Any(m => m.Id == msg.Id && msg.Id > 0) ||
                                         CurrentMessages.Any(m => m.Timestamp == msg.Timestamp && m.Content == msg.Content);

                            if (!exists)
                            {
                                CurrentMessages.Add(msg);
                            }
                            if (!_isHistoryLoading)
                            {
                                conv.UnreadCount = 0;
                                conv.IsUnread = false;
                                await _chatService.MarkAsReadAsync(incomingId);
                            }
                        }
                        else
                        {
                            conv.UnreadCount++;
                            conv.IsUnread = true;
                        }
                        RefreshListSilently();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Handle incoming error: {ex.Message}");
                }
            });
        }

        private void HandleMessageSentConfirmed(ChatMessage msg)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // TÌM TIN NHẮN "ĐANG GỬI..."
                // Sửa lỗi: So sánh thời gian cho phép lệch nhau 1-2 giây để tránh lỗi sai số giây/mili-giây
                var existingMsg = CurrentMessages.FirstOrDefault(m =>
                    m.Content == msg.Content &&
                    m.MaNV_Sender == msg.MaNV_Sender &&
                    Math.Abs((m.Timestamp - msg.Timestamp).TotalSeconds) < 5 && // Cho phép lệch 5 giây
                    !m.IsSentConfirmed); // Chỉ tìm những cái chưa xác nhận

                if (existingMsg != null)
                {
                    existingMsg.Id = msg.Id; // Cập nhật ID thật từ server
                    existingMsg.IsSentConfirmed = true; // <-- Chuyển trạng thái thành "Đã nhận"

                    // Nếu tin nhắn này server báo về là "IsRead = true" (trường hợp hiếm) thì cập nhật luôn
                    if (msg.IsRead) existingMsg.IsRead = true;
                }
            });
        }

        private void HandleUserRead(string conversationId, string userId)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Kiểm tra xem có đang mở đúng cuộc hội thoại đó không
                if (SelectedConversation?.Id == conversationId)
                {
                    var myUnreadMessages = CurrentMessages
                        .Where(m => m.MaNV_Sender == UserState.CurrentMaNV && !m.IsRead)
                        .ToList();

                    foreach (var msg in myUnreadMessages)
                    {
                        msg.IsRead = true; 
                    }
                }

                var conv = _allConversations.FirstOrDefault(c => c.Id == conversationId);
                if (conv != null && userId != UserState.CurrentMaNV)
                {
                    
                }
            });
        }

        [RelayCommand]
        private async Task SendMessage()
        {
            if (string.IsNullOrWhiteSpace(MessageInput) || SelectedConversation == null)
                return;

            try
            {
                var msg = new ChatMessage
                {
                    Content = MessageInput.Trim(),
                    MaNV_Sender = UserState.CurrentMaNV,
                    SenderName = UserState.CurrentTenNV,
                    MaNV_Receiver = SelectedConversation.IsGroup ? null : SelectedConversation.PartnerId,
                    ConversationId = SelectedConversation.Id,
                    Timestamp = DateTime.Now,
                    IsImage = false,
                    IsRead = false,
                    IsSentConfirmed = false
                };

                CurrentMessages.Add(msg);
                SelectedConversation.LastMessage = MessageInput.Trim();
                SelectedConversation.LastMessageTime = msg.Timestamp;
                MessageInput = string.Empty;
                await _chatService.SendMessageAsync(msg);
                RefreshListSilently();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Send message error: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Lỗi", 
                    "Không thể gửi tin nhắn. Vui lòng thử lại.", "OK");
            }
        }

        [RelayCommand]
        private async Task SendImage()
        {
            if (SelectedConversation == null || IsUploadingImage)
                return;

            try
            {
                var result = await MediaPicker.PickPhotoAsync(new MediaPickerOptions
                {
                    Title = "Chọn ảnh"
                });

                if (result == null)
                    return;

                IsUploadingImage = true;
                using var stream = await result.OpenReadAsync();
                var imageUrl = await _chatService.UploadImageAsync(stream, result.FileName);

                if (string.IsNullOrEmpty(imageUrl))
                {
                    await Application.Current.MainPage.DisplayAlert("Lỗi", 
                        "Không thể tải ảnh lên. Vui lòng thử lại.", "OK");
                    return;
                }
                var msg = new ChatMessage
                {
                    Content = imageUrl,
                    MaNV_Sender = UserState.CurrentMaNV,
                    SenderName = UserState.CurrentTenNV,
                    MaNV_Receiver = SelectedConversation.IsGroup ? null : SelectedConversation.PartnerId,
                    ConversationId = SelectedConversation.Id,
                    Timestamp = DateTime.Now,
                    IsImage = true,
                    IsRead = false
                };

                CurrentMessages.Add(msg);

                SelectedConversation.LastMessage = "📷Hình ảnh";
                SelectedConversation.LastMessageTime = msg.Timestamp;

                await _chatService.SendMessageAsync(msg);

                RefreshListSilently();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Send image error: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Lỗi", 
                    "Không thể gửi ảnh. Vui lòng thử lại.", "OK");
            }
            finally
            {
                IsUploadingImage = false;
            }
        }

        partial void OnSelectedConversationChanged(ChatConversation? value)
        {
            if (value == null || value.Id == _currentConversationId || _isUpdatingList) return;

            _currentConversationId = value.Id;
            value.UnreadCount = 0;
            value.IsUnread = false;

            CurrentMessages.Clear();

            Task.Run(async () =>
            {
                try
                {
                    await _chatService.Connect();
                    var history = await _chatService.GetHistoryAsync(value.Id);

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        _isHistoryLoading = true;
                        foreach (var m in history) CurrentMessages.Add(m);
                        _isHistoryLoading = false;
                    });

                    await _chatService.JoinConversationAsync(value.Id);
                    await _chatService.MarkAsReadAsync(value.Id);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error switching chat: {ex.Message}");
                }
            });
        }
        private void RefreshList()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                FilteredConversations.Clear();
                var items = _allConversations
                    .OrderByDescending(c => c.LastMessageTime)
                    .ToList();

                foreach (var item in items)
                {
                    FilteredConversations.Add(item);
                }
            });
        }

        private void RefreshListSilently()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _isUpdatingList = true;

                var currentId = SelectedConversation?.Id;
                var items = _allConversations
                    .OrderByDescending(c => c.LastMessageTime)
                    .ToList();

                FilteredConversations.Clear();
                foreach (var item in items)
                {
                    FilteredConversations.Add(item);
                }
                if (!string.IsNullOrEmpty(currentId))
                {
                    var found = FilteredConversations.FirstOrDefault(c => c.Id == currentId);
                    if (found != null)
                    {
                        selectedConversation = found;
                        OnPropertyChanged(nameof(SelectedConversation));
                    }
                }

                _isUpdatingList = false;
            });
        }

        public void ClearSelection()
        {
            SelectedConversation = null;
            _currentConversationId = string.Empty;
        }
    }
}