using System.Text.Json.Serialization;

namespace RestaurantManagementGUI.Models
{
    public class RevenueReportDto
    {
        [JsonPropertyName("totalRevenue")]
        public decimal TotalRevenue { get; set; }

        [JsonPropertyName("totalOrders")]
        public int TotalOrders { get; set; }

        [JsonPropertyName("averageOrderValue")]
        public decimal AverageOrderValue { get; set; }

        [JsonPropertyName("revenueTrend")]
        public double RevenueTrend { get; set; }

        [JsonPropertyName("dailyRevenues")]
        public List<DailyRevenueDto> DailyRevenues { get; set; }

        [JsonPropertyName("topEmployees")]
        public List<EmployeePerformanceDto> TopEmployees { get; set; }

        [JsonPropertyName("recentTransactions")]
        public List<TransactionDetailDto> RecentTransactions { get; set; }
    }

    public class DailyRevenueDto
    {
        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("revenue")]
        public decimal Revenue { get; set; }
    }

    public class EmployeePerformanceDto
    {
        [JsonPropertyName("employeeName")]
        public string EmployeeName { get; set; }

        [JsonPropertyName("totalRevenue")]
        public decimal TotalRevenue { get; set; }
    }

    public class TransactionDetailDto
    {
        [JsonPropertyName("maHD")]
        public string MaHD { get; set; }

        [JsonPropertyName("tongTien")]
        public decimal TongTien { get; set; }

        [JsonPropertyName("thoiGian")]
        public DateTime ThoiGian { get; set; }

        [JsonPropertyName("trangThai")]
        public string TrangThai { get; set; }
    }
}