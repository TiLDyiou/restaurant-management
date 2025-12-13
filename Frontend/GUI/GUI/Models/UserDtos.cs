using System.Text.Json.Serialization;

namespace RestaurantManagementGUI.Models
{
    // DTO tạo User mới
    public class AddUserRequestModel
    {
        public string TenDangNhap { get; set; }
        public string MatKhau { get; set; }
        public string Email { get; set; }
        public string HoTen { get; set; }
        public string ChucVu { get; set; }
        public string SDT { get; set; }
        public string Quyen { get; set; }
    }

    // DTO cập nhật User
    public class UpdateUserRequestModel
    {
        public string HoTen { get; set; }
        public string ChucVu { get; set; }
        public string SDT { get; set; }
        public string Email { get; set; }
        public string MatKhau { get; set; }
        public string Quyen { get; set; }
    }
}