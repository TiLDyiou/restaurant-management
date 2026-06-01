using System.Text.Json.Serialization;

namespace RestaurantManagementGUI.Models
{
    public class CreateBanDto
    {
        [JsonPropertyName("maBan")]
        public string MaBan { get; set; } = string.Empty;

        [JsonPropertyName("tenBan")]
        public string TenBan { get; set; } = string.Empty;

        [JsonPropertyName("sucChua")]
        public int SucChua { get; set; } = 4;

        [JsonPropertyName("khuVuc")]
        public string KhuVuc { get; set; } = string.Empty;
    }
}
