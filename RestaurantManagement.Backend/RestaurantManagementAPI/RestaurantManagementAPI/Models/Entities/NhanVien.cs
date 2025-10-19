using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurentManagementAPI.Models.Entities
{
    [Table("NHANVIEN")]
    public class NhanVien
    {
        [Key]
        [Column("MaNV")]
        [StringLength(5)]
        public string MaNV { get; set; } = string.Empty;

        [Column("HoTen")]
        public string HoTen { get; set; } = string.Empty;

        [Column("ChucVu")]
        public string? ChucVu { get; set; }

        [Column("SDT")]
        public string? SDT { get; set; }

        [Column("NgayVaoLam")]
        public DateTime? NgayVaoLam { get; set; }

        [Column("TrangThai")]
        public string? TrangThai { get; set; }
    }
}
