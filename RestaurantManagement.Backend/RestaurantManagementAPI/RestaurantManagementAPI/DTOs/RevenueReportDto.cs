using System;
using System.Collections.Generic;

namespace RestaurantManagementAPI.DTOs
{
    public class RevenueReportResponse
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public decimal AverageOrderValue { get; set; }
        public decimal RevenueTrend { get; set; }
        public List<DailyRevenueDto> DailyRevenues { get; set; }
        public List<EmployeePerformanceDto> TopEmployees { get; set; }
    }

    public class DailyRevenueDto
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }

    public class EmployeePerformanceDto
    {
        public string EmployeeName { get; set; }
        public int OrdersServed { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}