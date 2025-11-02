namespace RestaurentManagementAPI.DTOs
{
    public class RegisterDto
    {
        public string TenDangNhap { get; set; } = string.Empty;
        public string MatKhau { get; set; } = string.Empty;
        public string MaNV { get; set; } = string.Empty;
        public string HoTen { get; set; } = string.Empty;
        public string ChucVu { get; set; } = "Nhân viên";
        public string SDT { get; set; } = string.Empty;
        public string Quyen { get; set; } = "NhanVien";
    }

    public class LoginDto
    {
        public string TenDangNhap { get; set; } = string.Empty;
        public string MatKhau { get; set; } = string.Empty;
    }

    // Admin update toàn bộ thông tin user
    public class AdminUpdateUserDto
    {
        public string? MatKhau { get; set; }
        public string? Quyen { get; set; }
        public string? HoTen { get; set; }
        public string? ChucVu { get; set; }
        public string? SDT { get; set; }
    }

    public class UpdateProfileDto
    {
        public string? SDT { get; set; }           // đổi số điện thoại
        public string? CurrentPassword { get; set; } // nếu đổi password thì cần mật khẩu hiện tại
        public string? NewPassword { get; set; }     // mật khẩu mới
    }

    public class DeleteUserDto
    {
        public string MaNV { get; set; } = string.Empty;
        public string HoTen { get; set; } = string.Empty;
    }

}
