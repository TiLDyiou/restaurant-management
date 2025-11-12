// File: Models/FoodModel.cs
using System.Text.Json.Serialization; // <-- BẮT BUỘC PHẢI CÓ DÒNG NÀY

namespace RestaurantManagementGUI.Models
{
    public class FoodModel
    {
        // Ánh xạ 'maMA' (từ API) sang 'Id' (trong app)
        [JsonPropertyName("maMA")]
        public string Id { get; set; } = string.Empty;

        // Ánh xạ 'tenMA' sang 'Name'
        [JsonPropertyName("tenMA")]
        public string Name { get; set; } = string.Empty;

        // Ánh xạ 'donGia' sang 'Price'
        [JsonPropertyName("donGia")]
        public decimal Price { get; set; }

        // Ánh xạ 'loai' sang 'Category'
        [JsonPropertyName("loai")]
        public string Category { get; set; } = string.Empty;

        // Ánh xạ 'hinhAnh' sang 'ImageUrl'
        [JsonPropertyName("hinhAnh")]
        public string ImageUrl { get; set; } = string.Empty;

        // Ánh xạ 'trangThai'
        [JsonPropertyName("trangThai")]
        public bool TrangThai { get; set; } = true;
    }
}