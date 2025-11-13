using System.ComponentModel.DataAnnotations;

namespace RestaurentManagementAPI.DTOs.BanDtos
{
    public class CreateDatBanDto
    {
        [Required]
        public string MaBan { get; set; } = string.Empty;

        [Required]
        public string TenKhachHang { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string SoDienThoai { get; set; } = string.Empty;

        [Required]
        public DateTime ThoiGianDat { get; set; }

        [Range(1, int.MaxValue)]
        public int SoNguoi { get; set; }
    }
}
namespace RestaurentManagementAPI.DTOs.BanDtos
{
    public class DatBanDto
    {
        public string MaDatBan { get; set; } = string.Empty;
        public string MaBan { get; set; } = string.Empty;
        public string TenKhachHang { get; set; } = string.Empty;
        public string SoDienThoai { get; set; } = string.Empty;
        public DateTime ThoiGianDat { get; set; }
        public int SoNguoi { get; set; }
        public string? TrangThai { get; set; }
    }
}