using System.Text.Json.Serialization;

namespace RestaurantManagementGUI.Models
{
    public class UpdateUserRequestModel
    {
        [JsonPropertyName("hoTen")]
        public string HoTen { get; set; }

        [JsonPropertyName("chucVu")]
        public string ChucVu { get; set; }

        [JsonPropertyName("sdt")]
        public string SDT { get; set; }

        [JsonPropertyName("quyen")]
        public string Quyen { get; set; }

        [JsonPropertyName("matKhau")]
        public string MatKhau { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }
    }

    public class UpdateUserResponse
    {
        public string Message { get; set; } = string.Empty;
        public string TenDangNhap { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsVerified { get; set; }
    }
}