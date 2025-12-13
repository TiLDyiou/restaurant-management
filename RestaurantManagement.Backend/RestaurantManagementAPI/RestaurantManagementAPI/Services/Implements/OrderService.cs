using Microsoft.EntityFrameworkCore;
using RestaurantManagementAPI.Data;
using RestaurantManagementAPI.DTOs;
using RestaurantManagementAPI.DTOs.MonAnDtos;
using RestaurantManagementAPI.Models.Entities;
using RestaurantManagementAPI.Services.Interfaces;
using System.Text.Json;

namespace RestaurantManagementAPI.Services.Implements
{
    public class OrderService : IOrderService
    {
        private readonly QLNHDbContext _context;

        public OrderService(QLNHDbContext context)
        {
            _context = context;
        }

        public async Task<List<HoaDonDto>> GetOrdersAsync()
        {
            return await _context.HOADON
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
        }

        public async Task<HoaDonDto?> GetOrderByIdAsync(string id)
        {
            var hd = await _context.HOADON
                .Include(h => h.ChiTietHoaDons)!.ThenInclude(ct => ct.MonAn)
                .FirstOrDefaultAsync(h => h.MaHD == id);

            if (hd == null) return null;

            return new HoaDonDto
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
        }

        public async Task<(bool Success, string Message, HoaDonDto? Data)> CreateOrderAsync(CreateHoaDonDto dto)
        {
            using var trans = await _context.Database.BeginTransactionAsync();
            try
            {
                var ban = await _context.BAN.FindAsync(dto.MaBan);
                if (ban == null) return (false, "Bàn không tồn tại", null);
                var nv = await _context.NHANVIEN.FindAsync(dto.MaNV);
                if (nv == null) return (false, $"Nhân viên có mã '{dto.MaNV}' không tồn tại.", null);
                var maHD = await GenerateMaHD();
                var hoaDon = new HoaDon
                {
                    MaHD = maHD,
                    MaBan = dto.MaBan,
                    MaNV = dto.MaNV,
                    NgayLap = DateTime.Now,
                    TrangThai = "Chưa thanh toán",
                    PaymentMethod = null,
                    TongTien = 0
                };

                decimal tongTienCalc = 0;
                var listChiTiet = new List<ChiTietHoaDon>();
                foreach (var item in dto.ChiTietHoaDons)
                {
                    var monAn = await _context.MONAN.FindAsync(item.MaMA);
                    if (monAn == null) throw new Exception($"Món ăn {item.MaMA} không tồn tại");
                    decimal thanhTienItem = item.SoLuong * monAn.DonGia;

                    var chiTiet = new ChiTietHoaDon
                    {
                        MaHD = maHD,
                        MaMA = item.MaMA,
                        SoLuong = item.SoLuong,
                        DonGia = monAn.DonGia,
                        TrangThai = "Đang chờ"
                    };

                    listChiTiet.Add(chiTiet);
                    tongTienCalc += thanhTienItem;
                }

                hoaDon.TongTien = tongTienCalc;
                _context.HOADON.Add(hoaDon);
                _context.CHITIETHOADON.AddRange(listChiTiet);

                ban.TrangThai = "Có khách";
                _context.BAN.Update(ban);

                await _context.SaveChangesAsync();
                await trans.CommitAsync();

                if (TcpSocketServer.Instance != null)
                {
  
                    var tablePayload = JsonSerializer.Serialize(new { MaBan = dto.MaBan, TrangThai = "Có khách" });
                    await TcpSocketServer.Instance.BroadcastAsync($"TABLE|{tablePayload}");

                    await TcpSocketServer.Instance.BroadcastAsync($"ORDER|NEW|{maHD}");
                }

                var resultDto = await GetOrderByIdAsync(maHD);
                return (true, "Tạo đơn thành công", resultDto);
            }
            catch (Exception ex)
            {
                await trans.RollbackAsync();
                return (false, "Lỗi hệ thống: " + ex.Message, null);
            }
        }

        public async Task<(bool Success, string Message)> UpdateOrderItemStatusAsync(string maHD, string maMA, string newStatus)
        {
            var item = await _context.CHITIETHOADON.FindAsync(maHD, maMA);

            if (item == null) return (false, "Chi tiết món không tồn tại");

            item.TrangThai = newStatus;
            await _context.SaveChangesAsync();
            return (true, "Cập nhật trạng thái món thành công");
        }

        public async Task<(bool Success, string Message)> UpdateOrderStatusAsync(string id, string newStatus)
        {
            var hd = await _context.HOADON.FindAsync(id);
            if (hd == null) return (false, "Hóa đơn không tồn tại");

            hd.TrangThai = newStatus;
            await _context.SaveChangesAsync();
            return (true, "Cập nhật trạng thái hóa đơn thành công");
        }

        public async Task<(bool Success, string Message, HoaDonDto? Data)> CheckoutAsync(string maHD, CheckoutRequestDto dto)
        {
            var hd = await _context.HOADON.FindAsync(maHD);
            if (hd == null) return (false, "Hóa đơn không tồn tại", null);

            if (hd.TrangThai == "Đã thanh toán")
                return (false, "Hóa đơn này đã được thanh toán trước đó", null);
            hd.TrangThai = "Đã thanh toán";
            hd.PaymentMethod = dto.PaymentMethod;

            var ban = await _context.BAN.FindAsync(hd.MaBan);
            if (ban != null)
            {
                ban.TrangThai = "Trống";
                _context.BAN.Update(ban);
            }

            await _context.SaveChangesAsync();
            if (TcpSocketServer.Instance != null && ban != null)
            {
                var payload = JsonSerializer.Serialize(new { MaBan = ban.MaBan, TrangThai = "Trống" });
                await TcpSocketServer.Instance.BroadcastAsync($"TABLE|{payload}");
            }

            return (true, "Thanh toán thành công", await GetOrderByIdAsync(maHD));
        }

        private async Task<string> GenerateMaHD()
        {
            var lastHD = await _context.HOADON
                .OrderByDescending(h => h.MaHD)
                .Select(h => h.MaHD)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(lastHD)) return "HD00001";

            if (lastHD.Length > 2 && int.TryParse(lastHD.Substring(2), out int num))
            {
                return $"HD{(num + 1):D5}";
            }
            return $"HD{DateTime.Now.Ticks}";
        }
    }
}