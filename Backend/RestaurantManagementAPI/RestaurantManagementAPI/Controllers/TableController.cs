using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantManagementAPI.DTOs;
using RestaurantManagementAPI.Interfaces;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RestaurantManagementAPI.Controllers
{
    [Authorize]
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
        public async Task<IActionResult> GetBan([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _banService.GetAllBanAsync(pageNumber, pageSize);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetBanById(string id)
        {
            var result = await _banService.GetBanByIdAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateBan([FromBody] CreateBanDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var maNV = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _banService.CreateBanAsync(dto, maNV);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBan(string id, [FromBody] UpdateBanDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var maNV = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _banService.UpdateBanAsync(id, dto, maNV);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteBan(string id)
        {
            var maNV = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _banService.DeleteBanAsync(id, maNV);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] string trangThai)
        {
            var maNV = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _banService.UpdateStatusAsync(id, trangThai, maNV);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("merge")]
        public async Task<IActionResult> MergeTables([FromBody] MergeTablesDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var maNV = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _banService.MergeTablesAsync(dto, maNV);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("{id}/split")]
        public async Task<IActionResult> SplitTables(string id)
        {
            var maNV = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _banService.SplitTablesAsync(id, maNV);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("transfer")]
        public async Task<IActionResult> TransferOrder([FromBody] TransferOrderDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var maNV = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _banService.TransferOrderAsync(dto, maNV);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("{id}/history")]
        public async Task<IActionResult> GetTableHistory(string id)
        {
            var result = await _banService.GetTableHistoryAsync(id);
            return Ok(result);
        }
    }
}