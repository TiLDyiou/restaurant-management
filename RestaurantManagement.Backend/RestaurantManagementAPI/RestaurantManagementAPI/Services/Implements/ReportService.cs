using Microsoft.EntityFrameworkCore;
using RestaurantManagementAPI.Data;
using RestaurantManagementAPI.DTOs.ReportDtos;
using RestaurantManagementAPI.Services.Interfaces;
using System.Globalization;

namespace RestaurantManagementAPI.Services.Implements
{
    public class ReportService : IReportService
    {
        private readonly QLNHDbContext _context;

        public ReportService(QLNHDbContext context)
        {
            _context = context;
        }

        public async Task<RevenueReportResponse> GetRevenueReportAsync(DateTime startDate, DateTime endDate, string groupBy = "day")
        {
            // 1. Chuẩn hóa thời gian
            var start = startDate.Date;
            var end = endDate.Date.AddDays(1).AddTicks(-1);

            // 2. Lấy dữ liệu thô (Raw Data)
            var ordersInRange = await _context.HOADON
                .Where(o => o.NgayLap.HasValue &&
                            o.NgayLap.Value >= start &&
                            o.NgayLap.Value <= end &&
                            o.TrangThai == "Đã thanh toán")
                .Include(o => o.NhanVien)
                .ToListAsync();

            // 3. Tính toán tổng quan (Giữ nguyên)
            var totalRevenue = ordersInRange.Sum(o => o.TongTien);
            var totalOrders = ordersInRange.Count;
            var avgOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;

            // 4. Tính Trend (Giữ nguyên - so sánh với kỳ trước cùng độ dài)
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

            // 5. Xử lý Biểu đồ (Daily/Monthly Revenues)
            List<DailyRevenueDto> chartData = new();

            if (groupBy.ToLower() == "month")
            {
                // LOGIC NHÓM THEO THÁNG 

                // Tạo danh sách các tháng trong khoảng thời gian (để tháng nào 0 doanh thu vẫn hiện)
                var currentMonth = new DateTime(start.Year, start.Month, 1);
                var endMonth = new DateTime(end.Year, end.Month, 1);

                var allMonths = new List<DateTime>();
                while (currentMonth <= endMonth)
                {
                    allMonths.Add(currentMonth);
                    currentMonth = currentMonth.AddMonths(1);
                }

                // Group dữ liệu order theo tháng
                chartData = allMonths.GroupJoin(
                    ordersInRange,
                    month => new { month.Year, month.Month }, // Key bên trái
                    order => new { order.NgayLap!.Value.Year, order.NgayLap!.Value.Month }, // Key bên phải
                    (month, orders) => new DailyRevenueDto
                    {
                        // Trả về ngày đầu tháng để Frontend dễ format (ví dụ: 01/01/2024 -> hiển thị thàng "T1/2024")
                        Date = month,
                        Revenue = orders.Sum(o => o.TongTien),
                        OrderCount = orders.Count()
                    }
                )
                .OrderBy(d => d.Date)
                .ToList();
            }
            else
            {
                //  LOGIC NHÓM THEO NGÀY 
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
                )
                .OrderBy(d => d.Date)
                .ToList();
            }

            // 6. Tính Top Nhân viên & Giao dịch gần đây
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

            return new RevenueReportResponse
            {
                TotalRevenue = totalRevenue,
                TotalOrders = totalOrders,
                AverageOrderValue = avgOrderValue,
                RevenueTrend = Math.Round(trend, 2),
                DailyRevenues = chartData, // Dữ liệu này giờ có thể là Ngày hoặc Tháng
                TopEmployees = topEmployees,
                RecentTransactions = recentTransactions
            };
        }
    }
}