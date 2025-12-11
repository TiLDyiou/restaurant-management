using RestaurantManagementAPI.Models.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("NHANVIEN")]
public class NhanVien
{
    [Key]
    [Column("MaNV")]
    [StringLength(5)]
    public string MaNV { get; set; } = string.Empty;

    [Column("HoTen")]
    [Required]
    public string HoTen { get; set; } = string.Empty;

    [Column("ChucVu")]
    public string? ChucVu { get; set; }

    [Column("SDT")]
    public string? SDT { get; set; }

    [Column("NgayVaoLam")]
    public DateTime? NgayVaoLam { get; set; }

    [Column("TrangThai")]
    public string? TrangThai { get; set; }

    public TaiKhoan? TaiKhoan { get; set; }   // 1-1
    public ICollection<HoaDon>? HoaDons { get; set; } // 1-n
    public ICollection<PhieuNhapKho>? PhieuNhapKhos { get; set; } // 1-n
}
