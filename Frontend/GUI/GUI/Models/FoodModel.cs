// File: Models/FoodModel.cs
using System.Text.Json.Serialization;

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
        /// Xử lý ImageUrl để hiển thị đúng
        /// </summary>
        public string DisplayImageUrl
        {
            get
            {
                // Nếu không có ảnh, trả về ảnh placeholder từ URL
                if (string.IsNullOrWhiteSpace(ImageUrl))
                    return "https://via.placeholder.com/300x200/EEEEEE/999999?text=No+Image";

                // Nếu đã là URL đầy đủ
                if (ImageUrl.StartsWith("http://") || ImageUrl.StartsWith("https://"))
                    return ImageUrl;

                // Nếu là đường dẫn tương đối, ghép với base URL
                // TODO: Thay YOUR_API_BASE_URL bằng URL thực tế của bạn
                // Ví dụ: return $"https://yourapi.com{ImageUrl}";
                return $"https://YOUR_API_BASE_URL{ImageUrl}";
            }
        }

        /// <summary>
        /// Hiển thị giá đã format
        /// </summary>
        public string FormattedPrice => $"{Price:N0} ₫";

        /// <summary>
        /// Badge hiển thị trạng thái
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