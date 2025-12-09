using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurentManagementAPI.Data;
using RestaurentManagementAPI.DTOs;
using RestaurentManagementAPI.DTOs.BanDtos;
using RestaurentManagementAPI.Models.Entities;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using RestaurentManagementAPI.Hubs;

namespace RestaurentManagementAPI.Controllers
{
    [Route("api/reservations")]
    [ApiController]
    public class ReservationsController : ControllerBase
    {
        private readonly QLNHDbContext _context;
        private readonly IHubContext<BanHub> _banHubContext;
        public ReservationsController(QLNHDbContext context, IHubContext<BanHub> banHubContext)
        {
            _context = context;
            _banHubContext = banHubContext;
        }

        // HÀM TẠO MÃ ĐẶT BÀN TỰ ĐỘNG
        private async Task<string> GenerateNewDatBanIdAsync()
        {
            var lastDatBan = await _context.DATBAN
                                .OrderByDescending(db => db.MaDatBan)
                                .FirstOrDefaultAsync();

            if (lastDatBan == null)
            {
                return "DB00001"; // Bắt đầu
            }

            string numberPart = lastDatBan.MaDatBan.Substring(2); // Bỏ "DB"
            if (int.TryParse(numberPart, out int lastNumber))
            {
                int newNumber = lastNumber + 1;
                return $"DB{newNumber:D5}"; // Format thành 5 chữ số
            }

            throw new Exception("Không thể tạo mã đặt bàn mới.");
        }

        // API: POST /api/reservations 
        // Ghi nhận thông tin đặt bàn mới
        [HttpPost]
        public async Task<ActionResult<DatBanDto>> CreateReservation([FromBody] CreateDatBanDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Kiểm tra xem bàn có tồn tại không
            var ban = await _context.BAN.FindAsync(createDto.MaBan);
            if (ban == null)
            {
                return BadRequest($"Bàn với mã {createDto.MaBan} không tồn tại.");
            }

           

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var newMaDatBan = await GenerateNewDatBanIdAsync();

                var datBan = new DatBan
                {
                    MaDatBan = newMaDatBan,
                    MaBan = createDto.MaBan,
                    TenKhachHang = createDto.TenKhachHang,
                    SoDienThoai = createDto.SoDienThoai,
                    ThoiGianDat = createDto.ThoiGianDat,
                    SoNguoi = createDto.SoNguoi,
                    TrangThai = "Đã xác nhận" // Trạng thái mặc định
                };

                // Cập nhật trạng thái Bàn
                // Nếu đặt bàn cho tương lai gần, có thể set là "Bàn đã đặt"
                if (datBan.ThoiGianDat > DateTime.UtcNow && datBan.ThoiGianDat < DateTime.UtcNow.AddHours(3))
                {
                    ban.TrangThai = "Bàn đã đặt";
                }

                await _context.DATBAN.AddAsync(datBan);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                if (ban.TrangThai == "Bàn đã đặt")
                {
                    await _banHubContext.Clients.All.SendAsync("BanUpdated", new
                    {
                        MaBan = createDto.MaBan,
                        TrangThai = "Bàn đã đặt"
                    });
                }
                var datBanDto = new DatBanDto
                {
                    MaDatBan = datBan.MaDatBan,
                    MaBan = datBan.MaBan,
                    TenKhachHang = datBan.TenKhachHang,
                    SoDienThoai = datBan.SoDienThoai,
                    ThoiGianDat = datBan.ThoiGianDat,
                    SoNguoi = datBan.SoNguoi,
                    TrangThai = datBan.TrangThai
                };
                return Ok(datBanDto); // Trả về đối tượng vừa tạo
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Lỗi nội bộ server: {ex.Message}");
            }
        }
    }
}
