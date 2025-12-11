using System;
using System.Collections.Generic;

namespace RestaurantManagementGUI.Models
{
    public class RevenueReportData
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public decimal AverageOrderValue { get; set; }
        public decimal RevenueTrend { get; set; }
        public List<DailyRevenue> DailyRevenues { get; set; } = new();
        public List<EmployeePerformance> TopEmployees { get; set; } = new();
        public List<TransactionDetail> RecentTransactions { get; set; } = new(); // List cho nhân viên
    }

    public class DailyRevenue
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }

    public class EmployeePerformance
    {
        public string EmployeeName { get; set; }
        public int OrdersServed { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class TransactionDetail
    {
        public string MaHD { get; set; }
        public DateTime ThoiGian { get; set; }
        public decimal TongTien { get; set; }
        public string TrangThai { get; set; }
    }
}