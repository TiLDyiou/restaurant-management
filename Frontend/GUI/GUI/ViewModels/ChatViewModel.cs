using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantManagementGUI.Models;
using System.Collections.ObjectModel;

namespace RestaurantManagementGUI.ViewModels
{
    public partial class ChatViewModel : ObservableObject
    {
        // --- TRẠNG THÁI MODAL ---
        [ObservableProperty] private bool showAddFriendModal;
        [ObservableProperty] private bool showFriendRequestModal;
        [ObservableProperty] private bool showCreateGroupModal;
        [ObservableProperty] private bool showManageGroupModal;
        [ObservableProperty] private bool showEmojiPicker;

        // --- DỮ LIỆU ---
        [ObservableProperty]
        private bool isDirectTab = true;

        // Khi đổi Tab -> Gọi hàm lọc lại danh sách
        partial void OnIsDirectTabChanged(bool value) => RefreshConversationList();

        // Kho dữ liệu gốc (Chứa tất cả)
        private List<ChatConversation> _allConversations = new();

        // Danh sách hiển thị ra màn hình (Đã lọc)
        public ObservableCollection<ChatConversation> FilteredConversations { get; } = new();

        // Danh sách tin nhắn hiện tại (Quan trọng)
        public ObservableCollection<ChatMessage> CurrentMessages { get; } = new();

        // Hội thoại đang chọn
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CurrentChatName))]
        private ChatConversation selectedConversation;

        // Khi chọn hội thoại khác -> Nạp tin nhắn của hội thoại đó
        partial void OnSelectedConversationChanged(ChatConversation value)
        {
            CurrentMessages.Clear();
            if (value != null)
            {
                foreach (var msg in value.Messages)
                {
                    CurrentMessages.Add(msg);
                }
            }
        }

        public string CurrentChatName => SelectedConversation?.Name ?? "Chat";
        [ObservableProperty] private string messageInput;
        [ObservableProperty] private string newGroupName;
        [ObservableProperty] private string groupCountText;

        // Dữ liệu Modal
        public ObservableCollection<Employee> AvailableUsersToAdd { get; } = new();
        public ObservableCollection<FriendRequest> PendingRequests { get; } = new();
        public ObservableCollection<Employee> UsersForNewGroup { get; } = new();
        public ObservableCollection<ChatConversation> GroupList { get; } = new();

        public ChatViewModel()
        {
            LoadDummyData();
        }

        private void LoadDummyData()
        {
            try
            {
                // Dữ liệu mẫu
                var group = new ChatConversation { Name = "Bếp & Order", Type = "group", Avatar = "#FF9800", LastMessage = "Alo 123", LastMessageTime = DateTime.Now };
                group.Messages.Add(new ChatMessage { SenderName = "A", Content = "Test tin nhắn", Timestamp = DateTime.Now, IsSentMessage = false, IsImage = false });

                var friend = new ChatConversation { Name = "Nguyễn Văn A", Type = "direct", Avatar = "#2196F3", LastMessage = "Hi", LastMessageTime = DateTime.Now };

                _allConversations.Add(group);
                _allConversations.Add(friend);

                // Modal data
                AvailableUsersToAdd.Add(new Employee { Name = "User A", Username = "@a", Avatar = "user.png" });
                foreach (var u in AvailableUsersToAdd) UsersForNewGroup.Add(new Employee { Name = u.Name, Username = u.Username, IsSelectedForGroup = false });

                // Init
                IsDirectTab = false;
                SelectedConversation = group;
                RefreshConversationList();
            }
            catch { }
        }

        private void RefreshConversationList()
        {
            MainThread.BeginInvokeOnMainThread(() => // BẮT BUỘC DÙNG CÁI NÀY
            {
                FilteredConversations.Clear();
                string targetType = IsDirectTab ? "direct" : "group";
                var items = _allConversations.Where(c => c.Type == targetType).OrderByDescending(c => c.LastMessageTime);
                foreach (var item in items) FilteredConversations.Add(item);
            });
        }

        // --- COMMANDS ---
        [RelayCommand] void SwitchToDirect() { IsDirectTab = true; }
        [RelayCommand] void SwitchToGroup() { IsDirectTab = false; }

        [RelayCommand]
        void OpenManageGroup()
        {
            GroupList.Clear();
            var groups = _allConversations.Where(c => c.Type == "group").ToList();
            foreach (var g in groups) GroupList.Add(g);
            GroupCountText = $"Tổng cộng: {groups.Count} nhóm";
            ShowManageGroupModal = true;
        }
        [RelayCommand] void CloseManageGroup() => ShowManageGroupModal = false;

        [RelayCommand] void OpenAddFriend() => ShowAddFriendModal = true;
        [RelayCommand] void CloseAddFriend() => ShowAddFriendModal = false;
        [RelayCommand] void OpenFriendRequests() => ShowFriendRequestModal = true;
        [RelayCommand] void CloseFriendRequests() => ShowFriendRequestModal = false;
        [RelayCommand] void OpenCreateGroup() { ShowCreateGroupModal = true; NewGroupName = ""; }
        [RelayCommand] void CloseCreateGroup() => ShowCreateGroupModal = false;
        [RelayCommand] void ToggleEmoji() => ShowEmojiPicker = !ShowEmojiPicker;

        // --- GỬI TIN NHẮN (FIX) ---
        [RelayCommand]
        void SendMessage()
        {
            if (string.IsNullOrWhiteSpace(MessageInput) || SelectedConversation == null) return;

            var msg = new ChatMessage
            {
                Content = MessageInput,
                SenderName = "Me",
                Timestamp = DateTime.Now,
                IsSentMessage = true,
                IsImage = false
            };

            // Cập nhật dữ liệu
            SelectedConversation.Messages.Add(msg);
            SelectedConversation.LastMessage = $"Bạn: {MessageInput}";
            SelectedConversation.LastMessageTime = DateTime.Now;

            // Cập nhật UI ngay lập tức
            MainThread.BeginInvokeOnMainThread(() =>
            {
                CurrentMessages.Add(msg);
                MessageInput = "";
                RefreshConversationList(); // Đẩy chat lên đầu sidebar
            });
        }

        [RelayCommand]
        async Task SendImage()
        {
            try
            {
                var result = await FilePicker.PickAsync(new PickOptions { FileTypes = FilePickerFileType.Images });
                if (result != null && SelectedConversation != null)
                {
                    var msg = new ChatMessage
                    {
                        Content = result.FullPath,
                        SenderName = "Me",
                        Timestamp = DateTime.Now,
                        IsSentMessage = true,
                        IsImage = true
                    };

                    SelectedConversation.Messages.Add(msg);
                    SelectedConversation.LastMessage = "Đã gửi 1 ảnh";

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        CurrentMessages.Add(msg);
                        RefreshConversationList();
                    });
                }
            }
            catch { }
        }

        [RelayCommand] void AddEmoji(string emoji) => MessageInput += emoji;

        [RelayCommand]
        void ConfirmCreateGroup()
        {
            if (string.IsNullOrWhiteSpace(NewGroupName)) return;
            var newGroup = new ChatConversation { Name = NewGroupName, Type = "group", Avatar = "#9C27B0", LastMessage = "Nhóm mới", LastMessageTime = DateTime.Now };
            _allConversations.Insert(0, newGroup);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                IsDirectTab = false;
                SelectedConversation = newGroup;
                RefreshConversationList();
                ShowCreateGroupModal = false;
            });
        }

        [RelayCommand] async Task CallAudio() => await App.Current.MainPage.DisplayAlert("Call", "Calling...", "OK");
        [RelayCommand] async Task CallVideo() => await App.Current.MainPage.DisplayAlert("Video", "Video Calling...", "OK");
    }
}