using RestaurantManagementAPI.Models.Entities;
namespace RestaurantManagementAPI.Interfaces
{
    public interface IJwtTokenGenerator
    {
        string GenerateToken(TaiKhoan user);
    }
}