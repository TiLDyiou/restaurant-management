using RestaurantManagementAPI.DTOs.MonAnDtos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RestaurantManagementAPI.Services
{
    public interface IOrderService
    {
        // Lấy danh sách đơn
        Task<IEnumerable<HoaDonDto>> GetOrdersAsync();

        // Lấy đơn theo ID
        Task<HoaDonDto> GetOrderByIdAsync(string id);

        // Tạo đơn mới
        Task<HoaDonDto> CreateOrderAsync(CreateHoaDonDto createDto);

        // Cập nhật trạng thái 1 món
        Task UpdateOrderItemStatusAsync(string maHD, string maMA, string newStatus);

        // Cập nhật trạng thái cả đơn
        Task UpdateOrderStatusAsync(string id, string newStatus);

        // Thanh toán
        Task<HoaDonDto> CheckoutAsync(string maHD, CheckoutRequestDto checkoutDto);
    }
}