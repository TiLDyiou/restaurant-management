using Microsoft.EntityFrameworkCore;
using RestaurantManagementAPI.Common.Constants;
using RestaurantManagementAPI.Common.Wrappers;
using RestaurantManagementAPI.Data;
using RestaurantManagementAPI.DTOs;
using RestaurantManagementAPI.Interfaces;
using RestaurantManagementAPI.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RestaurantManagementAPI.Services
{
    public class TableService : ITableService
    {
        private readonly QLNHDbContext _context;
        private readonly IRealtimeNotifier _notifier;

        public TableService(QLNHDbContext context, IRealtimeNotifier notifier)
        {
            _context = context;
            _notifier = notifier;
        }

        public async Task<ServiceResult<List<Ban>>> GetAllBanAsync()
        {
            var list = await _context.BAN
                .Where(b => !b.IsDeleted)
                .ToListAsync();
            return ServiceResult<List<Ban>>.Ok(list);
        }

        public async Task<ServiceResult<Ban>> GetBanByIdAsync(string maBan)
        {
            var ban = await _context.BAN.FirstOrDefaultAsync(b => b.MaBan == maBan && !b.IsDeleted);
            if (ban == null)
                return ServiceResult<Ban>.Fail("Bàn không tồn tại hoặc đã bị xóa");
            return ServiceResult<Ban>.Ok(ban);
        }

        public async Task<ServiceResult<Ban>> UpdateStatusAsync(string maBan, string trangThai, string? maNV = null)
        {
            var ban = await _context.BAN.FirstOrDefaultAsync(b => b.MaBan == maBan && !b.IsDeleted);
            if (ban == null) 
                return ServiceResult<Ban>.Fail("Bàn không tồn tại hoặc đã bị xóa");

            // Validate state machine transitions
            if (ban.TrangThai == trangThai)
            {
                return ServiceResult<Ban>.Ok(ban, "Trạng thái không đổi");
            }

            string? oldStatus = ban.TrangThai;
            ban.TrangThai = trangThai;
            await _context.SaveChangesAsync();

            await RecordHistoryAsync(maBan, oldStatus, trangThai, maNV);
            await _notifier.NotifyTableStatusChangedAsync(maBan, trangThai);

            return ServiceResult<Ban>.Ok(ban, "Cập nhật thành công");
        }

        public async Task<ServiceResult<Ban>> CreateBanAsync(CreateBanDto dto, string? maNV = null)
        {
            var existing = await _context.BAN.FindAsync(dto.MaBan);
            if (existing != null)
            {
                if (existing.IsDeleted)
                {
                    // Restore soft-deleted table
                    existing.IsDeleted = false;
                    existing.TenBan = dto.TenBan;
                    existing.SucChua = dto.SucChua;
                    existing.KhuVuc = dto.KhuVuc;
                    existing.TrangThai = SystemConstants.TableEmpty;
                    await _context.SaveChangesAsync();

                    await RecordHistoryAsync(existing.MaBan, null, SystemConstants.TableEmpty, maNV);
                    await _notifier.NotifyTableStatusChangedAsync(existing.MaBan, SystemConstants.TableEmpty);

                    return ServiceResult<Ban>.Ok(existing, "Khôi phục và cập nhật bàn thành công");
                }
                return ServiceResult<Ban>.Fail("Mã bàn đã tồn tại");
            }

            var ban = new Ban
            {
                MaBan = dto.MaBan,
                TenBan = dto.TenBan,
                SucChua = dto.SucChua,
                KhuVuc = dto.KhuVuc,
                TrangThai = SystemConstants.TableEmpty,
                IsDeleted = false
            };

            _context.BAN.Add(ban);
            await _context.SaveChangesAsync();

            await RecordHistoryAsync(ban.MaBan, null, SystemConstants.TableEmpty, maNV);
            await _notifier.NotifyTableStatusChangedAsync(ban.MaBan, SystemConstants.TableEmpty);

            return ServiceResult<Ban>.Ok(ban, "Tạo bàn thành công");
        }

        public async Task<ServiceResult<Ban>> UpdateBanAsync(string maBan, UpdateBanDto dto, string? maNV = null)
        {
            var ban = await _context.BAN.FirstOrDefaultAsync(b => b.MaBan == maBan && !b.IsDeleted);
            if (ban == null)
                return ServiceResult<Ban>.Fail("Bàn không tồn tại");

            string? oldStatus = ban.TrangThai;
            ban.TenBan = dto.TenBan;
            ban.SucChua = dto.SucChua;
            ban.KhuVuc = dto.KhuVuc;

            if (!string.IsNullOrEmpty(dto.TrangThai) && ban.TrangThai != dto.TrangThai)
            {
                ban.TrangThai = dto.TrangThai;
            }

            await _context.SaveChangesAsync();

            if (oldStatus != ban.TrangThai)
            {
                await RecordHistoryAsync(maBan, oldStatus, ban.TrangThai ?? SystemConstants.TableEmpty, maNV);
                await _notifier.NotifyTableStatusChangedAsync(maBan, ban.TrangThai ?? SystemConstants.TableEmpty);
            }

            return ServiceResult<Ban>.Ok(ban, "Cập nhật thông tin bàn thành công");
        }

        public async Task<ServiceResult<Ban>> DeleteBanAsync(string maBan, string? maNV = null)
        {
            var ban = await _context.BAN.FirstOrDefaultAsync(b => b.MaBan == maBan && !b.IsDeleted);
            if (ban == null)
                return ServiceResult<Ban>.Fail("Bàn không tồn tại");

            // Reject if table has unpaid orders
            var hasUnpaidOrder = await _context.HOADON.AnyAsync(h => h.MaBan == maBan && h.TrangThai == SystemConstants.OrderUnpaid);
            if (hasUnpaidOrder)
            {
                return ServiceResult<Ban>.Fail("Không thể xóa bàn đang có hóa đơn chưa thanh toán");
            }

            string? oldStatus = ban.TrangThai;
            ban.IsDeleted = true;
            await _context.SaveChangesAsync();

            await RecordHistoryAsync(maBan, oldStatus, "Đã xóa (Soft Delete)", maNV);
            await _notifier.NotifyTableStatusChangedAsync(maBan, "Deleted");

            return ServiceResult<Ban>.Ok(ban, "Xóa bàn thành công");
        }

        public async Task<ServiceResult<Ban>> MergeTablesAsync(MergeTablesDto dto, string? maNV = null)
        {
            var banChinh = await _context.BAN.FirstOrDefaultAsync(b => b.MaBan == dto.MaBanChinh && !b.IsDeleted);
            var banPhu = await _context.BAN.FirstOrDefaultAsync(b => b.MaBan == dto.MaBanPhu && !b.IsDeleted);

            if (banChinh == null || banPhu == null)
            {
                return ServiceResult<Ban>.Fail("Một hoặc cả hai bàn không tồn tại");
            }

            // Both tables must be Empty
            if (banChinh.TrangThai != SystemConstants.TableEmpty || banPhu.TrangThai != SystemConstants.TableEmpty)
            {
                return ServiceResult<Ban>.Fail("Tất cả các bàn gộp phải ở trạng thái Trống");
            }

            using var trans = await _context.Database.BeginTransactionAsync();
            try
            {
                string? oldChinh = banChinh.TrangThai;
                string? oldPhu = banPhu.TrangThai;

                // Merge operation: set MaBanGop of banPhu to banChinh
                banPhu.MaBanGop = banChinh.MaBan;
                
                // Set status of both tables to Occupied as they are now merged and ready for service
                banChinh.TrangThai = SystemConstants.TableOccupied;
                banPhu.TrangThai = SystemConstants.TableOccupied;

                await _context.SaveChangesAsync();
                await trans.CommitAsync();

                await RecordHistoryAsync(banChinh.MaBan, oldChinh, SystemConstants.TableOccupied, maNV);
                await RecordHistoryAsync(banPhu.MaBan, oldPhu, $"{SystemConstants.TableOccupied} (Gộp vào {banChinh.MaBan})", maNV);

                await _notifier.NotifyTableStatusChangedAsync(banChinh.MaBan, SystemConstants.TableOccupied);
                await _notifier.NotifyTableStatusChangedAsync(banPhu.MaBan, SystemConstants.TableOccupied);

                return ServiceResult<Ban>.Ok(banChinh, $"Gộp bàn {banPhu.MaBan} vào bàn {banChinh.MaBan} thành công");
            }
            catch (Exception ex)
            {
                await trans.RollbackAsync();
                return ServiceResult<Ban>.Fail("Lỗi hệ thống khi gộp bàn: " + ex.Message);
            }
        }

        public async Task<ServiceResult<Ban>> SplitTablesAsync(string maBanChinh, string? maNV = null)
        {
            var banChinh = await _context.BAN.FirstOrDefaultAsync(b => b.MaBan == maBanChinh && !b.IsDeleted);
            if (banChinh == null)
            {
                return ServiceResult<Ban>.Fail("Bàn chính không tồn tại");
            }

            var mergedTables = await _context.BAN
                .Where(b => b.MaBanGop == maBanChinh && !b.IsDeleted)
                .ToListAsync();

            if (!mergedTables.Any())
            {
                return ServiceResult<Ban>.Fail("Bàn này không có bàn nào đang gộp kèm");
            }

            using var trans = await _context.Database.BeginTransactionAsync();
            try
            {
                string? oldChinh = banChinh.TrangThai;
                banChinh.TrangThai = SystemConstants.TableEmpty;
                await RecordHistoryAsync(banChinh.MaBan, oldChinh, SystemConstants.TableEmpty, maNV);
                await _notifier.NotifyTableStatusChangedAsync(banChinh.MaBan, SystemConstants.TableEmpty);

                foreach (var banPhu in mergedTables)
                {
                    string? oldPhu = banPhu.TrangThai;
                    banPhu.MaBanGop = null;
                    banPhu.TrangThai = SystemConstants.TableEmpty;

                    await RecordHistoryAsync(banPhu.MaBan, oldPhu, SystemConstants.TableEmpty, maNV);
                    await _notifier.NotifyTableStatusChangedAsync(banPhu.MaBan, SystemConstants.TableEmpty);
                }

                await _context.SaveChangesAsync();
                await trans.CommitAsync();

                return ServiceResult<Ban>.Ok(banChinh, "Tách các bàn thành công");
            }
            catch (Exception ex)
            {
                await trans.RollbackAsync();
                return ServiceResult<Ban>.Fail("Lỗi hệ thống khi tách bàn: " + ex.Message);
            }
        }

        public async Task<ServiceResult<Ban>> TransferOrderAsync(TransferOrderDto dto, string? maNV = null)
        {
            var banNguon = await _context.BAN.FirstOrDefaultAsync(b => b.MaBan == dto.MaBanNguon && !b.IsDeleted);
            var banDich = await _context.BAN.FirstOrDefaultAsync(b => b.MaBan == dto.MaBanDich && !b.IsDeleted);

            if (banNguon == null || banDich == null)
            {
                return ServiceResult<Ban>.Fail("Bàn nguồn hoặc bàn đích không tồn tại");
            }

            // Target table must be Empty
            if (banDich.TrangThai != SystemConstants.TableEmpty)
            {
                return ServiceResult<Ban>.Fail("Bàn đích phải đang ở trạng thái Trống");
            }

            // Find active unpaid order on source table
            var order = await _context.HOADON
                .FirstOrDefaultAsync(h => h.MaBan == dto.MaBanNguon && h.TrangThai == SystemConstants.OrderUnpaid);

            if (order == null)
            {
                return ServiceResult<Ban>.Fail("Bàn nguồn không có hóa đơn chưa thanh toán");
            }

            using var trans = await _context.Database.BeginTransactionAsync();
            try
            {
                string? oldNguon = banNguon.TrangThai;
                string? oldDich = banDich.TrangThai;

                // Transfer order to target table
                order.MaBan = dto.MaBanDich;

                // Update table statuses
                banNguon.TrangThai = SystemConstants.TableEmpty;
                banDich.TrangThai = SystemConstants.TableOccupied;

                await _context.SaveChangesAsync();
                await trans.CommitAsync();

                // History logs
                await RecordHistoryAsync(banNguon.MaBan, oldNguon, SystemConstants.TableEmpty, maNV);
                await RecordHistoryAsync(banDich.MaBan, oldDich, SystemConstants.TableOccupied, maNV);

                // Notifications
                await _notifier.NotifyTableStatusChangedAsync(banNguon.MaBan, SystemConstants.TableEmpty);
                await _notifier.NotifyTableStatusChangedAsync(banDich.MaBan, SystemConstants.TableOccupied);
                await _notifier.NotifyOrderCreatedAsync(order.MaHD); // Notify that order table changed

                return ServiceResult<Ban>.Ok(banDich, $"Chuyển hóa đơn thành công từ bàn {dto.MaBanNguon} sang bàn {dto.MaBanDich}");
            }
            catch (Exception ex)
            {
                await trans.RollbackAsync();
                return ServiceResult<Ban>.Fail("Lỗi hệ thống khi chuyển bàn: " + ex.Message);
            }
        }

        public async Task<ServiceResult<List<LichSuBanDto>>> GetTableHistoryAsync(string maBan)
        {
            var list = await _context.LICHSUBAN
                .Include(x => x.NhanVien)
                .Where(x => x.MaBan == maBan)
                .OrderByDescending(x => x.ThoiGian)
                .Select(x => new LichSuBanDto
                {
                    Id = x.Id,
                    MaBan = x.MaBan,
                    TrangThaiCu = x.TrangThaiCu,
                    TrangThaiMoi = x.TrangThaiMoi,
                    ThoiGian = x.ThoiGian,
                    MaNV = x.MaNV,
                    TenNV = x.NhanVien != null ? x.NhanVien.HoTen : "Hệ thống"
                })
                .ToListAsync();

            return ServiceResult<List<LichSuBanDto>>.Ok(list);
        }

        private async Task RecordHistoryAsync(string maBan, string? trangThaiCu, string trangThaiMoi, string? maNV)
        {
            var history = new LichSuBan
            {
                MaBan = maBan,
                TrangThaiCu = trangThaiCu,
                TrangThaiMoi = trangThaiMoi,
                ThoiGian = DateTime.Now,
                MaNV = maNV
            };
            _context.LICHSUBAN.Add(history);
            await _context.SaveChangesAsync();
        }
    }
}