namespace RestaurantManagementAPI.Models.DTOs
{
    public class ChatMessageDto
    {
        public int Id { get; set; } // Thêm Id để dễ quản lý ở Client
        public string Content { get; set; } = string.Empty;
        public string MaNV_Sender { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string? MaNV_Receiver { get; set; } // Thêm người nhận (cho chat 1-1)
        public string ConversationId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool IsImage { get; set; }
        public bool IsRead { get; set; } // Thêm trạng thái đọc
    }

    public class InboxItemDto
    {
        public string ConversationId { get; set; } = string.Empty;

        // Thông tin đối phương (Hoặc tên Nhóm chung)
        public string DisplayName { get; set; } = string.Empty;
        public string? PartnerId { get; set; } // Mã nhân viên đối phương (null nếu là nhóm chung)

        // Thông tin tin nhắn cuối cùng để hiển thị bản xem trước
        public string LastMessage { get; set; } = string.Empty;
        public DateTime LastTimestamp { get; set; }

        // Số lượng tin nhắn chưa đọc (Nếu > 0 thì Client sẽ cho in đậm dòng này)
        public int UnreadCount { get; set; }

        public bool IsGroup { get; set; } // Để phân biệt icon nhóm hay icon cá nhân
    }
}