using System.ComponentModel.DataAnnotations;

namespace RestaurantManagementAPI.DTOs.MonAnDtos
{
    public class ChiTietHoaDonViewDto
    {
        public string MaMA { get; set; } = string.Empty;
        public string TenMA { get; set; } = string.Empty;
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public decimal ThanhTien { get; set; }
        public string? TrangThai { get; set; }
    }

    public class HoaDonDto
    {
        public string MaHD { get; set; } = string.Empty;
        public string MaBan { get; set; } = string.Empty;
        public string MaNV { get; set; } = string.Empty;
        public DateTime? NgayLap { get; set; }
        public decimal TongTien { get; set; }
        public string? TrangThai { get; set; }
        public ICollection<ChiTietHoaDonViewDto> ChiTietHoaDons { get; set; } = new List<ChiTietHoaDonViewDto>();
    }
    public class CheckoutRequestDto
    {
        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
        public string PaymentMethod { get; set; } = string.Empty;
    }
}
