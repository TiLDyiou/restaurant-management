using Microsoft.EntityFrameworkCore;
using RestaurantManagementAPI.Data;
using RestaurantManagementAPI.DTOs.ReportDtos;
using RestaurantManagementAPI.Services.Interfaces;

namespace RestaurantManagementAPI.Services.Implements
{
    public class ReportService : IReportService
    {
        private readonly QLNHDbContext _context;

        public ReportService(QLNHDbContext context)
        {
            _context = context;
        }

        public async Task<RevenueReportResponse> GetRevenueReportAsync(DateTime startDate, DateTime endDate)
        {
            // Chuẩn hóa thời gian (Từ đầu ngày start đến cuối ngày end)
            var start = startDate.Date;
            var end = endDate.Date.AddDays(1).AddTicks(-1);

            // Lấy dữ liệu trong khoảng thời gian
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

            // Tính Trend (So sánh với kỳ trước đó)
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
                trend = 100; // Nếu kỳ trước 0 mà kỳ này có tiền thì tăng 100% (hoặc vô cực)

            // Tính biểu đồ doanh thu theo ngày (Daily Revenues)
            // Tạo danh sách tất cả các ngày trong range (để ngày nào không có đơn vẫn hiện doanh thu = 0)
            var allDates = Enumerable.Range(0, 1 + (end.Date - start.Date).Days)
                                     .Select(offset => start.Date.AddDays(offset))
                                     .ToList();

            var dailyRevenues = allDates.GroupJoin(
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

            // Tính Top Nhân viên xuất sắc
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

            // Lấy danh sách giao dịch gần đây
            var recentTransactions = ordersInRange
                .OrderByDescending(o => o.NgayLap)
                .Take(10) // Chỉ lấy 10 đơn gần nhất để hiển thị list
                .Select(o => new TransactionDetailDto
                {
                    MaHD = o.MaHD,
                    ThoiGian = o.NgayLap!.Value,
                    TongTien = o.TongTien,
                    TrangThai = o.TrangThai
                })
                .ToList();

            // Đóng gói kết quả
            return new RevenueReportResponse
            {
                TotalRevenue = totalRevenue,
                TotalOrders = totalOrders,
                AverageOrderValue = avgOrderValue,
                RevenueTrend = Math.Round(trend, 2),
                DailyRevenues = dailyRevenues,
                TopEmployees = topEmployees,
                RecentTransactions = recentTransactions
            };
        }
    }
}