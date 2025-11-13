using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RestaurentManagementAPI.Data;
using RestaurentManagementAPI.Models.Entities;
using RestaurentManagementAPI.Hubs;

namespace RestaurentManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BanController : ControllerBase
    {
        private readonly QLNHDbContext _context;
        private readonly IHubContext<BanHub> _hubContext;

        public BanController(QLNHDbContext context, IHubContext<BanHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // GET: api/ban
        [HttpGet]
        public async Task<IActionResult> GetBan()
        {
            var bans = await _context.BAN
                .Select(b => new { b.MaBan, b.TenBan, b.TrangThai })
                .ToListAsync();

            return Ok(bans);
        }

        // PUT: api/ban/{maBan}/trangthai
        [HttpPut("{maBan}/trangthai")]
        public async Task<IActionResult> CapNhatTrangThaiBan(string maBan, [FromBody] string trangThai)
        {
            var ban = await _context.BAN.FindAsync(maBan);
            if (ban == null) return NotFound("Bàn không tồn tại");

            ban.TrangThai = trangThai;
            await _context.SaveChangesAsync();

            // Gửi realtime cho frontend
            await _hubContext.Clients.All.SendAsync("BanUpdated", new { MaBan = maBan, TrangThai = trangThai });

            return Ok(ban);
        }
    }
}
