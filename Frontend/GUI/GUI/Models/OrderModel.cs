// File: Models/OrderModel.cs
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace RestaurantManagementGUI.Models
{
    // DTO này khớp với ChiTietHoaDonViewDto của API
    public class ChiTietHoaDonModel
    {
        [JsonPropertyName("maMA")]
        public string MaMA { get; set; }

        [JsonPropertyName("tenMA")]
        public string TenMA { get; set; }

        [JsonPropertyName("soLuong")]
        public int SoLuong { get; set; }

        [JsonPropertyName("donGia")]
        public decimal DonGia { get; set; }

        [JsonPropertyName("thanhTien")]
        public decimal ThanhTien { get; set; }

        [JsonPropertyName("trangThai")]
        public string TrangThai { get; set; }

        // Property để hiển thị
        public string DisplayText => $"{SoLuong} x {TenMA}";
    }

    // DTO này khớp với HoaDonDto của API
    public class HoaDonModel
    {
        [JsonPropertyName("maHD")]
        public string MaHD { get; set; }

        [JsonPropertyName("maBan")]
        public object MaBan { get; set; } // Giả sử MaBan là int

        [JsonPropertyName("maNV")]
        public string MaNV { get; set; }

        [JsonPropertyName("ngayLap")]
        public DateTime NgayLap { get; set; }

        [JsonPropertyName("tongTien")]
        public decimal TongTien { get; set; }

        [JsonPropertyName("trangThai")]
        public string TrangThai { get; set; }

        [JsonPropertyName("chiTietHoaDons")]
        public List<ChiTietHoaDonModel> ChiTietHoaDons { get; set; } = new();

        // Property để hiển thị
        public string DisplayText => $"{MaHD} (Bàn {MaBan})";
    }

    // DTO để gửi đi khi cập nhật trạng thái món
    public class UpdateOrderItemStatusDto
    {
        [JsonPropertyName("newStatus")]
        public string NewStatus { get; set; }
    }
}