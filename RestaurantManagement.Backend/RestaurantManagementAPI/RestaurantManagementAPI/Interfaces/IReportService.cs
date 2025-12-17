using RestaurantManagementAPI.Common.Wrappers;
using RestaurantManagementAPI.DTOs.ReportDtos;

namespace RestaurantManagementAPI.Interfaces
{
    public interface IReportService
    {
        // groupBy có thể nhận: "day" hoặc "month"
        Task<ServiceResult<RevenueReportResponse>> GetRevenueReportAsync(DateTime startDate, DateTime endDate, string groupBy = "day");
    }
}