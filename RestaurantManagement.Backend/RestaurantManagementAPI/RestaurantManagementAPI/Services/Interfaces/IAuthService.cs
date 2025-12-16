using RestaurantManagementAPI.DTOs;
using System.Threading.Tasks;

namespace RestaurantManagementAPI.Services.Interfaces
{
    public interface IAuthService
    {
        Task<(bool Success, string Message, string? MaNV)> RegisterAsync(RegisterDto dto);
        Task<(bool Success, string Message, object? Data)> LoginAsync(LoginDto dto);
        Task<(bool Success, string Message)> SendRegisterOtpAsync(string email);
        Task<(bool Success, string Message)> VerifyRegisterOtpAsync(string email, string otp);
        Task<(bool Success, string Message)> ForgotPasswordAsync(string email);
        Task<(bool Success, string Message)> VerifyForgotOtpAsync(string email, string otp);
        Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordDto dto);
        Task<(bool Success, string Message)> LogoutAsync(string maNV);
    }
}   