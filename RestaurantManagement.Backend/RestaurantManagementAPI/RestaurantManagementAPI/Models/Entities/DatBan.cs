using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurentManagementAPI.Models.Entities
{
    [Table("DATBAN")]
    public class DatBan
    {
        [Key]
        [Column("MaDatBan")]
        [StringLength(10)]
        public string MaDatBan { get; set; } = string.Empty;

        [Column("MaBan")]
        [StringLength(5)]
        public string MaBan { get; set; } = string.Empty;

        [Column("TenKhachHang")]
        [StringLength(100)]
        [Required]
        public string TenKhachHang { get; set; } = string.Empty;

        [Column("SoDienThoai")]
        [StringLength(15)]
        [Required]
        public string SoDienThoai { get; set; } = string.Empty;

        [Column("ThoiGianDat")]
        public DateTime ThoiGianDat { get; set; }

        [Column("SoNguoi")]
        public int SoNguoi { get; set; }

        [Column("TrangThai")]
        [StringLength(50)]
        public string? TrangThai { get; set; } // Ví dụ: "Đã xác nhận", "Đã huỷ", "Đã đến"

        // Khóa ngoại
        public Ban? Ban { get; set; }
    }
}