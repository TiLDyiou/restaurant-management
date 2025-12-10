using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Controls;

namespace RestaurantManagementGUI;

public partial class ChatPage : ContentPage, INotifyPropertyChanged
{
    private string _currentTab = "friends";
    private string _searchText = "";
    private bool _hasSelectedConversation = false;

    public ObservableCollection<ConversationModel> Conversations { get; set; }
    public ObservableCollection<ConversationModel> FilteredConversations { get; set; }
    public ObservableCollection<MessageModel> Messages { get; set; }

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

    public string FriendsTabColor => _currentTab == "friends" ? "#0084FF" : "#CCCCCC";
    public string GroupTabColor => _currentTab == "group" ? "#0084FF" : "#CCCCCC";

    public ChatPage()
    {
        InitializeComponent();
        BindingContext = this;

        LoadMockData();
        FilterConversations();

        // Timer giả lập tin nhắn realtime
        Device.StartTimer(TimeSpan.FromSeconds(8), () =>
        {
            SimulateIncomingMessages();
            return true;
        });
    }

    private void LoadMockData()
    {
        Conversations = new ObservableCollection<ConversationModel>
        {
            new ConversationModel
            {
                Id = "group_public",
                DisplayName = "Nhóm chung nhà hàng",
                AvatarUrl = "group_icon.png",
                LastMessagePreview = "Admin: Họp lúc 15:00 chiều nay",
                LastMessageTime = "10:30",
                IsOnline = true,
                IsGroup = true,
                UnreadCount = 2,
                StatusText = "15 thành viên"
            },
            new ConversationModel
            {
                Id = "user_1",
                DisplayName = "Nguyễn Văn A (Quản lý)",
                AvatarUrl = "avatar1.png",
                LastMessagePreview = "Ok, món đó xong rồi nhé",
                LastMessageTime = "09:15",
                IsOnline = true,
                UnreadCount = 0,
                StatusText = "Đang trực"
            },
            new ConversationModel
            {
                Id = "user_2",
                DisplayName = "Trần Thị B (Phục vụ)",
                AvatarUrl = "avatar2.png",
                LastMessagePreview = "Chị ơi check bàn 5",
                LastMessageTime = "08:45",
                IsOnline = false,
                UnreadCount = 1,
                StatusText = "Nghỉ trưa"
            },
            new ConversationModel
            {
                Id = "user_3",
                DisplayName = "Lê Văn C (Đầu bếp)",
                AvatarUrl = "avatar3.png",
                LastMessagePreview = "Món số 7 còn 5 phút nữa xong",
                LastMessageTime = "Hôm qua",
                IsOnline = true,
                UnreadCount = 0,
                StatusText = "Đang làm việc"
            }
        };

        FilteredConversations = new ObservableCollection<ConversationModel>();
        foreach (var item in Conversations)
        {
            FilteredConversations.Add(item);
        }
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
        Messages = new ObservableCollection<MessageModel>();

        if (conversationId == "group_public")
        {
            Messages.Add(new MessageModel
            {
                Content = "Chào mừng tất cả mọi người đến với nhóm chat chung của nhà hàng!",
                IsMine = false,
                SenderName = "Hệ thống",
                Timestamp = DateTime.Now.AddMinutes(-30),
                ShowSenderName = true
            });
            Messages.Add(new MessageModel
            {
                Content = "Có ai biết bàn 3 order món gì không?",
                IsMine = false,
                SenderName = "Trần Thị B",
                Timestamp = DateTime.Now.AddMinutes(-15),
                ShowSenderName = true
            });
            Messages.Add(new MessageModel
            {
                Content = "Bàn 3 order phở bò và gỏi cuốn",
                IsMine = true,
                Timestamp = DateTime.Now.AddMinutes(-10)
            });
        }
        else if (conversationId == "user_1")
        {
            Messages.Add(new MessageModel
            {
                Content = "Chào bạn, bàn 5 cần thanh toán",
                IsMine = false,
                SenderName = "Nguyễn Văn A",
                Timestamp = DateTime.Now.AddHours(-1)
            });
            Messages.Add(new MessageModel
            {
                Content = "Ok, tôi sẽ xử lý ngay",
                IsMine = true,
                Timestamp = DateTime.Now.AddMinutes(-45)
            });
        }

        MessagesList.ItemsSource = Messages;
        ScrollToBottom();
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
        if (CurrentConversation != null && new Random().Next(0, 100) > 70) // 30% chance
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
                Content = messages[new Random().Next(messages.Length)],
                IsMine = false,
                SenderName = senders[new Random().Next(senders.Length)],
                Timestamp = DateTime.Now,
                ShowSenderName = CurrentConversation.IsGroup
            };

            Messages.Add(newMessage);

            // Cập nhật last message preview
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
        if (Messages?.Count > 0)
            MessagesList.ScrollTo(Messages.Last(), position: ScrollToPosition.End, animate: true);
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
        if (string.IsNullOrWhiteSpace(MessageEntry.Text) || CurrentConversation == null)
            return;

        var msg = new MessageModel
        {
            Content = MessageEntry.Text.Trim(),
            IsMine = true,
            Timestamp = DateTime.Now,
            SenderName = "Tôi"
        };

        Messages.Add(msg);
        MessageEntry.Text = string.Empty;

        // Cập nhật last message
        CurrentConversation.LastMessagePreview = msg.Content;
        CurrentConversation.LastMessageTime = "Vừa xong";
        CurrentConversation.UnreadCount = 0;

        FilterConversations();
        ScrollToBottom();
    }

    private async void OnAddFriendClicked(object sender, EventArgs e)
    {
        // TODO: Implement add friend logic with backend
        string result = await DisplayPromptAsync("Thêm bạn", "Nhập username hoặc email:");
        if (!string.IsNullOrWhiteSpace(result))
        {
            await DisplayAlert("Thông báo", $"Đã gửi lời mời kết bạn tới {result}", "OK");
        }
    }

    private async void OnRemoveFriendClicked(object sender, EventArgs e)
    {
        if (CurrentConversation == null || CurrentConversation.IsGroup)
        {
            await DisplayAlert("Thông báo", "Vui lòng chọn một người bạn để xóa", "OK");
            return;
        }

        bool answer = await DisplayAlert("Xác nhận",
            $"Bạn có chắc muốn xóa {CurrentConversation.DisplayName} khỏi danh sách bạn bè?",
            "Có", "Không");

        if (answer)
        {
            Conversations.Remove(CurrentConversation);
            FilterConversations();
            CurrentConversation = FilteredConversations.FirstOrDefault();
        }
    }

    private async void OnCreateGroupClicked(object sender, EventArgs e)
    {
        // TODO: Implement create group logic
        await DisplayAlert("Thông báo", "Tính năng tạo nhóm sẽ được phát triển sau", "OK");
    }

    private async void OnManageGroupClicked(object sender, EventArgs e)
    {
        // TODO: Implement group management logic
        await DisplayAlert("Thông báo", "Tính năng quản lý nhóm sẽ được phát triển sau", "OK");
    }

    private async void OnChatOptionsClicked(object sender, EventArgs e)
    {
        string action = await DisplayActionSheet("Tùy chọn", "Hủy", null,
            "Xem thông tin", "Tắt thông báo", "Rời khỏi nhóm", "Xóa lịch sử chat");

        // TODO: Handle selected action
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

// ===== MODELS & CONVERTERS =====
public class ConversationModel
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

    public Color OnlineColor => IsOnline ? Color.FromArgb("#4CAF50") : Color.FromArgb("#9E9E9E");
}

public class MessageModel
{
    public string Content { get; set; }
    public bool IsMine { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string SenderName { get; set; }
    public bool ShowSenderName { get; set; }

    public LayoutOptions Alignment => IsMine ? LayoutOptions.End : LayoutOptions.Start;
    public Color BubbleColor => IsMine ? Color.FromArgb("#0084FF") : Color.FromArgb("#E4E6EB");
    public Color TextColor => IsMine ? Colors.White : Colors.Black;
    public Color TimeColor => IsMine ? Color.FromArgb("#80FFFFFF") : Color.FromArgb("#80000000");
}

// Converter utilities
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