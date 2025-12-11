using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementAPI.Data;
using RestaurantManagementAPI.DTOs.BanDtos;
using RestaurantManagementAPI.Models.Entities;
using RestaurantManagementAPI.Services;
using System.Text.Json;

namespace RestaurantManagementAPI.Controllers
{
    [Route("api/reservations")]
    [ApiController]
    public class ReservationsController : ControllerBase
    {
        private readonly QLNHDbContext _context;

        public ReservationsController(QLNHDbContext context)
        {
            _context = context;
        }

        private async Task<string> GenerateId()
        {
            var last = await _context.DATBAN.OrderByDescending(db => db.MaDatBan).FirstOrDefaultAsync();
            if (last == null) return "DB00001";
            string num = last.MaDatBan.Substring(2);
            if (int.TryParse(num, out int n)) return $"DB{n + 1:D5}";
            return "DB00001";
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateDatBanDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var ban = await _context.BAN.FindAsync(dto.MaBan);
            if (ban == null) return BadRequest("Bàn không tồn tại");

            using var trans = await _context.Database.BeginTransactionAsync();
            try
            {
                var datBan = new DatBan
                {
                    MaDatBan = await GenerateId(),
                    MaBan = dto.MaBan,
                    TenKhachHang = dto.TenKhachHang,
                    SoDienThoai = dto.SoDienThoai,
                    ThoiGianDat = dto.ThoiGianDat,
                    SoNguoi = dto.SoNguoi,
                    TrangThai = "Đã xác nhận"
                };

                bool isUpdated = false;
                // Nếu đặt bàn trong vòng 3 tiếng tới thì đổi màu bàn luôn
                if (datBan.ThoiGianDat > DateTime.UtcNow && datBan.ThoiGianDat < DateTime.UtcNow.AddHours(3))
                {
                    ban.TrangThai = "Bàn đã đặt";
                    isUpdated = true;
                }

                _context.DATBAN.Add(datBan);
                await _context.SaveChangesAsync();
                await trans.CommitAsync();

                // GỬI SOCKET
                if (isUpdated && TcpSocketServer.Instance != null)
                {
                    var payload = new { MaBan = dto.MaBan, TrangThai = "Bàn đã đặt" };
                    string jsonTable = JsonSerializer.Serialize(payload);
                    await TcpSocketServer.Instance.BroadcastAsync($"TABLE|{jsonTable}");
                }

                return Ok(datBan);
            }
            catch (Exception ex)
            {
                await trans.RollbackAsync();
                return StatusCode(500, ex.Message);
            }
        }
    }
}