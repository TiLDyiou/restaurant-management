using RestaurantManagementAPI.DTOs.ReportDtos;

namespace RestaurantManagementAPI.Services.Interfaces
{
    public interface IReportService
    {
        Task<RevenueReportResponse> GetRevenueReportAsync(DateTime startDate, DateTime endDate);
    }
}