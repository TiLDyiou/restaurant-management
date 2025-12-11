using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementAPI.Data;
using RestaurantManagementAPI.Services;
using System.Text.Json;

namespace RestaurantManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BanController : ControllerBase
    {
        private readonly QLNHDbContext _context;

        public BanController(QLNHDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetBan()
        {
            return Ok(await _context.BAN.ToListAsync());
        }

        [HttpPut("{maBan}/trangthai")]
        public async Task<IActionResult> UpdateStatus(string maBan, [FromBody] string trangThai)
        {
            var ban = await _context.BAN.FindAsync(maBan);
            if (ban == null) return NotFound();

            ban.TrangThai = trangThai;
            await _context.SaveChangesAsync();

            // GỬI SOCKET
            if (TcpSocketServer.Instance != null)
            {
                var payload = new { MaBan = maBan, TrangThai = trangThai };
                string jsonTable = JsonSerializer.Serialize(payload);
                await TcpSocketServer.Instance.BroadcastAsync($"TABLE|{jsonTable}");
            }

            return Ok(ban);
        }
    }
}