using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace RestaurantManagementGUI.Models
{
    // Model Cuộc trò chuyện
    public partial class ChatConversation : ObservableObject
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public string Avatar { get; set; }
        public string Type { get; set; } // "direct" hoặc "group"

        [ObservableProperty] private string lastMessage;
        [ObservableProperty] private DateTime lastMessageTime;
        public string LastMessageTimeFormatted => LastMessageTime.ToString("HH:mm");

        [ObservableProperty] private bool isSelected;

        // Danh sách tin nhắn CỦA RIÊNG hội thoại này
        public ObservableCollection<ChatMessage> Messages { get; set; } = new();
    }

    // Model Tin nhắn
    public class ChatMessage
    {
        public string SenderName { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsSentMessage { get; set; }
        public bool IsImage { get; set; } // Quan trọng để trigger hiển thị ảnh
    }

    // Model Nhân viên
    public partial class Employee : ObservableObject
    {
        public string Name { get; set; }
        public string Username { get; set; }
        public string Avatar { get; set; }
        [ObservableProperty] private bool isSelectedForGroup;
    }

    // Model Lời mời
    public class FriendRequest
    {
        public string FromUserName { get; set; }
        public string FromUserAvatar { get; set; }
    }
}