using RestaurantManagementAPI.DTOs;

namespace RestaurantManagementAPI.Services.Interfaces
{
    public interface IDishService
    {
        Task<List<MonAnDto>> GetAllDishesAsync();
        Task<MonAnDto?> GetDishByIdAsync(string maMA);
        Task<(bool Success, string Message, MonAnDto? Data)> CreateDishAsync(CreateMonAnDto dto);
        Task<(bool Success, string Message)> UpdateDishAsync(string maMA, UpdateMonAnDto dto);
        Task<(bool Success, string Message)> SoftDeleteDishAsync(string maMA);
    }
}