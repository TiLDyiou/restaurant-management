using RestaurentManagementAPI.Entities;
using RestaurentManagementAPI.DTOs;

namespace RestaurentManagementAPI.Services
{
    public interface IAuthService
    {
        Task<TaiKhoan?> RegisterAsync(RegisterDto request);
        Task<string?> LoginAsync(LoginDto request);
    }
}
