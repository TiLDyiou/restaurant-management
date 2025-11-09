using System.Text.Json.Serialization; // <-- 1. BẮT BUỘC PHẢI CÓ DÒNG NÀY

namespace RestaurantManagementGUI.Models
{
    public class Ban
    {
        // 2. Ánh xạ "maBan" (JSON) tới "MaBan" (C#)
        [JsonPropertyName("maBan")]
        public string MaBan { get; set; } = string.Empty;

        // 3. Ánh xạ "tenBan" (JSON) tới "TenBan" (C#)
        [JsonPropertyName("tenBan")]
        public string? TenBan { get; set; }

        // 4. Ánh xạ "trangThai" (JSON) tới "TrangThai" (C#)
        [JsonPropertyName("trangThai")]
        public string? TrangThai { get; set; }
    }
}