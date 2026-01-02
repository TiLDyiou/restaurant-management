using RestaurantManagementAPI.Common.Wrappers;
using RestaurantManagementAPI.DTOs;

namespace RestaurantManagementAPI.Interfaces
{
    public interface IAuthService
    {
        Task<ServiceResult<string>> RegisterAsync(RegisterDto dto);
        Task<ServiceResult<object>> LoginAsync(LoginDto dto);
        Task<ServiceResult> SendRegisterOtpAsync(string email);
        Task<ServiceResult> VerifyRegisterOtpAsync(string email, string otp);
        Task<ServiceResult> ForgotPasswordAsync(string email);
        Task<ServiceResult> VerifyForgotOtpAsync(string email, string otp);
        Task<ServiceResult> ResetPasswordAsync(ResetPasswordDto dto);
        Task<ServiceResult> LogoutAsync(string maNV);
    }
}