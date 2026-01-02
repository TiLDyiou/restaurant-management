using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;
using Microsoft.Maui.Graphics;

namespace RestaurantManagementGUI.Models
{
    // 1. Class ChatConversation
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

    // 2. Class ChatMessage (CODE ĐÃ SỬA LỖI)
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

        // --- PHẦN SỬA LỖI: VIẾT THỦ CÔNG HOÀN TOÀN ---
        // Không dùng [ObservableProperty] ở đây nữa để tránh bị tạo trùng

        private bool _isRead;
        [JsonPropertyName("isRead")]
        public bool IsRead
        {
            get => _isRead;
            set
            {
                if (SetProperty(ref _isRead, value))
                {
                    OnPropertyChanged(nameof(StatusText));
                    OnPropertyChanged(nameof(StatusColor));
                }
            }
        }

        private bool _isSentConfirmed;
        // Biến này không cần JsonPropertyName
        public bool IsSentConfirmed
        {
            get => _isSentConfirmed;
            set
            {
                if (SetProperty(ref _isSentConfirmed, value))
                {
                    OnPropertyChanged(nameof(StatusText));
                    OnPropertyChanged(nameof(StatusColor));
                }
            }
        }
        // ----------------------------------------------

        public bool IsSentMessage => MaNV_Sender == UserState.CurrentMaNV;
        public bool IsReceivedMessage => !IsSentMessage;
        public string TimeDisplay => Timestamp.ToString("HH:mm");

        public string DateDisplay
        {
            get
            {
                var today = DateTime.Today;
                var msgDate = Timestamp.Date;

                if (msgDate == today) return "Hôm nay";
                else if (msgDate == today.AddDays(-1)) return "Hôm qua";
                else return Timestamp.ToString("dd/MM/yyyy");
            }
        }

        public string StatusText
        {
            get
            {
                if (!IsSentMessage) return string.Empty;
                if (IsRead) return "Đã xem";
                if (IsSentConfirmed) return "Đã nhận";
                return "Đang gửi...";
            }
        }

        public Color StatusColor
        {
            get
            {
                if (IsRead) return Color.FromArgb("#0084FF"); // Màu Xanh
                return Color.FromArgb("#B0B3B8"); // Màu Xám
            }
        }
    }

    // 3. Class EmployeeModel
    public class EmployeeModel
    {
        public string MaNV { get; set; } = string.Empty;
        public string HoTen { get; set; } = string.Empty;
        public string ChucVu { get; set; } = string.Empty;
        public string InitialLetter => string.IsNullOrEmpty(HoTen) ? "?" : HoTen[0].ToString().ToUpper();
    }
}