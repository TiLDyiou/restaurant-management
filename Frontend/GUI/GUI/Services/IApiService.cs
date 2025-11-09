using RestaurantManagementGUI.Models;

namespace RestaurantManagementGUI.Services
{
    public interface IApiService
    {
        // Lấy tất cả các bàn
        Task<List<Ban>> GetTablesAsync();

        // Cập nhật trạng thái của một bàn
        Task<bool> UpdateTableStatusAsync(string maBan, string newStatus);
    }
}