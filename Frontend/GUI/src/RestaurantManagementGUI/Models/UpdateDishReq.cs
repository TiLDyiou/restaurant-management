using System.Text.Json.Serialization;

namespace RestaurantManagementGUI.Models
{
    public class UpdateDishReq
    {
        [JsonPropertyName("tenMA")]
        public string TenMA { get; set; }

        [JsonPropertyName("donGia")]
        public decimal? DonGia { get; set; }

        [JsonPropertyName("loai")]
        public string Loai { get; set; }

        [JsonPropertyName("hinhAnh")]
        public string HinhAnh { get; set; }

        [JsonPropertyName("trangThai")]
        public bool? TrangThai { get; set; }
    }
}