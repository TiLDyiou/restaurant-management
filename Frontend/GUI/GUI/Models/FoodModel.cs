using System.Text.Json.Serialization;
using Microsoft.Maui.Graphics;

namespace RestaurantManagementGUI.Models
{
    public class FoodModel
    {
        [JsonPropertyName("maMA")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("tenMA")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("donGia")]
        public decimal Price { get; set; }

        [JsonPropertyName("loai")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("hinhAnh")]
        public string ImageUrl { get; set; } = string.Empty;

        [JsonPropertyName("trangThai")]
        public bool TrangThai { get; set; } = true;
        public string DisplayImageUrl =>
            string.IsNullOrWhiteSpace(ImageUrl)
                ? "https://via.placeholder.com/300x200/EEEEEE/999999?text=No+Image"
                : ImageUrl;
        public string FormattedPrice => $"{Price:N0} ₫";
        public string StatusBadge => TrangThai ? "Còn món" : "Hết món";
        public Color StatusColor => TrangThai ? Color.FromArgb("#4CAF50") : Color.FromArgb("#F44336");
        public bool CanOrder => TrangThai;
    }
}
