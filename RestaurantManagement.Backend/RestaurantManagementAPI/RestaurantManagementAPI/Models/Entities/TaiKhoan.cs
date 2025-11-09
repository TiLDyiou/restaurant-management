using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurentManagementAPI.Models.Entities
{
    [Table("TAIKHOAN")]
    public class TaiKhoan
    {
        [Key]
        [Column("TenDangNhap")]
        [StringLength(30)]
        public string TenDangNhap { get; set; } = string.Empty;

        [Column("MatKhau")]
        public string MatKhau { get; set; } = string.Empty;

        [Column("MaNV")]
        [StringLength(5)]
        public string MaNV { get; set; } = string.Empty;

        [Column("Quyen")]
        public string? Quyen { get; set; } = "NhanVien";

        [Column("HoatDong")]
        public bool IsActive { get; set; } = true;

        [Column("Online")]
        public bool Online { get; set; } = false;
        public NhanVien NhanVien { get; set; } = null!;
    }
}
