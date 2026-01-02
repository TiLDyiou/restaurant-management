using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantManagementAPI.Models.Entities
{
    [Table("MESSAGES")]
    public class Message
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string MaNV_Sender { get; set; } = string.Empty;

        [Required]
        public string SenderName { get; set; } = string.Empty;

        // Thêm MaNV_Receiver: 
        // Nếu là chat 1-1: Lưu mã nhân viên người nhận.
        // Nếu là chat Group Public: Để null hoặc giá trị rỗng.
        public string? MaNV_Receiver { get; set; }

        [Required]
        [StringLength(100)]
        public string ConversationId { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.Now;

        public bool IsImage { get; set; } = false;
        public bool IsRead { get; set; } = false;

        [ForeignKey("MaNV_Sender")]
        public virtual NhanVien? Sender { get; set; }

        [ForeignKey("MaNV_Receiver")]
        public virtual NhanVien? Receiver { get; set; }
    }
}