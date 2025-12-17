using Microsoft.EntityFrameworkCore;
using RestaurantManagementAPI.Common.Constants;
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

        public OrderService(QLNHDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceResult<List<HoaDonDto>>> GetOrdersAsync()
        {
            var list = await _context.HOADON
                .Include(hd => hd.ChiTietHoaDons)!.ThenInclude(ct => ct.MonAn)
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
                .ToListAsync();
            return ServiceResult<List<HoaDonDto>>.Ok(list);
        }

        public async Task<ServiceResult<HoaDonDto>> GetOrderByIdAsync(string id)
        {
            var hd = await _context.HOADON
                .Include(h => h.ChiTietHoaDons)!.ThenInclude(ct => ct.MonAn)
                .FirstOrDefaultAsync(h => h.MaHD == id);

            if (hd == null) return ServiceResult<HoaDonDto>.Fail("Không tìm thấy đơn");

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
                if (ban == null) return ServiceResult<HoaDonDto>.Fail("Bàn không tồn tại");

                var nv = await _context.NHANVIEN.FindAsync(dto.MaNV);
                if (nv == null) return ServiceResult<HoaDonDto>.Fail($"Nhân viên {dto.MaNV} không tồn tại.");

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
                    if (monAn == null) throw new Exception($"Món ăn {item.MaMA} không tồn tại");

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
                    if (TcpSocketServer.Instance != null)
                    {
                        var tablePayload = JsonSerializer.Serialize(new { MaBan = dto.MaBan, TrangThai = SystemConstants.TableOccupied });
                        await TcpSocketServer.Instance.BroadcastAsync($"TABLE|{tablePayload}");
                        await TcpSocketServer.Instance.BroadcastAsync($"ORDER|{maHD}");
                    }
                }
                catch (Exception ex) { Console.WriteLine($"Lỗi Socket: {ex.Message}"); }

                var resultDto = (await GetOrderByIdAsync(maHD)).Data;
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
                .Include(x => x.MonAn).Include(x => x.HoaDon)
                .FirstOrDefaultAsync(x => x.MaHD == maHD && x.MaMA == maMA);

            if (item == null) return ServiceResult.Fail("Không tìm thấy món");

            item.TrangThai = newStatus;

            // --- LOGIC FIX LỖI NHẬN THÔNG BÁO ---
            string statusNorm = newStatus?.ToLower().Trim() ?? "";

            // So sánh với "đã xong" (viết thường)
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

                if (TcpSocketServer.Instance != null)
                {
                    await TcpSocketServer.Instance.BroadcastAsync($"KITCHEN_DONE|{msg}");
                }
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
            if (hd == null) return ServiceResult.Fail("Hóa đơn không tồn tại");

            hd.TrangThai = newStatus;
            await _context.SaveChangesAsync();
            return ServiceResult.Ok("Cập nhật thành công");
        }

        public async Task<ServiceResult<HoaDonDto>> CheckoutAsync(string maHD, CheckoutRequestDto dto)
        {
            var hd = await _context.HOADON.FindAsync(maHD);
            if (hd == null) return ServiceResult<HoaDonDto>.Fail("Hóa đơn không tồn tại");

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

            if (TcpSocketServer.Instance != null && ban != null)
            {
                var payload = JsonSerializer.Serialize(new { MaBan = ban.MaBan, TrangThai = SystemConstants.TableEmpty });
                await TcpSocketServer.Instance.BroadcastAsync($"TABLE|{payload}");
            }

            var resultDto = (await GetOrderByIdAsync(maHD)).Data;
            return ServiceResult<HoaDonDto>.Ok(resultDto!, "Thanh toán thành công");
        }

        private async Task<string> GenerateMaHD()
        {
            var lastHD = await _context.HOADON.OrderByDescending(h => h.MaHD).Select(h => h.MaHD).FirstOrDefaultAsync();
            if (string.IsNullOrEmpty(lastHD)) return "HD00001";
            if (lastHD.Length > 2 && int.TryParse(lastHD.Substring(2), out int num)) return $"HD{(num + 1):D5}";
            return $"HD{DateTime.Now.Ticks}";
        }
    }
}