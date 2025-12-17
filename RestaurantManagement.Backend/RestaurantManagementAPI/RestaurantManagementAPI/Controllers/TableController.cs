using Microsoft.AspNetCore.Mvc;
using RestaurantManagementAPI.Interfaces;

namespace RestaurantManagementAPI.Controllers
{
    [Route("api/tables")]
    [ApiController]
    public class TableController : ControllerBase
    {
        private readonly ITableService _banService;
        public TableController(ITableService banService) { _banService = banService; }

        [HttpGet]
        public async Task<IActionResult> GetBan()
        {
            var result = await _banService.GetAllBanAsync();
            return Ok(result);
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] string trangThai)
        {
            var result = await _banService.UpdateStatusAsync(id, trangThai);
            return result.Success ? Ok(result) : NotFound(result);
        }
    }
}