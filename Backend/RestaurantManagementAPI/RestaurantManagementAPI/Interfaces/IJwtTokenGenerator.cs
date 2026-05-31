using RestaurantManagementAPI.Models.Entities;

namespace RestaurantManagementAPI.Interfaces
{
    public interface IJwtTokenGenerator
    {
        string GenerateAccessToken(TaiKhoan user);
        string GenerateRefreshToken();
    }
}
