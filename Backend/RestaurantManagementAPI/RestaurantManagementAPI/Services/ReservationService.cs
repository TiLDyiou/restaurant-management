using Microsoft.EntityFrameworkCore;
using RestaurantManagementAPI.Common.Constants;
using RestaurantManagementAPI.Common.Wrappers;
using RestaurantManagementAPI.Data;
using RestaurantManagementAPI.DTOs.BanDtos;
using RestaurantManagementAPI.Interfaces;
using RestaurantManagementAPI.Models.Entities;

namespace RestaurantManagementAPI.Services
{
    public class ReservationService : IReservationService
    {
        private readonly QLNHDbContext _context;
        private readonly IRealtimeNotifier _notifier;

        public ReservationService(QLNHDbContext context, IRealtimeNotifier notifier)
        {
            _context = context;
            _notifier = notifier;
        }

        public async Task<ServiceResult<DatBan>> CreateReservationAsync(CreateDatBanDto dto)
        {
            var ban = await _context.BAN.FindAsync(dto.MaBan);
            if (ban == null) return ServiceResult<DatBan>.Fail("Bàn không tồn tại");

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
                    ban.TrangThai = SystemConstants.TableReserved;
                    isUpdated = true;
                }

                _context.DATBAN.Add(datBan);
                await _context.SaveChangesAsync();
                await trans.CommitAsync();

                if (isUpdated)
                {
                    await _notifier.NotifyTableStatusChangedAsync(dto.MaBan, SystemConstants.TableReserved);
                }

                return ServiceResult<DatBan>.Ok(datBan, "Đặt bàn thành công");
            }
            catch (Exception ex)
            {
                await trans.RollbackAsync();
                return ServiceResult<DatBan>.Fail("Lỗi: " + ex.Message);
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