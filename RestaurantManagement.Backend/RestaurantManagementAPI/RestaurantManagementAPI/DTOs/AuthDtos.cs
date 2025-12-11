namespace RestaurantManagementAPI.DTOs
{
    // DTO đăng ký tài khoản
    public class RegisterDto
    {
        public string TenDangNhap { get; set; } = string.Empty;
        public string MatKhau { get; set; } = string.Empty;
        public string HoTen { get; set; } = string.Empty;
        public string ChucVu { get; set; } = "Nhân viên";
        public string SDT { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Quyen { get; set; } = "NhanVien";  // quyền mặc định
    }

    // DTO login
    public class LoginDto
    {
        public string TenDangNhap { get; set; } = string.Empty;
        public string MatKhau { get; set; } = string.Empty;
    }
    public class UpdateUserDto
    {
        public string? HoTen { get; set; }
        public string? ChucVu { get; set; }       // Admin mới dùng
        public string? SDT { get; set; }
        public string? Quyen { get; set; }        // Admin mới dùng
        public string? MatKhau { get; set; }      // Admin hoặc user
        public string? Email { get; set; }        // Cả 2 đều có thể đổi email
    }


    // DTO gửi email (OTP hoặc quên mật khẩu)
    public class EmailDto
    {
        public string Email { get; set; } = string.Empty;
    }

    // DTO verify OTP
    public class VerifyOtpDto
    {
        public string Email { get; set; } = string.Empty;
        public string OTP { get; set; } = string.Empty;
    }

    // DTO reset password với OTP
    public class ResetPasswordDto
    {
        public string Email { get; set; } = string.Empty;
        public string OTP { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
