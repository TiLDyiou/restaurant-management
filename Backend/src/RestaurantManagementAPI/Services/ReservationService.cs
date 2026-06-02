using Microsoft.EntityFrameworkCore;
using RestaurantManagementAPI.Common.Constants;
using RestaurantManagementAPI.Common.Wrappers;
using RestaurantManagementAPI.Data;
using RestaurantManagementAPI.DTOs.BanDtos;
using RestaurantManagementAPI.Interfaces;
using RestaurantManagementAPI.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            var ban = await _context.BAN.FirstOrDefaultAsync(b => b.MaBan == dto.MaBan && !b.IsDeleted);
            if (ban == null) return ServiceResult<DatBan>.Fail("Bàn không tồn tại hoặc đã bị xóa");

            // Conflict Check: Assume each reservation blocks the table for 2 hours
            var reservationStart = dto.ThoiGianDat;
            var reservationEnd = dto.ThoiGianDat.AddHours(2);

            var hasConflict = await _context.DATBAN
                .AnyAsync(r => r.MaBan == dto.MaBan 
                               && r.TrangThai != "Đã huỷ"
                               && r.ThoiGianDat < reservationEnd 
                               && r.ThoiGianDat.AddHours(2) > reservationStart);

            if (hasConflict)
            {
                return ServiceResult<DatBan>.Fail("Thời gian này đã có người đặt bàn ăn trước đó");
            }

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
                return ServiceResult<DatBan>.Fail("Lỗi hệ thống khi đặt bàn: " + ex.Message);
            }
        }

        public async Task<ServiceResult> CancelReservationAsync(string maDatBan)
        {
            var datBan = await _context.DATBAN.FindAsync(maDatBan);
            if (datBan == null)
            {
                return ServiceResult.Fail("Lịch đặt bàn không tồn tại");
            }

            if (datBan.TrangThai == "Đã huỷ")
            {
                return ServiceResult.Ok("Lịch đặt đã được hủy trước đó");
            }

            using var trans = await _context.Database.BeginTransactionAsync();
            try
            {
                datBan.TrangThai = "Đã huỷ";

                // If the table is currently Reserved, release it
                var ban = await _context.BAN.FindAsync(datBan.MaBan);
                if (ban != null && ban.TrangThai == SystemConstants.TableReserved)
                {
                    ban.TrangThai = SystemConstants.TableEmpty;
                    await _notifier.NotifyTableStatusChangedAsync(ban.MaBan, SystemConstants.TableEmpty);
                }

                await _context.SaveChangesAsync();
                await trans.CommitAsync();

                return ServiceResult.Ok("Hủy đặt bàn thành công");
            }
            catch (Exception ex)
            {
                await trans.RollbackAsync();
                return ServiceResult.Fail("Lỗi hệ thống khi hủy đặt bàn: " + ex.Message);
            }
        }

        public async Task<ServiceResult<PaginatedResult<DatBanDto>>> GetAllReservationsAsync(int pageNumber = 1, int pageSize = 10)
        {
            var query = _context.DATBAN.AsQueryable();

            var totalCount = await query.CountAsync();
            var list = await query
                .OrderByDescending(r => r.ThoiGianDat)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new DatBanDto
                {
                    MaDatBan = r.MaDatBan,
                    MaBan = r.MaBan,
                    TenKhachHang = r.TenKhachHang,
                    SoDienThoai = r.SoDienThoai,
                    ThoiGianDat = r.ThoiGianDat,
                    SoNguoi = r.SoNguoi,
                    TrangThai = r.TrangThai
                })
                .ToListAsync();

            var paginated = PaginatedResult<DatBanDto>.Create(list, totalCount, pageNumber, pageSize);
            return ServiceResult<PaginatedResult<DatBanDto>>.Ok(paginated);
        }

        private async Task<string> GenerateDatBanId()
        {
            var nextValList = await _context.Database
                .SqlQueryRaw<int>("SELECT NEXT VALUE FOR MaDatBanSequence")
                .ToListAsync();
            int nextVal = nextValList.FirstOrDefault();
            if (nextVal == 0) nextVal = 1;
            return $"DB{nextVal:D5}";
        }
    }
}