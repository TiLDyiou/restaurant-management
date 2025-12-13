using Microsoft.EntityFrameworkCore;
using RestaurantManagementAPI.Data;
using RestaurantManagementAPI.DTOs.BanDtos;
using RestaurantManagementAPI.Models.Entities;
using RestaurantManagementAPI.Services.Interfaces;
using System.Text.Json;

namespace RestaurantManagementAPI.Services.Implements
{
    public class ReservationService : IReservationService
    {
        private readonly QLNHDbContext _context;

        public ReservationService(QLNHDbContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string Message, DatBan? Data)> CreateReservationAsync(CreateDatBanDto dto)
        {
            var ban = await _context.BAN.FindAsync(dto.MaBan);
            if (ban == null) return (false, "Bàn không tồn tại", null);

            using var trans = await _context.Database.BeginTransactionAsync();
            try
            {
                var datBan = new DatBan
                {
                    MaDatBan = await GenerateDatBanId(),
                    MaBan = dto.MaBan,
                    TenKhachHang = dto.TenKhachHang,
                    SoDienThoai = dto.SoDienThoai,
                    ThoiGianDat = dto.ThoiGianDat,
                    SoNguoi = dto.SoNguoi,
                    TrangThai = "Đã xác nhận"
                };

                bool isUpdated = false;
                if (datBan.ThoiGianDat > DateTime.UtcNow && datBan.ThoiGianDat < DateTime.UtcNow.AddHours(3))
                {
                    ban.TrangThai = "Bàn đã đặt";
                    isUpdated = true;
                }

                _context.DATBAN.Add(datBan);
                await _context.SaveChangesAsync();
                await trans.CommitAsync();

                // Gửi Socket
                if (isUpdated && TcpSocketServer.Instance != null)
                {
                    var payload = new { MaBan = dto.MaBan, TrangThai = "Bàn đã đặt" };
                    string jsonTable = JsonSerializer.Serialize(payload);
                    await TcpSocketServer.Instance.BroadcastAsync($"TABLE|{jsonTable}");
                }

                return (true, "Đặt bàn thành công", datBan);
            }
            catch (Exception ex)
            {
                await trans.RollbackAsync();
                return (false, "Lỗi hệ thống: " + ex.Message, null);
            }
        }

        private async Task<string> GenerateDatBanId()
        {
            var last = await _context.DATBAN.OrderByDescending(db => db.MaDatBan).FirstOrDefaultAsync();
            if (last == null) return "DB00001";
            string num = last.MaDatBan.Substring(2);
            if (int.TryParse(num, out int n)) return $"DB{n + 1:D5}";
            return "DB00001";
        }
    }
}