using Microsoft.AspNetCore.Mvc;
using RestaurentManagementAPI.DTOs.MonAnDtos;
using RestaurentManagementAPI.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace RestaurentManagementAPI.Controllers
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

        // GET: /api/orders/get-all-orders-info
        [HttpGet("get-all-orders-info")]
        public async Task<ActionResult<IEnumerable<HoaDonDto>>> GetOrders()
        {
            var result = await _orderService.GetOrdersAsync();
            return Ok(result);
        }

        // GET: /api/orders/get-{id}-order-info
        [HttpGet("get-{id}-order-info")]
        public async Task<ActionResult<HoaDonDto>> GetOrderById(string id)
        {
            var result = await _orderService.GetOrderByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        // POST: /api/orders/api/create-and-send-orders
        [HttpPost("api/create-and-send-orders")]
        public async Task<ActionResult<HoaDonDto>> CreateOrder([FromBody] CreateHoaDonDto createDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var result = await _orderService.CreateOrderAsync(createDto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Lỗi Server: " + ex.Message);
            }
        }

                // 5. Lưu tất cả thay đổi
                await _context.SaveChangesAsync();

                // 6. Commit transaction
                await transaction.CommitAsync();
                hoaDonDtoToReturn = new HoaDonDto
                {
                    MaHD = hoaDon.MaHD,
                    MaBan = hoaDon.MaBan,
                    MaNV = hoaDon.MaNV,
                    NgayLap = hoaDon.NgayLap,
                    TongTien = hoaDon.TongTien,
                    TrangThai = hoaDon.TrangThai, // Trạng thái tổng
                    ChiTietHoaDons = chiTietEntities.Select(ct => new ChiTietHoaDonViewDto
                    {
                        MaMA = ct.MaMA,
                        TenMA = _context.MONAN.Find(ct.MaMA)?.TenMA ?? "N/A",
                        SoLuong = ct.SoLuong,
                        DonGia = ct.DonGia,
                        // ThanhTien sẽ là 0 vì chưa có trong DB,
                        // Cần tính thủ công nếu muốn hiển thị ngay
                        ThanhTien = ct.SoLuong * ct.DonGia,

                        // GỬI TRẠNG THÁI MÓN ĂN:
                        TrangThai = ct.TrangThai
                    }).ToList()
                };

                // Gửi qua SignalR
                await _kitchenHubContext.Clients.Group("Kitchen")
                    .SendAsync("ReceiveOrder", hoaDonDtoToReturn);

                // Lấy lại dữ liệu vừa tạo để trả về (nếu cần)
                // (Bỏ qua bước này để đơn giản, trả về CreatedAtAction)

                return CreatedAtAction(nameof(GetOrderById), new { id = hoaDon.MaHD }, hoaDonDtoToReturn);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // PUT: /api/orders/update-all-dishes-in-{id}-order-status
        [HttpPut("update-all-dishes-in-{id}-order-status")]
        public async Task<IActionResult> UpdateOrderStatus(string id, [FromBody] UpdateOrderStatusDto updateDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                await _orderService.UpdateOrderStatusAsync(id, updateDto.NewStatus);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // PUT: /api/orders/checkout/{maHD}
        [HttpPut("checkout/{maHD}")]
        public async Task<IActionResult> Checkout(string maHD, [FromBody] CheckoutRequestDto checkoutDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var result = await _orderService.CheckoutAsync(maHD, checkoutDto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}