using RestaurantManagementAPI.DTOs.ReportDtos;

namespace RestaurantManagementAPI.Services.Interfaces
{
    public interface IReportService
    {
        // groupBy có thể nhận: "day" hoặc "month"
        Task<RevenueReportResponse> GetRevenueReportAsync(DateTime startDate, DateTime endDate, string groupBy = "day");
    }
}