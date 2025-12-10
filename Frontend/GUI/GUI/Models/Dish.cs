using System.Text.Json.Serialization;
namespace RestaurantManagementGUI.Models
{
    public class Dish
    {
        [JsonPropertyName("maMA")] public string MaMA { get; set; }
        [JsonPropertyName("tenMA")] public string TenMA { get; set; }
        [JsonPropertyName("donGia")] public decimal DonGia { get; set; }
        [JsonPropertyName("loai")] public string Loai { get; set; }
        [JsonPropertyName("hinhAnh")] public string HinhAnh { get; set; }
        public string FormattedPrice => DonGia.ToString("N0") + " VND";
    }

    public class UpdateMonAnDto_Full
    {
        [JsonPropertyName("tenMA")]
        public string? TenMA { get; set; }
        [JsonPropertyName("donGia")]
        public decimal? DonGia { get; set; }
        [JsonPropertyName("loai")]
        public string? Loai { get; set; }
        [JsonPropertyName("hinhAnh")]
        public string? HinhAnh { get; set; }
        [JsonPropertyName("trangThai")]
        public bool? TrangThai { get; set; }
    }
}