namespace RestaurentManagementAPI.DTOs
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

    // DTO admin update user
    public class AdminUpdateUserDto
    {
        public string? MatKhau { get; set; }
        public string? Quyen { get; set; }
        public string? HoTen { get; set; }
        public string? ChucVu { get; set; }
        public string? SDT { get; set; }
        public string? TrangThai { get; set; }
        public string? Email { get; set; }  // admin có thể update email
    }

    // DTO update thông tin cá nhân
    public class UpdateProfileDto
    {
        public string? SDT { get; set; }              // đổi số điện thoại
        public string? Email { get; set; }            // đổi email -> sẽ gửi OTP
        public string? CurrentPassword { get; set; }  // nếu đổi password cần mật khẩu hiện tại
        public string? NewPassword { get; set; }      // mật khẩu mới
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
