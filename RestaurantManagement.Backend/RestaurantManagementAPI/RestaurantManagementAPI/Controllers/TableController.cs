using Microsoft.AspNetCore.Mvc;
using RestaurantManagementAPI.Services.Interfaces;

namespace RestaurantManagementAPI.Controllers
{
    [Route("api/tables")]
    [ApiController]
    public class TableController : ControllerBase
    {
        private readonly ITableService _banService;

        public TableController(ITableService banService)
        {
            _banService = banService;
        }

        [HttpGet]
        public async Task<IActionResult> GetBan()
        {
            var data = await _banService.GetAllBanAsync();
            return Ok(new { success = true, data });
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] string trangThai)
        {
            var result = await _banService.UpdateStatusAsync(id, trangThai);
            return result.Success
                ? Ok(new { success = true, message = result.Message, data = result.Data })
                : NotFound(new { success = false, message = result.Message });
        }
    }
}