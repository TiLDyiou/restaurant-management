using System.ComponentModel.DataAnnotations;

namespace RestaurantManagementAPI.DTOs
{
    public class RegisterDto
    {
        [Required, StringLength(30, MinimumLength = 3)]
        public string TenDangNhap { get; set; } = string.Empty;

        [Required, MinLength(8)]
        public string MatKhau { get; set; } = string.Empty;

        [Required]
        public string HoTen { get; set; } = string.Empty;

        public string ChucVu { get; set; } = "Nhân viên";

        public string SDT { get; set; } = string.Empty;

        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public class LoginDto
    {
        [Required]
        public string TenDangNhap { get; set; } = string.Empty;

        [Required]
        public string MatKhau { get; set; } = string.Empty;
    }

    public class UpdateUserDto
    {
        public string? HoTen { get; set; }
        public string? ChucVu { get; set; }
        public string? SDT { get; set; }
        public string? Quyen { get; set; }
        public string? MatKhau { get; set; }
        public string? OldPassword { get; set; }
        public string? Email { get; set; }
    }

    public class EmailDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public class VerifyOtpDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(6, MinimumLength = 6)]
        public string OTP { get; set; } = string.Empty;
    }

    public class ResetPasswordDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string OTP { get; set; } = string.Empty;

        [Required, MinLength(8)]
        public string NewPassword { get; set; } = string.Empty;
    }

    public class RefreshTokenDto
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
