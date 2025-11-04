using System.Text.Json.Serialization;

namespace RestaurantManagementGUI.Models
{
    public class UserModel
    {
        [JsonPropertyName("maNV")]
        public string MaNV { get; set; }

        [JsonPropertyName("hoTen")]
        public string HoTen { get; set; }

        [JsonPropertyName("chucVu")]
        public string ChucVu { get; set; }

        [JsonPropertyName("sdt")]
        public string SDT { get; set; }

        [JsonPropertyName("quyen")]
        public string Quyen { get; set; }

        [JsonPropertyName("tenDangNhap")]
        public string TenDangNhap { get; set; }
    }
}
