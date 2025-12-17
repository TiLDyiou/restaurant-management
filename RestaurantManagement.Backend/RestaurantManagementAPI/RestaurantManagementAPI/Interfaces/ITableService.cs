using RestaurantManagementAPI.Common.Wrappers;
using RestaurantManagementAPI.Models.Entities;

namespace RestaurantManagementAPI.Interfaces
{
    public interface ITableService
    {
        Task<ServiceResult<List<Ban>>> GetAllBanAsync();
        Task<ServiceResult<Ban>> UpdateStatusAsync(string maBan, string trangThai);
    }
}