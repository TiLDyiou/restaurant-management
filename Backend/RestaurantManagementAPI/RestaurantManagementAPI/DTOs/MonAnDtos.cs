using System.ComponentModel.DataAnnotations;

namespace RestaurantManagementAPI.DTOs
{
    public class MonAnDto
    {
        public string MaMA { get; set; }
        public string TenMA { get; set; }
        public decimal DonGia { get; set; }
        public string? Loai { get; set; }
        public string? HinhAnh { get; set; }
    }
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
    public class UpdateMonAnDto
    {
        public string? TenMA { get; set; }
        public decimal? DonGia { get; set; }
        public string? Loai { get; set; }
        public string? HinhAnh { get; set; }
        public bool? TrangThai { get; set; }
    }
}