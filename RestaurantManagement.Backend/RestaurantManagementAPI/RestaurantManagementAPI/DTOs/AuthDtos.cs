namespace RestaurantManagementAPI.DTOs
{
    public class RegisterDto
    {
        public string TenDangNhap { get; set; } = string.Empty;
        public string MatKhau { get; set; } = string.Empty;
        public string HoTen { get; set; } = string.Empty;
        public string ChucVu { get; set; } = "Nhân viên";
        public string SDT { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Quyen { get; set; } = "NhanVien";
    }
    public class LoginDto
    {
        public string TenDangNhap { get; set; } = string.Empty;
        public string MatKhau { get; set; } = string.Empty;
    }
    public class UpdateUserDto
    {
        public string? HoTen { get; set; }
        public string? ChucVu { get; set; }
        public string? SDT { get; set; }
        public string? Quyen { get; set; }
        public string? MatKhau { get; set; }
        public string? Email { get; set; }
    }


    public class EmailDto
    {
        public string Email { get; set; } = string.Empty;
    }

    public class VerifyOtpDto
    {
        public string Email { get; set; } = string.Empty;
        public string OTP { get; set; } = string.Empty;
    }

    public class ResetPasswordDto
    {
        public string Email { get; set; } = string.Empty;
        public string OTP { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
