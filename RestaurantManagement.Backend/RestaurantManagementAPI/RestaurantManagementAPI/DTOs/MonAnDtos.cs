using System.ComponentModel.DataAnnotations;

namespace RestaurantManagementAPI.DTOs
{
    // DTO để trả về (GET)
    public class MonAnDto
    {
        public string MaMA { get; set; }
        public string TenMA { get; set; }
        public decimal DonGia { get; set; }
        public string? Loai { get; set; }
        public string? HinhAnh { get; set; }
    }

    // DTO để tạo mới (POST)
    public class CreateMonAnDto
    {


        [Required]
        public string TenMA { get; set; }

        [Range(0, double.MaxValue)]
        public decimal DonGia { get; set; }
        [Required]
        public string? Loai { get; set; }
        public string? HinhAnh { get; set; }
    }

    // DTO để cập nhật (PUT)
    public class UpdateMonAnDto
    {
        public string? TenMA { get; set; }
        public decimal? DonGia { get; set; }
        public string? Loai { get; set; }
        public string? HinhAnh { get; set; }
        public bool? TrangThai { get; set; }
    }
}