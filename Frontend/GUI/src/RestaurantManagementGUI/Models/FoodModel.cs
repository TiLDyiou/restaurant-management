
using System.Text.Json.Serialization;
using Microsoft.Maui.Graphics;

namespace RestaurantManagementGUI.Models
{
    public class FoodModel
    {
        [JsonPropertyName("maMA")]
        public string Id { get; set; }

        [JsonPropertyName("tenMA")]
        public string Name { get; set; }

        [JsonPropertyName("donGia")]
        public decimal Price { get; set; }

        [JsonPropertyName("loai")]
        public string Category { get; set; }

        [JsonPropertyName("hinhAnh")]
        public string ImageUrl { get; set; }

        [JsonPropertyName("trangThai")]
        public bool TrangThai { get; set; } = true;

        [JsonIgnore]
        public string DisplayImageUrl => string.IsNullOrEmpty(ImageUrl) ? "default_food.png" : ImageUrl;

        [JsonIgnore]
        public string FormattedPrice => $"{Price:N0} đ";
    }
}
