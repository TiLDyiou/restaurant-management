using Microsoft.AspNetCore.Mvc;
using RestaurantManagementAPI.Services.Interfaces;

namespace RestaurantManagementAPI.Controllers
{
    [Route("api/reports")] 
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportController(IReportService reportService)
        {
            _reportService = reportService;
        }

        // GET: api/reports/revenue?startDate=2025-11-01&endDate=2025-11-30
        [HttpGet("revenue")]
        public async Task<IActionResult> GetRevenueReport([FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] string groupBy = "day")
        {
            try
            {
                if (startDate > endDate)
                {
                    return BadRequest(new { success = false, message = "Ngày bắt đầu không được lớn hơn ngày kết thúc." });
                }

                var data = await _reportService.GetRevenueReportAsync(startDate, endDate, groupBy);

                return Ok(new
                {
                    success = true,
                    message = "Lấy báo cáo doanh thu thành công",
                    data = data
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi Server: " + ex.Message });
            }
        }
    }
}