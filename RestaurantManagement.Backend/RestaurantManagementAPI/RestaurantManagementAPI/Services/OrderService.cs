using Microsoft.EntityFrameworkCore;
using RestaurantManagementAPI.Data;
using RestaurantManagementAPI.DTOs.MonAnDtos;
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

        private async Task<string> GenerateNewHoaDonIdAsync()
        {
            var lastHoaDon = await _context.HOADON.OrderByDescending(h => h.MaHD).FirstOrDefaultAsync();
            if (lastHoaDon == null) return "HD001";
            string numberPart = lastHoaDon.MaHD.Substring(2);
            if (int.TryParse(numberPart, out int lastNumber)) return $"HD{lastNumber + 1:D3}";
            return "HD001";
        }

        public async Task<IEnumerable<HoaDonDto>> GetOrdersAsync()
        {
            return await _context.HOADON
                .AsNoTracking()
                .Include(h => h.ChiTietHoaDons).ThenInclude(ct => ct.MonAn)
                .OrderByDescending(h => h.NgayLap)
                .Select(h => new HoaDonDto
                {
                    MaHD = h.MaHD,
                    MaBan = h.MaBan,
                    MaNV = h.MaNV,
                    NgayLap = h.NgayLap,
                    TongTien = h.TongTien,
                    TrangThai = h.TrangThai,
                    ChiTietHoaDons = h.ChiTietHoaDons.Select(ct => new ChiTietHoaDonViewDto
                    {
                        MaMA = ct.MaMA,
                        TenMA = ct.MonAn != null ? ct.MonAn.TenMA : "N/A",
                        SoLuong = ct.SoLuong,
                        DonGia = ct.DonGia,
                        ThanhTien = ct.ThanhTien,
                        TrangThai = ct.TrangThai
                    }).ToList()
                }).ToListAsync();
        }

        public async Task<HoaDonDto> GetOrderByIdAsync(string id)
        {
            var h = await _context.HOADON.Include(x => x.ChiTietHoaDons).ThenInclude(ct => ct.MonAn).FirstOrDefaultAsync(x => x.MaHD == id);
            if (h == null) return null;
            return new HoaDonDto
            {
                MaHD = h.MaHD,
                MaBan = h.MaBan,
                MaNV = h.MaNV,
                NgayLap = h.NgayLap,
                TongTien = h.TongTien,
                TrangThai = h.TrangThai,
                ChiTietHoaDons = h.ChiTietHoaDons.Select(ct => new ChiTietHoaDonViewDto
                {
                    MaMA = ct.MaMA,
                    TenMA = ct.MonAn?.TenMA,
                    SoLuong = ct.SoLuong,
                    DonGia = ct.DonGia,
                    ThanhTien = ct.ThanhTien,
                    TrangThai = ct.TrangThai
                }).ToList()
            };
        }

        public async Task<HoaDonDto> CreateOrderAsync(CreateHoaDonDto createDto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                decimal tongTien = 0;
                var chiTietEntities = new List<ChiTietHoaDon>();
                var newMaHD = await GenerateNewHoaDonIdAsync();

                foreach (var itemDto in createDto.ChiTietHoaDons)
                {
                    var monAn = await _context.MONAN.FindAsync(itemDto.MaMA);
                    if (monAn == null) throw new Exception($"Món {itemDto.MaMA} không tồn tại");

                    var chiTiet = new ChiTietHoaDon { MaHD = newMaHD, MaMA = itemDto.MaMA, SoLuong = itemDto.SoLuong, DonGia = monAn.DonGia, TrangThai = "Chờ làm" };
                    chiTietEntities.Add(chiTiet);
                    tongTien += itemDto.SoLuong * monAn.DonGia;
                }

                var hoaDon = new HoaDon { MaHD = newMaHD, MaBan = createDto.MaBan, MaNV = createDto.MaNV, NgayLap = DateTime.Now, TongTien = tongTien, TrangThai = "Chờ xử lý" };
                await _context.HOADON.AddAsync(hoaDon);
                await _context.CHITIETHOADON.AddRangeAsync(chiTietEntities);

                var ban = await _context.BAN.FindAsync(createDto.MaBan);
                if (ban != null) ban.TrangThai = "Bàn bận";

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // GỬI REALTIME QUA SOCKET 
                var orderForKitchen = new HoaDonDto
                {
                    MaHD = hoaDon.MaHD,
                    MaBan = hoaDon.MaBan,
                    NgayLap = hoaDon.NgayLap,
                    TrangThai = hoaDon.TrangThai,
                    ChiTietHoaDons = chiTietEntities.Select(ct => new ChiTietHoaDonViewDto
                    {
                        MaMA = ct.MaMA,
                        TenMA = _context.MONAN.Find(ct.MaMA)?.TenMA,
                        SoLuong = ct.SoLuong,
                        TrangThai = ct.TrangThai
                    }).ToList()
                };

                if (TcpSocketServer.Instance != null)
                {
                    // Gửi cho Bếp: "ORDER|{json}"
                    string jsonOrder = JsonSerializer.Serialize(orderForKitchen);
                    await TcpSocketServer.Instance.BroadcastAsync($"ORDER|{jsonOrder}");

                    // Cập nhật Bàn: "TABLE|{json}"
                    string jsonTable = JsonSerializer.Serialize(new { MaBan = createDto.MaBan, TrangThai = "Bàn bận" });
                    await TcpSocketServer.Instance.BroadcastAsync($"TABLE|{jsonTable}");
                }

                return orderForKitchen;
            }
            catch { await transaction.RollbackAsync(); throw; }
        }

        public async Task UpdateOrderItemStatusAsync(string maHD, string maMA, string newStatus)
        {
            var item = await _context.CHITIETHOADON.FindAsync(maHD, maMA);
            if (item != null)
            {
                item.TrangThai = newStatus;
                if (newStatus == "Đã xong")
                {
                    bool conMonChuaLam = await _context.CHITIETHOADON.AnyAsync(ct => ct.MaHD == maHD && (ct.TrangThai == "Chờ làm" || ct.TrangThai == "Đang làm"));
                    if (!conMonChuaLam)
                    {
                        var hd = await _context.HOADON.FindAsync(maHD);
                        if (hd != null) hd.TrangThai = "Đã hoàn thành";
                    }
                }
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateOrderStatusAsync(string id, string newStatus)
        {
            var hd = await _context.HOADON.FindAsync(id);
            if (hd != null)
            {
                hd.TrangThai = newStatus;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<HoaDonDto> CheckoutAsync(string maHD, CheckoutRequestDto checkoutDto)
        {
            var hoaDon = await _context.HOADON.Include(h => h.Ban).FirstOrDefaultAsync(h => h.MaHD == maHD);

            if (hoaDon == null) throw new Exception("Không tìm thấy hóa đơn");
            if (hoaDon.TrangThai == "Đã thanh toán") throw new Exception("Đơn này đã thanh toán rồi");

            // 1. CẬP NHẬT TRẠNG THÁI (Khớp với Controller)
            hoaDon.TrangThai = "Đã thanh toán";

            // 2. CẬP NHẬT GIỜ THANH TOÁN (Khắc phục lỗi không hiện doanh thu hôm nay)
            hoaDon.NgayLap = DateTime.Now;

            hoaDon.PaymentMethod = checkoutDto.PaymentMethod;

            // 3. Trả bàn
            if (hoaDon.Ban != null)
            {
                hoaDon.Ban.TrangThai = "Bàn trống"; // Hoặc "Trống" tùy DB của bạn
            }

            await _context.SaveChangesAsync();

            // Gửi Socket (Giữ nguyên)
            if (TcpSocketServer.Instance != null)
            {
                string jsonTable = JsonSerializer.Serialize(new { MaBan = hoaDon.MaBan, TrangThai = "Bàn trống" });
                await TcpSocketServer.Instance.BroadcastAsync($"TABLE|{jsonTable}");
            }

            return new HoaDonDto { MaHD = maHD };
        }
    }
}