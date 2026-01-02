using Microsoft.EntityFrameworkCore;
using RestaurantManagementAPI.Common.Wrappers;
using RestaurantManagementAPI.Data;
using RestaurantManagementAPI.DTOs.ReportDtos;
using RestaurantManagementAPI.Interfaces;

namespace RestaurantManagementAPI.Services
{
    public class ReportService : IReportService
    {
        private readonly QLNHDbContext _context;

        public ReportService(QLNHDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceResult<RevenueReportResponse>> GetRevenueReportAsync(DateTime startDate, DateTime endDate, string groupBy = "day")
        {
            try
            {
                // Chuẩn hóa thời gian
                var start = startDate.Date; // 00:00:00 của ngày bắt đầu
                var end = endDate.Date.AddDays(1).AddTicks(-1); // 23:59:59.9999999 của ngày kết thúc

                // Lấy dữ liệu thô (Raw Data)
                var ordersInRange = await _context.HOADON
                    .Where(o => o.NgayLap.HasValue &&
                                o.NgayLap.Value >= start &&
                                o.NgayLap.Value <= end &&
                                o.TrangThai == "Đã thanh toán")
                    .Include(o => o.NhanVien)
                    .ToListAsync();

                // Tính toán tổng quan
                var totalRevenue = ordersInRange.Sum(o => o.TongTien);
                var totalOrders = ordersInRange.Count;
                var avgOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;

                // Tính Trend
                var rangeDuration = end - start;
                var prevStart = start.Subtract(rangeDuration);
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


                // Xử lý Biểu đồ (Daily/Monthly Revenues)
                List<DailyRevenueDto> chartData = new();

                if (groupBy.ToLower() == "month")
                {
                    // Group theo tháng
                    var currentMonth = new DateTime(start.Year, start.Month, 1);
                    var endMonth = new DateTime(end.Year, end.Month, 1);
                    var allMonths = new List<DateTime>();
                    while (currentMonth <= endMonth)
                    {
                        allMonths.Add(currentMonth);
                        currentMonth = currentMonth.AddMonths(1);
                    }

                    chartData = allMonths.GroupJoin(
                        ordersInRange,
                        month => new { month.Year, month.Month },
                        order => new { order.NgayLap!.Value.Year, order.NgayLap!.Value.Month },
                        (month, orders) => new DailyRevenueDto
                        {
                            Date = month,
                            Revenue = orders.Sum(o => o.TongTien),
                            OrderCount = orders.Count()
                        }
                    ).OrderBy(d => d.Date).ToList();
                }
                else
                {
                    // Group theo ngày
                    var allDates = Enumerable.Range(0, 1 + (end.Date - start.Date).Days)
                                             .Select(offset => start.Date.AddDays(offset))
                                             .ToList();

                    chartData = allDates.GroupJoin(
                        ordersInRange,
                        date => date,
                        order => order.NgayLap!.Value.Date,
                        (date, orders) => new DailyRevenueDto
                        {
                            Date = date,
                            Revenue = orders.Sum(o => o.TongTien),
                            OrderCount = orders.Count()
                        }
                    ).OrderBy(d => d.Date).ToList();
                }

                // Tính Top Nhân viên & Giao dịch gần đây
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

                var recentTransactions = ordersInRange
                    .OrderByDescending(o => o.NgayLap)
                    .Take(10)
                    .Select(o => new TransactionDetailDto
                    {
                        MaHD = o.MaHD,
                        ThoiGian = o.NgayLap!.Value,
                        TongTien = o.TongTien,
                        TrangThai = o.TrangThai
                    })
                    .ToList();

                var response = new RevenueReportResponse
                {
                    TotalRevenue = totalRevenue,
                    TotalOrders = totalOrders,
                    AverageOrderValue = avgOrderValue,
                    RevenueTrend = Math.Round(trend, 2),
                    DailyRevenues = chartData,
                    TopEmployees = topEmployees,
                    RecentTransactions = recentTransactions
                };
                return ServiceResult<RevenueReportResponse>.Ok(response);
            }
            catch (Exception ex)
            {
                return ServiceResult<RevenueReportResponse>.Fail("Lỗi lấy báo cáo: " + ex.Message);
            }
        }
    }
}