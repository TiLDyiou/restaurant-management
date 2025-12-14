
using System.Text.Json.Serialization;

namespace RestaurantManagementGUI.Models
{
    public class LoginResponseModel
    {
        [JsonPropertyName("token")]
        public string Token { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; } // Backend trả về "role" (Admin, NhanVien...)

        [JsonPropertyName("maNV")]
        public string MaNV { get; set; }
    }
}
