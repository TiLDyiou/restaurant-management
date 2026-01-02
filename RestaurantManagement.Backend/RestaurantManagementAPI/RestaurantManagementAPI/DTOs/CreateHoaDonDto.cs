using System.ComponentModel.DataAnnotations;

namespace RestaurantManagementAPI.DTOs.MonAnDtos
{
    
    public class CreateChiTietHoaDonDto
    {
        [Required]
        public string MaMA { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int SoLuong { get; set; }
    }

    
    public class CreateHoaDonDto
    {
        [Required]
        public string MaBan { get; set; } = string.Empty;

        [Required]
        public string MaNV { get; set; } = string.Empty;

        [Required]
        [MinLength(1, ErrorMessage = "Phải có ít nhất một món ăn trong đơn hàng")]
        public List<CreateChiTietHoaDonDto> ChiTietHoaDons { get; set; } = new List<CreateChiTietHoaDonDto>();
    }
}