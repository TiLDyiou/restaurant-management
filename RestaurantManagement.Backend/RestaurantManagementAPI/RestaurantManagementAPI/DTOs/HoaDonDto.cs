namespace RestaurentManagementAPI.DTOs.MonAnDtos
{
    // DTO hiển thị chi tiết món ăn trong hoá đơn
    public class ChiTietHoaDonViewDto
    {
        public string MaMA { get; set; } = string.Empty;
        public string TenMA { get; set; } = string.Empty; // Lấy từ MonAn
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public decimal ThanhTien { get; set; }
        public string? TrangThai { get; set; }
        public string? GhiChu { get; set; }
    }

    // DTO hiển thị thông tin đầy đủ của 1 hoá đơn
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
}