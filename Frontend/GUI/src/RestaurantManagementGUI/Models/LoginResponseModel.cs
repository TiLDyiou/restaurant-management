using System;
using System.Text.Json.Serialization;

namespace RestaurantManagementGUI.Models
{
    public class LoginResponseModel
    {
        [JsonPropertyName("accessToken")]
        public string AccessToken { get; set; }

        [JsonPropertyName("refreshToken")]
        public string RefreshToken { get; set; }

        [JsonPropertyName("refreshTokenExpiresAt")]
        public DateTime RefreshTokenExpiresAt { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("maNV")]
        public string MaNV { get; set; }
    }
}
