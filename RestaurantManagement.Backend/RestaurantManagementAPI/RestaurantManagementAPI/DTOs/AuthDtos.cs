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

    // User bình thường update thông tin cá nhân
    public class UpdateUserDto
    {
        public string? TenDangNhap { get; set; } = string.Empty;
        public string? SDT { get; set; }
    }

    // Admin update toàn bộ thông tin user
    public class AdminUpdateUserDto
    {
        public string TenDangNhap { get; set; } = string.Empty; // biết update ai qua TenDangNhap duy nhất
        public string? MatKhau { get; set; }
        public string? Quyen { get; set; }
        public string? HoTen { get; set; }
        public string? ChucVu { get; set; }
        public string? SDT { get; set; }
    }

    // user bình thường đổi mật khẩu
    public class ChangePasswordDto
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    // admin reset mật khẩu cho user
    public class ResetPasswordDto
    {
        public string TenDangNhap { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class DeleteUserDto
    {
        public string MaNV { get; set; } = string.Empty;
        public string HoTen { get; set; } = string.Empty;
    }

}
