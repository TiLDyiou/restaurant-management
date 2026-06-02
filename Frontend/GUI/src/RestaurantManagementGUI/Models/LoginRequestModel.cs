
using System.Text.Json.Serialization;

namespace RestaurantManagementGUI.Models
{
    public class LoginRequestModel
    {
        public string TenDangNhap { get; set; }
        public string MatKhau { get; set; }
    }
}
