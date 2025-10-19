namespace RestaurentManagementAPI.DTOs
{
    public class RegisterDto
    {
        public string TenDangNhap { get; set; } = string.Empty;
        public string MatKhau { get; set; } = string.Empty;
        public string MaNV { get; set; } = string.Empty;
        public string HoTen { get; set; } = string.Empty;
        public string ChucVu { get; set; } = "NhanVien";
        public string SDT { get; set; } = string.Empty;
        public string Quyen { get; set; } = "NhanVien";
    }

    public class LoginDto
    {
        public string TenDangNhap { get; set; } = string.Empty;
        public string MatKhau { get; set; } = string.Empty;
    }
}
