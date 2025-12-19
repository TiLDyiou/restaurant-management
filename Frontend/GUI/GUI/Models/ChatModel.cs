using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace RestaurantManagementGUI.Models
{
    public partial class ChatConversation : ObservableObject
    {
        [JsonPropertyName("conversationId")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("displayName")]
        public string Name { get; set; } = string.Empty;

        public string Avatar { get; set; } = "#2196F3";

        [JsonPropertyName("partnerId")]
        public string? PartnerId { get; set; }

        [JsonPropertyName("isGroup")]
        public bool IsGroup { get; set; }

        [ObservableProperty]
        [JsonPropertyName("lastMessage")]
        private string lastMessage = string.Empty;

        [ObservableProperty]
        [JsonPropertyName("lastTimestamp")]
        private DateTime lastMessageTime;

        [ObservableProperty]
        [JsonPropertyName("unreadCount")]
        [NotifyPropertyChangedFor(nameof(HasUnread))]
        [NotifyPropertyChangedFor(nameof(IsUnread))]
        private int unreadCount;
        public bool HasUnread => UnreadCount > 0;

        [ObservableProperty]
        private bool isUnread;

        public string InitialLetter => string.IsNullOrEmpty(Name) ? "?" : Name[0].ToString().ToUpper();

        partial void OnUnreadCountChanged(int value)
        {
            IsUnread = value > 0;
        }
    }

    public partial class ChatMessage : ObservableObject
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("maNV_Sender")]
        public string MaNV_Sender { get; set; } = string.Empty;

        [JsonPropertyName("senderName")]
        public string SenderName { get; set; } = string.Empty;

        [JsonPropertyName("maNV_Receiver")]
        public string? MaNV_Receiver { get; set; }

        [JsonPropertyName("conversationId")]
        public string ConversationId { get; set; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("isImage")]
        public bool IsImage { get; set; }

        [ObservableProperty]
        [JsonPropertyName("isRead")]
        private bool isRead;

        public bool IsSentMessage => MaNV_Sender == UserState.CurrentMaNV;

        public bool IsReceivedMessage => !IsSentMessage;
        public string TimeDisplay => Timestamp.ToString("HH:mm");

        public string DateDisplay
        {
            get
            {
                var today = DateTime.Today;
                var msgDate = Timestamp.Date;

                if (msgDate == today)
                    return "Hôm nay";
                else if (msgDate == today.AddDays(-1))
                    return "Hôm qua";
                else
                    return Timestamp.ToString("dd/MM/yyyy");
            }
        }
    }

    public class EmployeeModel
    {
        public string MaNV { get; set; } = string.Empty;
        public string HoTen { get; set; } = string.Empty;
        public string ChucVu { get; set; } = string.Empty;
        public string InitialLetter => string.IsNullOrEmpty(HoTen) ? "?" : HoTen[0].ToString().ToUpper();
    }
}