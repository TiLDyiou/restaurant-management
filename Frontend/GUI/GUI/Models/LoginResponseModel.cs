
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
        public string Role { get; set; }

        [JsonPropertyName("maNV")]
        public string MaNV { get; set; }
        [JsonPropertyName("hoTen")]
        public string hoTen { get; set; }
    }
}
