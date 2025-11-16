// File: Models/FoodModel.cs
using System.Text.Json.Serialization;
using Microsoft.Maui.Graphics;

namespace RestaurantManagementGUI.Models
{
    public class FoodModel
    {
        // ===== Properties từ API =====
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

        // ===== Properties hỗ trợ UI =====

        /// <summary>
        /// Đường dẫn ảnh để hiển thị. Nếu API trả null hoặc rỗng, dùng placeholder.
        /// </summary>
        public string DisplayImageUrl =>
            string.IsNullOrWhiteSpace(ImageUrl)
                ? "https://via.placeholder.com/300x200/EEEEEE/999999?text=No+Image"
                : ImageUrl;

        /// <summary>
        /// Giá hiển thị đã format
        /// </summary>
        public string FormattedPrice => $"{Price:N0} ₫";

        /// <summary>
        /// Badge hiển thị trạng thái món
        /// </summary>
        public string StatusBadge => TrangThai ? "Còn món" : "Hết món";

        /// <summary>
        /// Màu badge
        /// </summary>
        public Color StatusColor => TrangThai ? Color.FromArgb("#4CAF50") : Color.FromArgb("#F44336");

        /// <summary>
        /// Có thể đặt món không?
        /// </summary>
        public bool CanOrder => TrangThai;
    }
}
