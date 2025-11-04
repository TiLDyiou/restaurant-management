using System.Text.Json.Serialization;

namespace RestaurantManagementGUI.Models
{
    public class AddUserRequestModel
    {
        [JsonPropertyName("tenDangNhap")]
        public string TenDangNhap { get; set; }

        [JsonPropertyName("matKhau")]
        public string MatKhau { get; set; }

        [JsonPropertyName("hoTen")]
        public string HoTen { get; set; }

        [JsonPropertyName("chucVu")]
        public string ChucVu { get; set; }

        [JsonPropertyName("sdt")]
        public string SDT { get; set; }

        [JsonPropertyName("quyen")]
        public string Quyen { get; set; }
    }
}
