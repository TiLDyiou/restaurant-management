using Microsoft.AspNetCore.Mvc;
using RestaurantManagementAPI.Interfaces;

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

        [HttpGet("revenue")]
        public async Task<IActionResult> GetRevenueReport([FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] string groupBy = "day")
        {
            if (startDate > endDate)
            {
                return BadRequest(new { success = false, message = "Ngày bắt đầu không được lớn hơn ngày kết thúc." });
            }

            var result = await _reportService.GetRevenueReportAsync(startDate, endDate, groupBy);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}