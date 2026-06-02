using System.Text.Json.Serialization;

namespace RestaurantManagementGUI.Models
{
    public class AddUserRequestModel
    {
        public string HoTen { get; set; }
        public string TenDangNhap { get; set; }
        public string MatKhau { get; set; }
        public string SDT { get; set; }
        public string Email { get; set; }
        public string ChucVu { get; set; }
        public string Quyen { get; set; }
    }
}