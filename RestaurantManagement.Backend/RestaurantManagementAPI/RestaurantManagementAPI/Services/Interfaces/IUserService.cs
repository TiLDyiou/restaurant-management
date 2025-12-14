using RestaurantManagementAPI.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RestaurantManagementAPI.Services.Interfaces
{
    public interface IUserService
    {
        Task<object?> GetUserProfileAsync(string username);
        Task<List<object>> GetAllUsersAsync();
        Task<(bool Success, string Message, object? Data)> UpdateUserAsync(string requesterUsername, bool isAdmin, string? targetMaNV, UpdateUserDto dto);
        Task<(bool Success, string Message)> VerifyEmailOtpAsync(string email, string otp);
        Task<(bool Success, string Message)> ResendEmailOtpAsync(string email);
        Task<(bool Success, string Message, object? Data)> ToggleUserStatusAsync(string maNV);
        Task<(bool Success, string Message)> HardDeleteUserAsync(string maNV);
    }
}