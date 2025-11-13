namespace RestaurantManagementGUI.Models
{
    // Dùng để gửi email khi yêu cầu OTP
    public class EmailDto
    {
        public string Email { get; set; }
    }

    // Dùng để verify OTP (cả email và OTP)
    public class VerifyOtpDto
    {
        public string Email { get; set; }
        public string OTP { get; set; }
    }

    // Dùng để reset password (email + OTP + mật khẩu mới)
    public class ResetPasswordDto
    {
        public string Email { get; set; }
        public string OTP { get; set; }
        public string NewPassword { get; set; }
    }
}
