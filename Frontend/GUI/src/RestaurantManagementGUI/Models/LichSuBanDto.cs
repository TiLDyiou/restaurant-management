using System;
using System.Text.Json.Serialization;

namespace RestaurantManagementGUI.Models
{
    public class LichSuBanDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("maBan")]
        public string MaBan { get; set; } = string.Empty;

        [JsonPropertyName("trangThaiCu")]
        public string TrangThaiCu { get; set; }

        [JsonPropertyName("trangThaiMoi")]
        public string TrangThaiMoi { get; set; } = string.Empty;

        [JsonPropertyName("thoiGian")]
        public DateTime ThoiGian { get; set; }

        [JsonPropertyName("maNV")]
        public string MaNV { get; set; }

        [JsonPropertyName("tenNV")]
        public string TenNV { get; set; }
    }
}
