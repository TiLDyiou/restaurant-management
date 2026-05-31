using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantManagementAPI.Models.Entities
{
    [Table("MONAN")]
    public class MonAn
    {
        [Key]
        [Column("MaMA")]
        [StringLength(20)] // Mã món ăn sẽ có định dạng kiểu TrangMieng001
        public string MaMA { get; set; } = string.Empty;

        [Column("TenMA")]
        [Required]
        public string TenMA { get; set; } = string.Empty;

        [Column("DonGia")]
        public decimal DonGia { get; set; }

        [Column("Loai")]
        [StringLength(20)]
        [Required] // Phải có loại món ăn để chương trình sinh MaMA tự động
        public string? Loai { get; set; }

        [Column("HinhAnh")]
        public string? HinhAnh { get; set; } // Thêm hình ảnh minh hoạ cho món ăn

        [Column("TrangThai")]
        public bool TrangThai { get; set; } = true; // Dùng để "xoá mềm", set thành false khi không còn muốn bán món này nữa
        public ICollection<ChiTietHoaDon>? ChiTietHoaDons { get; set; } // 1-n
    }
}
