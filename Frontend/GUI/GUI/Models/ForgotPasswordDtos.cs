
namespace RestaurantManagementGUI.Models
{
    public class EmailDto
    {
        public string Email { get; set; }
    }
    public class VerifyOtpDto
    {
        public string Email { get; set; }
        public string OTP { get; set; }
    }
    public class ResetPasswordDto
    {
        public string Email { get; set; }
        public string OTP { get; set; }
        public string NewPassword { get; set; }
    }
}
