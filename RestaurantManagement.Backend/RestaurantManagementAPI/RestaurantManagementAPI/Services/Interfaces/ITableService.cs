using RestaurantManagementAPI.Models.Entities;

namespace RestaurantManagementAPI.Services.Interfaces
{
    public interface ITableService
    {
        Task<List<Ban>> GetAllBanAsync();
        Task<(bool Success, string Message, Ban? Data)> UpdateStatusAsync(string maBan, string trangThai);
    }
}