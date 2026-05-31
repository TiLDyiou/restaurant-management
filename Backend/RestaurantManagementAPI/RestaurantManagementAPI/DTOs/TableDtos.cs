using System;
using System.ComponentModel.DataAnnotations;

namespace RestaurantManagementAPI.DTOs
{
    public class CreateBanDto
    {
        [Required]
        [StringLength(5)]
        public string MaBan { get; set; } = string.Empty;

        [Required]
        public string TenBan { get; set; } = string.Empty;

        [Range(1, 100)]
        public int SucChua { get; set; } = 4;

        [Required]
        public string KhuVuc { get; set; } = string.Empty;
    }

    public class UpdateBanDto
    {
        [Required]
        public string TenBan { get; set; } = string.Empty;

        [Range(1, 100)]
        public int SucChua { get; set; } = 4;

        [Required]
        public string KhuVuc { get; set; } = string.Empty;

        public string? TrangThai { get; set; }
    }

    public class MergeTablesDto
    {
        [Required]
        [StringLength(5)]
        public string MaBanChinh { get; set; } = string.Empty;

        [Required]
        [StringLength(5)]
        public string MaBanPhu { get; set; } = string.Empty;
    }

    public class TransferOrderDto
    {
        [Required]
        [StringLength(5)]
        public string MaBanNguon { get; set; } = string.Empty;

        [Required]
        [StringLength(5)]
        public string MaBanDich { get; set; } = string.Empty;
    }

    public class LichSuBanDto
    {
        public int Id { get; set; }
        public string MaBan { get; set; } = string.Empty;
        public string? TrangThaiCu { get; set; }
        public string TrangThaiMoi { get; set; } = string.Empty;
        public DateTime ThoiGian { get; set; }
        public string? MaNV { get; set; }
        public string? TenNV { get; set; }
    }
}
