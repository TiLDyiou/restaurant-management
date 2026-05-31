using RestaurantManagementAPI.Common.Wrappers;
using RestaurantManagementAPI.DTOs;
using RestaurantManagementAPI.DTOs.MonAnDtos;

namespace RestaurantManagementAPI.Interfaces
{
    public interface IOrderService
    {
        Task<ServiceResult<List<HoaDonDto>>> GetOrdersAsync();
        Task<ServiceResult<HoaDonDto>> GetOrderByIdAsync(string id);
        Task<ServiceResult<HoaDonDto>> CreateOrderAsync(CreateHoaDonDto createDto);
        Task<ServiceResult> UpdateOrderItemStatusAsync(string maHD, string maMA, string newStatus);
        Task<ServiceResult> UpdateOrderStatusAsync(string id, string newStatus);
        Task<ServiceResult<HoaDonDto>> CheckoutAsync(string maHD, CheckoutRequestDto checkoutDto);
    }
}