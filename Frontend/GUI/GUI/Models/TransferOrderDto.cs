using System.Text.Json.Serialization;

namespace RestaurantManagementGUI.Models
{
    public class TransferOrderDto
    {
        [JsonPropertyName("maBanNguon")]
        public string MaBanNguon { get; set; } = string.Empty;

        [JsonPropertyName("maBanDich")]
        public string MaBanDich { get; set; } = string.Empty;
    }
}
