using Microsoft.Maui.Controls;
using RestaurantManagementGUI.PopUps;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace RestaurantManagementGUI;

public partial class ChatPage : ContentPage, INotifyPropertyChanged
{
    private string _currentTab = "friends";
    private string _searchText = "";
    private bool _hasSelectedConversation = false;
    private string _currentUserId = "current_user"; // ID người dùng hiện tại

    public ObservableCollection<ConversationModel> Conversations { get; set; }
    public ObservableCollection<ConversationModel> FilteredConversations { get; set; }
    public ObservableCollection<MessageModel> Messages { get; set; }
    public ObservableCollection<FriendRequestModel> PendingRequests { get; set; }
    public ObservableCollection<EmployeeModel> AllEmployees { get; set; }

    private ConversationModel _currentConversation;
    public ConversationModel CurrentConversation
    {
        get => _currentConversation;
        set
        {
            _currentConversation = value;
            HasSelectedConversation = _currentConversation != null;
            OnPropertyChanged();
            if (_currentConversation != null)
            {
                LoadMessagesForConversation(_currentConversation.Id);
                MarkConversationAsRead(_currentConversation.Id);
            }
        }
    }

    public bool HasSelectedConversation
    {
        get => _hasSelectedConversation;
        set
        {
            _hasSelectedConversation = value;
            OnPropertyChanged();
        }
    }

    public bool HasPendingRequests => PendingRequests?.Count > 0;
    public int PendingRequestsCount => PendingRequests?.Count ?? 0;

    public string FriendsTabColor => _currentTab == "friends" ? "#0084FF" : "#CCCCCC";
    public string GroupTabColor => _currentTab == "group" ? "#0084FF" : "#CCCCCC";

    public ChatPage()
    {
        InitializeComponent();
        BindingContext = this;

        InitializeData();
        LoadMockData();
        FilterConversations();

        // Timer giả lập tin nhắn realtime
        Device.StartTimer(TimeSpan.FromSeconds(10), () =>
        {
            SimulateIncomingMessages();
            return true;
        });
    }

    private void InitializeData()
    {
        Conversations = new ObservableCollection<ConversationModel>();
        FilteredConversations = new ObservableCollection<ConversationModel>();
        Messages = new ObservableCollection<MessageModel>();
        PendingRequests = new ObservableCollection<FriendRequestModel>();
        AllEmployees = new ObservableCollection<EmployeeModel>();

        // Mock data nhân viên
        AllEmployees.Add(new EmployeeModel { Id = "emp_001", Name = "Nguyễn Văn A", Position = "Quản lý", EmployeeCode = "NV001" });
        AllEmployees.Add(new EmployeeModel { Id = "emp_002", Name = "Trần Thị B", Position = "Phục vụ", EmployeeCode = "NV002" });
        AllEmployees.Add(new EmployeeModel { Id = "emp_003", Name = "Lê Văn C", Position = "Đầu bếp", EmployeeCode = "NV003" });
        AllEmployees.Add(new EmployeeModel { Id = "emp_004", Name = "Phạm Thị D", Position = "Thu ngân", EmployeeCode = "NV004" });
        AllEmployees.Add(new EmployeeModel { Id = "emp_005", Name = "Hoàng Văn E", Position = "Pha chế", EmployeeCode = "NV005" });
    }

    private void LoadMockData()
    {
        // Thêm nhóm chung
        Conversations.Add(new ConversationModel
        {
            Id = "group_public",
            DisplayName = "Nhóm chung nhà hàng",
            AvatarUrl = "group_icon.png",
            LastMessagePreview = "Chào mừng tất cả mọi người đến với nhóm chat chung của nhà hàng!",
            LastMessageTime = "08:29",
            IsOnline = true,
            IsGroup = true,
            UnreadCount = 2,
            StatusText = "15 thành viên",
            Members = new ObservableCollection<string> { "emp_001", "emp_002", "emp_003", "emp_004", "emp_005" }
        });

        // Thêm bạn bè
        Conversations.Add(new ConversationModel
        {
            Id = "user_1",
            DisplayName = "Nguyễn Văn A",
            AvatarUrl = "avatar1.png",
            LastMessagePreview = "Ok, món đó xong rồi nhé",
            LastMessageTime = "09:15",
            IsOnline = true,
            UnreadCount = 0,
            StatusText = "Đang trực",
            EmployeeCode = "NV001"
        });

        Conversations.Add(new ConversationModel
        {
            Id = "user_2",
            DisplayName = "Trần Thị B",
            AvatarUrl = "avatar2.png",
            LastMessagePreview = "Có ai biết bàn 3 order món gì không?",
            LastMessageTime = "08:44",
            IsOnline = true,
            UnreadCount = 1,
            StatusText = "Đang làm việc",
            EmployeeCode = "NV002"
        });

        Conversations.Add(new ConversationModel
        {
            Id = "user_3",
            DisplayName = "Lê Văn C",
            AvatarUrl = "avatar3.png",
            LastMessagePreview = "Order món mới cho bàn 7",
            LastMessageTime = "08:59",
            IsOnline = false,
            UnreadCount = 0,
            StatusText = "Nghỉ trưa",
            EmployeeCode = "NV003"
        });

        // Mock lời mời kết bạn
        PendingRequests.Add(new FriendRequestModel
        {
            RequestId = "req_1",
            SenderId = "emp_004",
            SenderName = "Phạm Thị D",
            SenderCode = "NV004",
            Message = "Xin chào, tôi muốn kết bạn với bạn"
        });

        OnPropertyChanged(nameof(HasPendingRequests));
        OnPropertyChanged(nameof(PendingRequestsCount));

        // Chọn nhóm chung làm mặc định
        CurrentConversation = Conversations.FirstOrDefault(c => c.Id == "group_public");
    }

    private void FilterConversations()
    {
        var filtered = Conversations.Where(c =>
            (string.IsNullOrEmpty(_searchText) ||
             c.DisplayName.ToLower().Contains(_searchText.ToLower())) &&
            (_currentTab == "group" ? c.IsGroup : !c.IsGroup)
        ).ToList();

        FilteredConversations.Clear();
        foreach (var item in filtered)
        {
            FilteredConversations.Add(item);
        }
    }

    private void LoadMessagesForConversation(string conversationId)
    {
        Messages.Clear();

        if (conversationId == "group_public")
        {
            Messages.Add(new MessageModel
            {
                Id = Guid.NewGuid().ToString(),
                Content = "Chào mừng tất cả mọi người đến với nhóm chat chung của nhà hàng!",
                IsMine = false,
                SenderName = "Hệ thống",
                Timestamp = DateTime.Now.AddMinutes(-61),
                ShowSenderName = true
            });
            Messages.Add(new MessageModel
            {
                Id = Guid.NewGuid().ToString(),
                Content = "Có ai biết bàn 3 order món gì không?",
                IsMine = false,
                SenderName = "Trần Thị B",
                Timestamp = DateTime.Now.AddMinutes(-16),
                ShowSenderName = true
            });
            Messages.Add(new MessageModel
            {
                Id = Guid.NewGuid().ToString(),
                Content = "Bàn 3 order phở bò và gỏi cuốn",
                IsMine = true,
                Timestamp = DateTime.Now.AddMinutes(-11),
                SenderName = "Tôi"
            });
        }
        else if (conversationId == "user_1")
        {
            Messages.Add(new MessageModel
            {
                Id = Guid.NewGuid().ToString(),
                Content = "Chào bạn, bàn 5 cần thanh toán",
                IsMine = false,
                SenderName = "Nguyễn Văn A",
                Timestamp = DateTime.Now.AddHours(-1),
                ShowSenderName = false
            });
            Messages.Add(new MessageModel
            {
                Id = Guid.NewGuid().ToString(),
                Content = "Ok, tôi sẽ xử lý ngay",
                IsMine = true,
                Timestamp = DateTime.Now.AddMinutes(-45),
                SenderName = "Tôi"
            });
        }
        else if (conversationId == "user_2")
        {
            Messages.Add(new MessageModel
            {
                Id = Guid.NewGuid().ToString(),
                Content = "Có ai biết bàn 3 order món gì không?",
                IsMine = false,
                SenderName = "Trần Thị B",
                Timestamp = DateTime.Now.AddMinutes(-16),
                ShowSenderName = false
            });
        }
        else if (conversationId == "user_3")
        {
            Messages.Add(new MessageModel
            {
                Id = Guid.NewGuid().ToString(),
                Content = "Order món mới cho bàn 7",
                IsMine = false,
                SenderName = "Lê Văn C",
                Timestamp = DateTime.Now.AddMinutes(-1),
                ShowSenderName = false
            });
        }

        // Notify UI update
        OnPropertyChanged(nameof(Messages));

        // Cuộn xuống cuối sau khi load xong
        Device.BeginInvokeOnMainThread(async () =>
        {
            await Task.Delay(300);
            ScrollToBottom();
        });
    }

    private void MarkConversationAsRead(string conversationId)
    {
        var conversation = Conversations.FirstOrDefault(c => c.Id == conversationId);
        if (conversation != null)
        {
            conversation.UnreadCount = 0;
            OnPropertyChanged(nameof(Conversations));
            FilterConversations();
        }
    }

    private void SimulateIncomingMessages()
    {
        if (CurrentConversation != null && new Random().Next(0, 100) > 70)
        {
            var senders = new[] { "Nguyễn Văn A", "Trần Thị B", "Lê Văn C", "Phạm Thị D" };
            var messages = new[]
            {
                "Bàn 2 cần thêm nước",
                "Order món mới cho bàn 7",
                "Món số 5 đã xong chưa?",
                "Check hóa đơn bàn 4",
                "Cần hỗ trợ tại quầy bar"
            };

            var newMessage = new MessageModel
            {
                Id = Guid.NewGuid().ToString(),
                Content = messages[new Random().Next(messages.Length)],
                IsMine = false,
                SenderName = senders[new Random().Next(senders.Length)],
                Timestamp = DateTime.Now,
                ShowSenderName = CurrentConversation.IsGroup
            };

            Messages.Add(newMessage);

            if (CurrentConversation != null)
            {
                CurrentConversation.LastMessagePreview = newMessage.Content;
                CurrentConversation.LastMessageTime = "Vừa xong";
            }

            ScrollToBottom();
        }
    }

    private void ScrollToBottom()
    {
        Device.BeginInvokeOnMainThread(async () =>
        {
            await Task.Delay(150); // Tăng thời gian đợi để đảm bảo UI đã render

            if (MessageScrollView != null)
            {
                // Tính toán chiều cao của nội dung
                double contentHeight = MessageScrollView.ContentSize.Height;

                if (contentHeight > 0)
                {
                    await MessageScrollView.ScrollToAsync(0, contentHeight, true);
                }
            }
        });
    }

    // ===== EVENT HANDLERS =====

    private void OnTabChanged(object sender, EventArgs e)
    {
        var button = sender as Button;
        _currentTab = button?.Text == "Bạn bè" ? "friends" : "group";

        OnPropertyChanged(nameof(FriendsTabColor));
        OnPropertyChanged(nameof(GroupTabColor));

        FilterConversations();
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _searchText = e.NewTextValue;
        FilterConversations();
    }

    private void OnConversationSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is ConversationModel selected)
            CurrentConversation = selected;
    }

    private void OnSendClicked(object sender, EventArgs e)
    {
        // KHÔNG CẦN FindByName nữa vì đã có x:Name="MessageEditor" bên XAML
        // Lấy text trực tiếp từ control Editor
        string messageText = MessageEditor.Text;

        if (string.IsNullOrWhiteSpace(messageText) || CurrentConversation == null)
            return;

        var msg = new MessageModel
        {
            Id = Guid.NewGuid().ToString(),
            Content = messageText.Trim(),
            IsMine = true,
            Timestamp = DateTime.Now,
            SenderName = "Tôi"
        };

        // Thêm tin nhắn vào danh sách
        Messages.Add(msg);

        // Xóa nội dung ô nhập
        MessageEditor.Text = string.Empty;

        // Cập nhật thông tin cuộc hội thoại
        CurrentConversation.LastMessagePreview = msg.Content.Length > 50
            ? msg.Content.Substring(0, 50) + "..."
            : msg.Content;
        CurrentConversation.LastMessageTime = "Vừa xong";
        CurrentConversation.UnreadCount = 0;

        // Cập nhật UI
        OnPropertyChanged(nameof(Messages));
        FilterConversations();

        // Cuộn xuống tin nhắn mới nhất
        ScrollToBottom();
    }

    private async void OnSelectImageClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await FilePicker.PickAsync(new PickOptions
            {
                FileTypes = FilePickerFileType.Images,
                PickerTitle = "Chọn ảnh để gửi"
            });

            if (result != null && CurrentConversation != null)
            {
                var msg = new MessageModel
                {
                    Id = Guid.NewGuid().ToString(),
                    Content = "📷 Hình ảnh",
                    IsMine = true,
                    Timestamp = DateTime.Now,
                    SenderName = "Tôi",
                    ImagePath = result.FullPath,
                    HasImage = true
                };

                Messages.Add(msg);
                CurrentConversation.LastMessagePreview = "📷 Hình ảnh";
                CurrentConversation.LastMessageTime = "Vừa xong";

                FilterConversations();
                ScrollToBottom();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", $"Không thể chọn ảnh: {ex.Message}", "OK");
        }
    }

    private async void OnEmojiClicked(object sender, EventArgs e)
    {
        string[] emojis = { "😀", "😂", "❤️", "😍", "😉", "👍", "🙌", "🎉" };
        var action = await DisplayActionSheet("Chọn emoji", "Hủy", null, emojis);

        if (!string.IsNullOrEmpty(action) && action != "Hủy")
        {
            // Gọi trực tiếp MessageEditor thay vì FindByName
            MessageEditor.Text += action;
            MessageEditor.Focus(); // Focus lại vào editor
        }
    }

    private async void OnNotificationClicked(object sender, EventArgs e)
    {
        if (PendingRequests.Count == 0)
        {
            await DisplayAlert("Thông báo", "Không có lời mời kết bạn nào", "OK");
            return;
        }

        // Hiển thị popup danh sách lời mời
        var popup = new FriendRequestPopup(PendingRequests);
        popup.OnRequestAccepted += (request) =>
        {
            AcceptFriendRequest(request);
        };
        popup.OnRequestRejected += (request) =>
        {
            RejectFriendRequest(request);
        };

        await Navigation.PushModalAsync(popup);
    }

    private void AcceptFriendRequest(FriendRequestModel request)
    {
        // Thêm vào danh sách bạn bè
        var newFriend = new ConversationModel
        {
            Id = request.SenderId,
            DisplayName = $"{request.SenderName}",
            AvatarUrl = "avatar_default.png",
            LastMessagePreview = "Bạn đã chấp nhận lời mời kết bạn",
            LastMessageTime = "Vừa xong",
            IsOnline = true,
            UnreadCount = 0,
            StatusText = "Bạn bè mới",
            EmployeeCode = request.SenderCode
        };

        Conversations.Insert(0, newFriend);
        PendingRequests.Remove(request);

        OnPropertyChanged(nameof(HasPendingRequests));
        OnPropertyChanged(nameof(PendingRequestsCount));
        FilterConversations();

        DisplayAlert("Thành công", $"Đã chấp nhận lời mời kết bạn từ {request.SenderName}", "OK");
    }

    private void RejectFriendRequest(FriendRequestModel request)
    {
        PendingRequests.Remove(request);
        OnPropertyChanged(nameof(HasPendingRequests));
        OnPropertyChanged(nameof(PendingRequestsCount));
    }

    private async void OnAddFriendClicked(object sender, EventArgs e)
    {
        var popup = new AddFriendPopup(AllEmployees.ToList());
        popup.OnFriendSelected += (selectedEmployees) =>
        {
            foreach (var emp in selectedEmployees)
            {
                // Giả lập gửi lời mời (trong thực tế sẽ gọi API)
                DisplayAlert("Thành công", $"Đã gửi lời mời kết bạn tới {emp.Name} ({emp.EmployeeCode})", "OK");
            }
        };

        await Navigation.PushModalAsync(popup);
    }

    private async void OnRemoveFriendClicked(object sender, EventArgs e)
    {
        var friends = Conversations.Where(c => !c.IsGroup).ToList();

        if (friends.Count == 0)
        {
            await DisplayAlert("Thông báo", "Bạn chưa có bạn bè nào", "OK");
            return;
        }

        var popup = new RemoveFriendPopup(friends);
        popup.OnFriendRemoved += (conversation) =>
        {
            Conversations.Remove(conversation);
            FilterConversations();
            if (CurrentConversation?.Id == conversation.Id)
            {
                CurrentConversation = null;
            }
        };

        await Navigation.PushModalAsync(popup);
    }

    private async void OnCreateGroupClicked(object sender, EventArgs e)
    {
        var friends = Conversations.Where(c => !c.IsGroup).ToList();

        if (friends.Count == 0)
        {
            await DisplayAlert("Thông báo", "Bạn cần có ít nhất 1 bạn bè để tạo nhóm", "OK");
            return;
        }

        var popup = new CreateGroupPopup(friends);
        popup.OnGroupCreated += (groupName, selectedMembers) =>
        {
            var newGroup = new ConversationModel
            {
                Id = $"group_{Guid.NewGuid()}",
                DisplayName = groupName,
                AvatarUrl = "group_icon.png",
                LastMessagePreview = "Nhóm vừa được tạo",
                LastMessageTime = "Vừa xong",
                IsOnline = true,
                IsGroup = true,
                UnreadCount = 0,
                StatusText = $"{selectedMembers.Count + 1} thành viên",
                Members = new ObservableCollection<string>(selectedMembers.Select(m => m.Id))
            };

            Conversations.Add(newGroup);
            FilterConversations();
            DisplayAlert("Thành công", $"Đã tạo nhóm '{groupName}' với {selectedMembers.Count} thành viên", "OK");
        };

        await Navigation.PushModalAsync(popup);
    }

    private async void OnManageGroupClicked(object sender, EventArgs e)
    {
        var groups = Conversations.Where(c => c.IsGroup).ToList();

        if (groups.Count == 0)
        {
            await DisplayAlert("Thông báo", "Bạn chưa tham gia nhóm nào", "OK");
            return;
        }

        var popup = new ManageGroupPopup(groups);
        popup.OnGroupSelected += (group) =>
        {
            CurrentConversation = group;
            _currentTab = "group";
            OnPropertyChanged(nameof(GroupTabColor));
            OnPropertyChanged(nameof(FriendsTabColor));
            FilterConversations();
        };

        await Navigation.PushModalAsync(popup);
    }

    private async void OnChatOptionsClicked(object sender, EventArgs e)
    {
        if (CurrentConversation == null) return;

        string action = await DisplayActionSheet("Tùy chọn", "Hủy", null,
            "Xem thông tin",
            CurrentConversation.IsMuted ? "Bật thông báo" : "Tắt thông báo",
            CurrentConversation.IsGroup ? "Rời khỏi nhóm" : null);

        switch (action)
        {
            case "Xem thông tin":
                await ShowChatInfo();
                break;
            case "Tắt thông báo":
            case "Bật thông báo":
                ToggleMute();
                break;
            case "Rời khỏi nhóm":
                await LeaveGroup();
                break;
        }
    }

    private async Task ShowChatInfo()
    {
        if (CurrentConversation.IsGroup)
        {
            string memberList = string.Join(", ", CurrentConversation.Members.Take(5));
            if (CurrentConversation.Members.Count > 5)
                memberList += $" và {CurrentConversation.Members.Count - 5} người khác";

            await DisplayAlert("Thông tin nhóm",
                $"Tên nhóm: {CurrentConversation.DisplayName}\n" +
                $"Số thành viên: {CurrentConversation.Members.Count}\n" +
                $"Thành viên: {memberList}",
                "OK");
        }
        else
        {
            await DisplayAlert("Thông tin người dùng",
                $"Tên: {CurrentConversation.DisplayName}\n" +
                $"Mã nhân viên: {CurrentConversation.EmployeeCode}\n" +
                $"Trạng thái: {CurrentConversation.StatusText}",
                "OK");
        }
    }

    private void ToggleMute()
    {
        CurrentConversation.IsMuted = !CurrentConversation.IsMuted;
        OnPropertyChanged(nameof(CurrentConversation));
        FilterConversations();

        string status = CurrentConversation.IsMuted ? "tắt" : "bật";
        DisplayAlert("Thông báo", $"Đã {status} thông báo cho {CurrentConversation.DisplayName}", "OK");
    }

    private async Task LeaveGroup()
    {
        bool answer = await DisplayAlert("Xác nhận",
            $"Bạn có chắc muốn rời khỏi nhóm '{CurrentConversation.DisplayName}'?",
            "Có", "Không");

        if (answer)
        {
            Conversations.Remove(CurrentConversation);
            FilterConversations();
            CurrentConversation = null;
            await DisplayAlert("Thông báo", "Đã rời khỏi nhóm", "OK");
        }
    }

    private async void OnMessageTapped(object sender, EventArgs e)
    {
        var grid = sender as Grid;
        var message = grid?.BindingContext as MessageModel;

        if (message == null) return;

        bool answer = await DisplayAlert("Xóa tin nhắn",
            "Bạn có muốn xóa tin nhắn này không?",
            "Xóa", "Hủy");

        if (answer)
        {
            Messages.Remove(message);
        }
    }

    public new event PropertyChangedEventHandler PropertyChanged;
    protected new void OnPropertyChanged([CallerMemberName] string propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

// ===== MODELS =====

public class ConversationModel : INotifyPropertyChanged
{
    public string Id { get; set; }
    public string DisplayName { get; set; }
    public string AvatarUrl { get; set; }
    public string LastMessagePreview { get; set; }
    public string LastMessageTime { get; set; }
    public bool IsOnline { get; set; }
    public bool IsGroup { get; set; }
    public string StatusText { get; set; }
    public int UnreadCount { get; set; }
    public string EmployeeCode { get; set; }
    public ObservableCollection<string> Members { get; set; } = new ObservableCollection<string>();

    private bool _isMuted;
    public bool IsMuted
    {
        get => _isMuted;
        set
        {
            _isMuted = value;
            OnPropertyChanged();
        }
    }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            _isSelected = value;
            OnPropertyChanged();
        }
    }

    public Color OnlineColor => IsOnline ? Color.FromArgb("#4CAF50") : Color.FromArgb("#9E9E9E");
    public string InitialLetter => string.IsNullOrEmpty(DisplayName) ? "?" : DisplayName.Substring(0, 1).ToUpper();

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public class MessageModel
{
    public string Id { get; set; }
    public string Content { get; set; }
    public bool IsMine { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string SenderName { get; set; }
    public bool ShowSenderName { get; set; }
    public string ImagePath { get; set; }
    public bool HasImage { get; set; }

    public LayoutOptions Alignment => IsMine ? LayoutOptions.End : LayoutOptions.Start;
    public Color BubbleColor => IsMine ? Color.FromArgb("#0084FF") : Color.FromArgb("#E4E6EB");
    public Color TextColor => IsMine ? Colors.White : Colors.Black;
    public Color TimeColor => IsMine ? Color.FromArgb("#80FFFFFF") : Color.FromArgb("#80000000");
}

public class FriendRequestModel
{
    public string RequestId { get; set; }
    public string SenderId { get; set; }
    public string SenderName { get; set; }
    public string SenderCode { get; set; }
    public string Message { get; set; }
}

public class EmployeeModel
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Position { get; set; }
    public string EmployeeCode { get; set; }
    public bool IsSelected { get; set; }
}

// ===== CONVERTERS =====

public class InverseBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool boolValue)
            return !boolValue;
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool boolValue)
            return !boolValue;
        return false;
    }
}

public class GreaterThanZeroConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is int intValue)
            return intValue > 0;
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}