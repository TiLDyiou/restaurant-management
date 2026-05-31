using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantManagementAPI.Common.Constants;
using RestaurantManagementAPI.Common.StateMachines;
using RestaurantManagementAPI.Common.Wrappers;
using RestaurantManagementAPI.Data;
using RestaurantManagementAPI.DTOs;
using RestaurantManagementAPI.DTOs.MonAnDtos;
using RestaurantManagementAPI.Infrastructure.Sockets;
using RestaurantManagementAPI.Interfaces;
using RestaurantManagementAPI.Models.Entities;
using System.Text.Json;

namespace RestaurantManagementAPI.Services
{
    public class OrderService : IOrderService
    {
        private readonly QLNHDbContext _context;
        private readonly ILogger<OrderService> _logger;
        private readonly IRealtimeNotifier _notifier;

        public OrderService(QLNHDbContext context, ILogger<OrderService> logger, IRealtimeNotifier notifier)
        {
            _context = context;
            _logger = logger;
            _notifier = notifier;
        }

        public async Task<ServiceResult<PaginatedResult<HoaDonDto>>> GetOrdersAsync(int pageNumber = 1, int pageSize = 10)
        {
            var query = _context.HOADON
                .Include(hd => hd.ChiTietHoaDons)!
                .ThenInclude(ct => ct.MonAn);

            var totalCount = await query.CountAsync();

            var list = await query
                .Select(hd => new HoaDonDto
                {
                    MaHD = hd.MaHD,
                    MaBan = hd.MaBan,
                    MaNV = hd.MaNV,
                    TongTien = hd.TongTien,
                    TrangThai = hd.TrangThai,
                    NgayLap = hd.NgayLap,
                    ChiTietHoaDons = hd.ChiTietHoaDons.Select(ct => new ChiTietHoaDonViewDto
                    {
                        MaMA = ct.MaMA,
                        TenMA = ct.MonAn != null ? ct.MonAn.TenMA : "Unknown",
                        SoLuong = ct.SoLuong,
                        DonGia = ct.DonGia,
                        ThanhTien = ct.ThanhTien,
                        TrangThai = ct.TrangThai
                    }).ToList()
                })
                .OrderByDescending(h => h.NgayLap)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginated = PaginatedResult<HoaDonDto>.Create(list, totalCount, pageNumber, pageSize);
            return ServiceResult<PaginatedResult<HoaDonDto>>.Ok(paginated);
        }

        public async Task<ServiceResult<HoaDonDto>> GetOrderByIdAsync(string id)
        {
            var hd = await _context.HOADON
                .Include(h => h.ChiTietHoaDons)!
                .ThenInclude(ct => ct.MonAn)
                .FirstOrDefaultAsync(h => h.MaHD == id);

            if (hd == null) 
                return ServiceResult<HoaDonDto>.Fail("Không tìm thấy đơn");

            var dto = new HoaDonDto
            {
                MaHD = hd.MaHD,
                MaBan = hd.MaBan,
                MaNV = hd.MaNV,
                TongTien = hd.TongTien,
                TrangThai = hd.TrangThai,
                NgayLap = hd.NgayLap,
                ChiTietHoaDons = hd.ChiTietHoaDons.Select(ct => new ChiTietHoaDonViewDto
                {
                    MaMA = ct.MaMA,
                    TenMA = ct.MonAn != null ? ct.MonAn.TenMA : "Unknown",
                    SoLuong = ct.SoLuong,
                    DonGia = ct.DonGia,
                    ThanhTien = ct.ThanhTien,
                    TrangThai = ct.TrangThai
                }).ToList()
            };
            return ServiceResult<HoaDonDto>.Ok(dto);
        }

        public async Task<ServiceResult<HoaDonDto>> CreateOrderAsync(CreateHoaDonDto dto)
        {
            using var trans = await _context.Database.BeginTransactionAsync();
            try
            {
                var ban = await _context.BAN.FindAsync(dto.MaBan);
                if (ban == null) 
                    return ServiceResult<HoaDonDto>.Fail("Bàn không tồn tại");

                var nv = await _context.NHANVIEN.FindAsync(dto.MaNV);
                if (nv == null) 
                    return ServiceResult<HoaDonDto>.Fail($"Nhân viên {dto.MaNV} không tồn tại.");

                var maHD = await GenerateMaHD();
                var hoaDon = new HoaDon
                {
                    MaHD = maHD,
                    MaBan = dto.MaBan,
                    MaNV = dto.MaNV,
                    NgayLap = DateTime.Now,
                    TrangThai = SystemConstants.OrderUnpaid,
                    TongTien = 0
                };

                decimal tongTienCalc = 0;
                var listChiTiet = new List<ChiTietHoaDon>();

                foreach (var item in dto.ChiTietHoaDons)
                {
                    var monAn = await _context.MONAN.FindAsync(item.MaMA);
                    if (monAn == null) 
                        throw new Exception($"Món ăn {item.MaMA} không tồn tại"); // Lỗi ngoại lệ để rollback

                    var chiTiet = new ChiTietHoaDon
                    {
                        MaHD = maHD,
                        MaMA = item.MaMA,
                        SoLuong = item.SoLuong,
                        DonGia = monAn.DonGia,
                        TrangThai = SystemConstants.ItemWaiting
                    };
                    listChiTiet.Add(chiTiet);
                    tongTienCalc += (item.SoLuong * monAn.DonGia);
                }

                hoaDon.TongTien = tongTienCalc;
                _context.HOADON.Add(hoaDon);
                _context.CHITIETHOADON.AddRange(listChiTiet);

                ban.TrangThai = SystemConstants.TableOccupied;
                _context.BAN.Update(ban);

                var thongBao = new ThongBao
                {
                    NoiDung = $"Bàn {dto.MaBan} vừa lên đơn mới",
                    ThoiGian = DateTime.Now,
                    IsRead = false,
                    Loai = SystemConstants.NotiKitchen
                };
                _context.THONGBAO.Add(thongBao);

                await _context.SaveChangesAsync();
                await trans.CommitAsync();

                try
                {
                    await _notifier.NotifyTableStatusChangedAsync(dto.MaBan, SystemConstants.TableOccupied);
                    await _notifier.NotifyOrderCreatedAsync(maHD);
                }
                catch (Exception ex) 
                { 
                    _logger.LogError(ex, "Lỗi khi phát thông tin bàn/hóa đơn qua notifier"); 
                }

                var resultDto = (await GetOrderByIdAsync(maHD)).Data; // dùng query lại để lấy đầy đủ thông tin vì Entity HoaDon mới chỉ có dữ liệu cơ bản
                return ServiceResult<HoaDonDto>.Ok(resultDto!, "Tạo đơn thành công");
            }
            catch (Exception ex)
            {
                await trans.RollbackAsync();
                return ServiceResult<HoaDonDto>.Fail("Lỗi hệ thống: " + ex.Message);
            }
        }

        public async Task<ServiceResult> UpdateOrderItemStatusAsync(string maHD, string maMA, string newStatus)
        {
            var item = await _context.CHITIETHOADON
                .Include(x => x.MonAn)
                .Include(x => x.HoaDon)
                .FirstOrDefaultAsync(x => x.MaHD == maHD && x.MaMA == maMA);

            if (item == null) 
                return ServiceResult.Fail("Không tìm thấy món");

            // State machine validation
            if (!OrderStateMachine.IsItemTransitionAllowed(item.TrangThai ?? OrderStateMachine.ItemWaiting, newStatus))
            {
                return ServiceResult.Fail($"Chuyển trạng thái món từ '{item.TrangThai}' sang '{newStatus}' không hợp lệ");
            }

            item.TrangThai = newStatus;
            string statusNorm = newStatus?.ToLower().Trim() ?? "";
            if (statusNorm == SystemConstants.ItemReady.ToLower() || statusNorm == "done")
            {
                string msg = $"Bàn {item.HoaDon?.MaBan}: {item.MonAn?.TenMA} đã xong";

                var thongBao = new ThongBao
                {
                    NoiDung = msg,
                    ThoiGian = DateTime.Now,
                    Loai = SystemConstants.NotiService
                };
                _context.THONGBAO.Add(thongBao);
                await _context.SaveChangesAsync();

                await _notifier.NotifyKitchenItemReadyAsync(msg);
            }
            else
            {
                await _context.SaveChangesAsync();
            }

            return ServiceResult.Ok("Cập nhật thành công");
        }

        public async Task<ServiceResult> UpdateOrderStatusAsync(string id, string newStatus)
        {
            var hd = await _context.HOADON.FindAsync(id);
            if (hd == null) 
                return ServiceResult.Fail("Hóa đơn không tồn tại");

            // State machine validation
            if (!OrderStateMachine.IsOrderTransitionAllowed(hd.TrangThai ?? OrderStateMachine.OrderUnpaid, newStatus))
            {
                return ServiceResult.Fail($"Chuyển trạng thái hóa đơn từ '{hd.TrangThai}' sang '{newStatus}' không hợp lệ");
            }

            hd.TrangThai = newStatus;
            await _context.SaveChangesAsync();
            return ServiceResult.Ok("Cập nhật thành công");
        }

        public async Task<ServiceResult<HoaDonDto>> CheckoutAsync(string maHD, CheckoutRequestDto dto)
        {
            var hd = await _context.HOADON.FindAsync(maHD);
            if (hd == null) 
                return ServiceResult<HoaDonDto>.Fail("Hóa đơn không tồn tại");

            if (hd.TrangThai == SystemConstants.OrderPaid)
                return ServiceResult<HoaDonDto>.Fail("Hóa đơn này đã thanh toán rồi");

            hd.TrangThai = SystemConstants.OrderPaid;
            hd.PaymentMethod = dto.PaymentMethod;

            var ban = await _context.BAN.FindAsync(hd.MaBan);
            if (ban != null)
            {
                ban.TrangThai = SystemConstants.TableEmpty;
                _context.BAN.Update(ban);
            }

            await _context.SaveChangesAsync();

            if (ban != null)
            {
                await _notifier.NotifyTableStatusChangedAsync(ban.MaBan, SystemConstants.TableEmpty);
            }

            var resultDto = (await GetOrderByIdAsync(maHD)).Data;
            return ServiceResult<HoaDonDto>.Ok(resultDto!, "Thanh toán thành công");
        }

        private async Task<string> GenerateMaHD()
        {
            var nextValList = await _context.Database
                .SqlQueryRaw<int>("SELECT NEXT VALUE FOR MaHDSequence")
                .ToListAsync();
            int nextVal = nextValList.FirstOrDefault();
            if (nextVal == 0) nextVal = 1;
            return $"HD{nextVal:D5}";
        }
    }
}