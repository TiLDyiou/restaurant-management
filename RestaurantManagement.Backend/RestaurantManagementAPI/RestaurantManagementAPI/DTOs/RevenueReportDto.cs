namespace RestaurantManagementAPI.DTOs.ReportDtos
{
    public class RevenueReportResponse
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public decimal AverageOrderValue { get; set; }
        public decimal RevenueTrend { get; set; }
        public List<DailyRevenueDto> DailyRevenues { get; set; } = new();
        public List<EmployeePerformanceDto> TopEmployees { get; set; } = new();
        public List<TransactionDetailDto> RecentTransactions { get; set; } = new();
    }

    public class DailyRevenueDto
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }

    public class EmployeePerformanceDto
    {
        public string EmployeeName { get; set; } = string.Empty;
        public int OrdersServed { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class TransactionDetailDto
    {
        public string MaHD { get; set; } = string.Empty;
        public DateTime ThoiGian { get; set; }
        public decimal TongTien { get; set; }
        public string? TrangThai { get; set; }
    }
}