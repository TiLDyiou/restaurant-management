using RestaurantManagementAPI.Common.Wrappers;
using RestaurantManagementAPI.DTOs;

namespace RestaurantManagementAPI.Interfaces
{
    public interface IUserService
    {
        Task<ServiceResult<object>> GetUserProfileAsync(string username);
        Task<ServiceResult<List<object>>> GetAllUsersAsync();
        Task<ServiceResult<object>> UpdateUserAsync(string requesterUsername, bool isAdmin, string? targetMaNV, UpdateUserDto dto);
        Task<ServiceResult> VerifyEmailOtpAsync(string email, string otp);
        Task<ServiceResult> ResendEmailOtpAsync(string email);
        Task<ServiceResult<object>> ToggleUserStatusAsync(string maNV);
        Task<ServiceResult> HardDeleteUserAsync(string maNV);
    }
}