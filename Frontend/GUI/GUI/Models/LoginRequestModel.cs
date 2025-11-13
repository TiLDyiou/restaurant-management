using System.Text.Json.Serialization;

namespace RestaurantManagementGUI.Models
{
    public class LoginRequestModel
    {
        [JsonPropertyName("tenDangNhap")]
        public string TenDangNhap { get; set; }

        [JsonPropertyName("matKhau")]
        public string MatKhau { get; set; }
    }
}