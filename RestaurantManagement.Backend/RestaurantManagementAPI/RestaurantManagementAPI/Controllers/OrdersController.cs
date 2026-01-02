using Microsoft.AspNetCore.Mvc;
using RestaurantManagementAPI.DTOs.MonAnDtos;
using RestaurantManagementAPI.Interfaces;

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
            var result = await _orderService.GetOrdersAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(string id)
        {
            var result = await _orderService.GetOrderByIdAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateHoaDonDto createDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _orderService.CreateOrderAsync(createDto);
            
            if (!result.Success)
                return BadRequest(result);
            return CreatedAtAction
            (
                nameof(GetOrderById), 
                new { id = result.Data?.MaHD }, 
                result
            );
        }

        [HttpPut("{maHD}/items/{maMA}/status")]
        public async Task<IActionResult> UpdateOrderItemStatus(string maHD, string maMA, [FromBody] UpdateOrderItemStatusDto updateDto)
        {
            var result = await _orderService.UpdateOrderItemStatusAsync(maHD, maMA, updateDto.NewStatus);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(string id, [FromBody] UpdateOrderStatusDto updateDto)
        {
            var result = await _orderService.UpdateOrderStatusAsync(id, updateDto.NewStatus);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("{id}/checkout")]
        public async Task<IActionResult> Checkout(string id, [FromBody] CheckoutRequestDto checkoutDto)
        {
            var result = await _orderService.CheckoutAsync(id, checkoutDto);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}