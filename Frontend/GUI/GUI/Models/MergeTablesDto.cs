using System.Text.Json.Serialization;

namespace RestaurantManagementGUI.Models
{
    public class MergeTablesDto
    {
        [JsonPropertyName("maBanChinh")]
        public string MaBanChinh { get; set; } = string.Empty;

        [JsonPropertyName("maBanPhu")]
        public string MaBanPhu { get; set; } = string.Empty;
    }
}
