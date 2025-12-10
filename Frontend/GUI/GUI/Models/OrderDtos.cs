using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RestaurantManagementGUI.Models
{
    // DTO dùng để gửi yêu cầu tạo hóa đơn lên Server
    public class CreateHoaDonDto
    {
        [JsonPropertyName("maBan")]
        public string? MaBan { get; set; }

        [JsonPropertyName("maNV")]
        public string MaNV { get; set; }

        [JsonPropertyName("chiTietHoaDons")]
        public List<ChiTietHoaDonDto> ChiTietHoaDons { get; set; }
    }

    // DTO chi tiết món ăn trong đơn
    public class ChiTietHoaDonDto
    {
        [JsonPropertyName("maMA")]
        public string? MaMA { get; set; }

        [JsonPropertyName("soLuong")]
        public int SoLuong { get; set; }
    }
}