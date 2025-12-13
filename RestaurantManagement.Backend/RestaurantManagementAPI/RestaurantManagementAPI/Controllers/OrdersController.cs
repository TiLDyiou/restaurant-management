using Microsoft.AspNetCore.Mvc;
using RestaurantManagementAPI.DTOs;
using RestaurantManagementAPI.DTOs.MonAnDtos;
using RestaurantManagementAPI.Services.Interfaces;

namespace RestaurantManagementAPI.Controllers
{
    [Route("api/orders")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpGet]
        public async Task<IActionResult> GetOrders()
        {
            var data = await _orderService.GetOrdersAsync();
            return Ok(new { success = true, data });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(string id)
        {
            var data = await _orderService.GetOrderByIdAsync(id);
            if (data == null) return NotFound(new { success = false, message = "Không tìm thấy hóa đơn" });
            return Ok(new { success = true, data });
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateHoaDonDto createDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors = ModelState });

            var result = await _orderService.CreateOrderAsync(createDto);

            return result.Success
                ? Ok(new { success = true, message = result.Message, data = result.Data })
                : BadRequest(new { success = false, message = result.Message });
        }

        [HttpPut("{maHD}/items/{maMA}/status")]
        public async Task<IActionResult> UpdateOrderItemStatus(string maHD, string maMA, [FromBody] UpdateOrderItemStatusDto updateDto)
        {
            var result = await _orderService.UpdateOrderItemStatusAsync(maHD, maMA, updateDto.NewStatus);

            return result.Success
                ? Ok(new { success = true, message = result.Message })
                : BadRequest(new { success = false, message = result.Message });
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(string id, [FromBody] UpdateOrderStatusDto updateDto)
        {
            var result = await _orderService.UpdateOrderStatusAsync(id, updateDto.NewStatus);

            return result.Success
                ? Ok(new { success = true, message = result.Message })
                : BadRequest(new { success = false, message = result.Message });
        }

        [HttpPost("{id}/checkout")]
        public async Task<IActionResult> Checkout(string id, [FromBody] CheckoutRequestDto checkoutDto)
        {
            var result = await _orderService.CheckoutAsync(id, checkoutDto);

            return result.Success
                ? Ok(new { success = true, message = result.Message, data = result.Data })
                : BadRequest(new { success = false, message = result.Message });
        }
    }
}