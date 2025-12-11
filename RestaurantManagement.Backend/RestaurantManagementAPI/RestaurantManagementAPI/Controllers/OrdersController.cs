using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Cần cái này để dùng .ToListAsync, .SumAsync
using RestaurantManagementAPI.Data; // Cần cái này để dùng QLNHDbContext
using RestaurantManagementAPI.DTOs.MonAnDtos;
using RestaurantManagementAPI.Services;
using RestaurantManagementAPI.Models.Entities; // Cần cái này để dùng HoaDon, NhanVien
using RestaurantManagementAPI.DTOs; // Namespace chứa các DTO báo cáo (RevenueReportResponse...)
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq; // Cần cái này để dùng LINQ

namespace RestaurantManagementAPI.Controllers
{
    [Route("api/orders")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly QLNHDbContext _context; // 1. Khai báo thêm Context

        // 2. Inject thêm QLNHDbContext vào Constructor
        public OrdersController(IOrderService orderService, QLNHDbContext context)
        {
            _orderService = orderService;
            _context = context;
        }

        // ==========================================
        // PHẦN BÁO CÁO DOANH THU (MỚI THÊM VÀO)
        // ==========================================

        // GET: /api/orders/revenue-report
        [HttpGet("revenue-report")]
        public async Task<IActionResult> GetRevenueReport([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            try
            {
                var start = startDate.Date;
                var end = endDate.Date.AddDays(1).AddTicks(-1);

                // Lấy dữ liệu từ DB (dùng _context)
                var ordersInRange = await _context.HOADON
                    .Where(o => o.NgayLap.HasValue &&
                                o.NgayLap.Value >= start &&
                                o.NgayLap.Value <= end &&
                                o.TrangThai == "Đã thanh toán") // Chỉ lấy đơn đã thanh toán
                    .Include(o => o.NhanVien)
                    .ToListAsync();

                // Tính tổng quan
                var totalRevenue = ordersInRange.Sum(o => o.TongTien);
                var totalOrders = ordersInRange.Count;
                var avgOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;

                // Tính Trend (So sánh kỳ trước)
                var daysDiff = (end - start).TotalDays;
                var prevStart = start.AddDays(-daysDiff);
                var prevEnd = start.AddTicks(-1);

                var prevRevenue = await _context.HOADON
                    .Where(o => o.NgayLap.HasValue &&
                                o.NgayLap.Value >= prevStart &&
                                o.NgayLap.Value <= prevEnd &&
                                o.TrangThai == "Đã thanh toán")
                    .SumAsync(o => o.TongTien);

                decimal trend = 0;
                if (prevRevenue > 0)
                    trend = ((totalRevenue - prevRevenue) / prevRevenue) * 100;
                else if (totalRevenue > 0)
                    trend = 100;

                // Tính Daily Revenues
                var allDates = Enumerable.Range(0, 1 + end.Subtract(start).Days)
                                         .Select(offset => start.AddDays(offset))
                                         .ToList();

                var dailyRevenues = allDates.GroupJoin(
                        ordersInRange,
                        date => date,
                        order => order.NgayLap.Value.Date,
                        (date, orders) => new DailyRevenueDto
                        {
                            Date = date,
                            Revenue = orders.Sum(o => o.TongTien),
                            OrderCount = orders.Count()
                        }
                    )
                    .OrderBy(d => d.Date)
                    .ToList();

                // Tính Top Employees
                var topEmployees = ordersInRange
                    .GroupBy(o => o.NhanVien != null ? o.NhanVien.HoTen : "Không xác định")
                    .Select(g => new EmployeePerformanceDto
                    {
                        EmployeeName = g.Key,
                        OrdersServed = g.Count(),
                        TotalRevenue = g.Sum(o => o.TongTien)
                    })
                    .OrderByDescending(e => e.TotalRevenue)
                    .Take(5)
                    .ToList();

                var result = new RevenueReportResponse
                {
                    TotalRevenue = totalRevenue,
                    TotalOrders = totalOrders,
                    AverageOrderValue = avgOrderValue,
                    RevenueTrend = trend,
                    DailyRevenues = dailyRevenues,
                    TopEmployees = topEmployees
                };

                return Ok(new { Success = true, Message = "Lấy dữ liệu thành công", Data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi Server: " + ex.Message });
            }
        }

        // ==========================================
        // PHẦN CODE CŨ CỦA BẠN (GIỮ NGUYÊN)
        // ==========================================

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

        // PUT: /api/orders/update-dishes-status
        [HttpPut("update-dishes-status")]
        public async Task<IActionResult> UpdateOrderItemStatus(string maHD, string maMA, [FromBody] UpdateOrderItemStatusDto updateDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                await _orderService.UpdateOrderItemStatusAsync(maHD, maMA, updateDto.NewStatus);
                return NoContent();
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
                // LƯU Ý: Đảm bảo hàm CheckoutAsync trong Service của bạn 
                // đã cập nhật TrangThai = "Đã thanh toán" và NgayLap = DateTime.Now 
                // thì báo cáo mới hiện dữ liệu nhé!
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