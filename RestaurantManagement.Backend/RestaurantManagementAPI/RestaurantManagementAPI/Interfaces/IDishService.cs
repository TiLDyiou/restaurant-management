using RestaurantManagementAPI.Common.Wrappers;
using RestaurantManagementAPI.DTOs;

namespace RestaurantManagementAPI.Interfaces
{
    public interface IDishService
    {
        Task<ServiceResult<List<MonAnDto>>> GetAllDishesAsync();
        Task<ServiceResult<MonAnDto>> GetDishByIdAsync(string maMA);
        Task<ServiceResult<MonAnDto>> CreateDishAsync(CreateMonAnDto dto);
        Task<ServiceResult> UpdateDishAsync(string maMA, UpdateMonAnDto dto);
        Task<ServiceResult> SoftDeleteDishAsync(string maMA);
    }
}