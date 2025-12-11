using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementAPI.Data;
using RestaurantManagementAPI.DTOs.MonAnDtos;
using RestaurantManagementAPI.Services;
using RestaurantManagementAPI.Models.Entities;
using RestaurantManagementAPI.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace RestaurantManagementAPI.Controllers
{
    [Route("api/orders")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly QLNHDbContext _context;

        public OrdersController(IOrderService orderService, QLNHDbContext context)
        {
            _orderService = orderService;
            _context = context;
        }

        // ============================================================
        // PHẦN BÁO CÁO DOANH THU (ĐÃ SỬA HOÀN CHỈNH)
        // ============================================================

        // GET: /api/orders/revenue-report
        [HttpGet("revenue-report")]
        public async Task<IActionResult> GetRevenueReport(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] string? maNV = null) // Tham số lọc theo nhân viên
        {
            try
            {
                var start = startDate.Date;
                var end = endDate.Date.AddDays(1).AddTicks(-1);

                // 1. QUERY CƠ BẢN (Lấy danh sách hóa đơn đã thanh toán)
                var query = _context.HOADON
                    .Where(o => o.NgayLap.HasValue &&
                                o.NgayLap.Value >= start &&
                                o.NgayLap.Value <= end &&
                                o.TrangThai == "Đã thanh toán")
                    .Include(o => o.NhanVien)
                    .AsQueryable();

                // 2. PHÂN QUYỀN: Nếu có mã NV -> Lọc theo người đó
                if (!string.IsNullOrEmpty(maNV))
                {
                    query = query.Where(o => o.MaNV == maNV);
                }

                var ordersInRange = await query.ToListAsync();

                // 3. TÍNH TOÁN TỔNG QUAN
                var totalRevenue = ordersInRange.Sum(o => o.TongTien);
                var totalOrders = ordersInRange.Count;
                var avgOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;

                // 4. TÍNH TREND (So sánh kỳ trước)
                var daysDiff = (end - start).TotalDays;
                var prevStart = start.AddDays(-daysDiff);
                var prevEnd = start.AddTicks(-1);

                var prevQuery = _context.HOADON
                    .Where(o => o.NgayLap.HasValue &&
                                o.NgayLap.Value >= prevStart &&
                                o.NgayLap.Value <= prevEnd &&
                                o.TrangThai == "Đã thanh toán");

                if (!string.IsNullOrEmpty(maNV)) prevQuery = prevQuery.Where(o => o.MaNV == maNV);

                var prevRevenue = await prevQuery.SumAsync(o => o.TongTien);
                decimal trend = 0;
                if (prevRevenue > 0) trend = ((totalRevenue - prevRevenue) / prevRevenue) * 100;
                else if (totalRevenue > 0) trend = 100;

                // 5. BIỂU ĐỒ DOANH THU (Daily Revenues)
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
                    .OrderBy(d => d.Date).ToList();

                // 6. TOP EMPLOYEES (Dành cho Admin)
                var topEmployees = ordersInRange
                    .GroupBy(o => o.NhanVien != null ? o.NhanVien.HoTen : "Unknown")
                    .Select(g => new EmployeePerformanceDto
                    {
                        EmployeeName = g.Key,
                        OrdersServed = g.Count(),
                        TotalRevenue = g.Sum(o => o.TongTien)
                    })
                    .OrderByDescending(e => e.TotalRevenue).Take(5).ToList();

                // 7. RECENT TRANSACTIONS (Dành cho Nhân viên) -> MỚI THÊM
                var recentTransactions = new List<TransactionDetailDto>();
                if (!string.IsNullOrEmpty(maNV))
                {
                    recentTransactions = ordersInRange
                        .OrderByDescending(o => o.NgayLap)
                        .Take(10) // Lấy 10 đơn gần nhất
                        .Select(o => new TransactionDetailDto
                        {
                            MaHD = o.MaHD,
                            ThoiGian = o.NgayLap.Value,
                            TongTien = o.TongTien,
                            TrangThai = o.TrangThai
                        }).ToList();
                }

                // Trả kết quả
                var result = new RevenueReportResponse
                {
                    TotalRevenue = totalRevenue,
                    TotalOrders = totalOrders,
                    AverageOrderValue = avgOrderValue,
                    RevenueTrend = trend,
                    DailyRevenues = dailyRevenues,
                    TopEmployees = topEmployees,
                    RecentTransactions = recentTransactions // List này sẽ có dữ liệu nếu là NV
                };

                return Ok(new { Success = true, Message = "Lấy dữ liệu thành công", Data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi Server: " + ex.Message });
            }
        }

        // ==========================================
        // CÁC HÀM CŨ (ORDER SERVICE) - GIỮ NGUYÊN
        // ==========================================

        [HttpGet("get-all-orders-info")]
        public async Task<ActionResult<IEnumerable<HoaDonDto>>> GetOrders()
        {
            var result = await _orderService.GetOrdersAsync();
            return Ok(result);
        }

        [HttpGet("get-{id}-order-info")]
        public async Task<ActionResult<HoaDonDto>> GetOrderById(string id)
        {
            var result = await _orderService.GetOrderByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

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