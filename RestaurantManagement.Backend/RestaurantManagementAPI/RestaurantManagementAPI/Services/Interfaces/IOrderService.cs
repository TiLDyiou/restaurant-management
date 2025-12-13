using RestaurantManagementAPI.DTOs;
using RestaurantManagementAPI.DTOs.MonAnDtos;

namespace RestaurantManagementAPI.Services.Interfaces
{
    public interface IOrderService
    {
        Task<List<HoaDonDto>> GetOrdersAsync();
        Task<HoaDonDto?> GetOrderByIdAsync(string id);
        Task<(bool Success, string Message, HoaDonDto? Data)> CreateOrderAsync(CreateHoaDonDto createDto);
        Task<(bool Success, string Message)> UpdateOrderItemStatusAsync(string maHD, string maMA, string newStatus);
        Task<(bool Success, string Message)> UpdateOrderStatusAsync(string id, string newStatus);
        Task<(bool Success, string Message, HoaDonDto? Data)> CheckoutAsync(string maHD, CheckoutRequestDto checkoutDto);
    }
}