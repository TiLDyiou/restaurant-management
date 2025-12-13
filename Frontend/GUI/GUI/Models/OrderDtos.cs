using System.Text.Json.Serialization;

namespace RestaurantManagementGUI.Models
{
    // DTO gửi xuống Backend khi tạo đơn
    public class CreateHoaDonDto
    {
        [JsonPropertyName("maBan")]
        public string MaBan { get; set; }

        [JsonPropertyName("maNV")]
        public string MaNV { get; set; }

        [JsonPropertyName("chiTietHoaDons")]
        public List<ChiTietHoaDonDto> ChiTietHoaDons { get; set; }
    }

    public class ChiTietHoaDonDto
    {
        [JsonPropertyName("maMA")]
        public string MaMA { get; set; }

        [JsonPropertyName("soLuong")]
        public int SoLuong { get; set; }
    }
}