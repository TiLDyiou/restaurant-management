using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RestaurentManagementAPI.Data;
using RestaurentManagementAPI.DTOs.MonAnDtos;
using RestaurentManagementAPI.Hubs;
using RestaurentManagementAPI.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RestaurentManagementAPI.Services
{
    public class OrderService : IOrderService
    {
        private readonly QLNHDbContext _context;
        private readonly IHubContext<KitchenHub> _kitchenHubContext;
        private readonly IHubContext<BanHub> _banHubContext;

        public OrderService(QLNHDbContext context,
                            IHubContext<KitchenHub> kitchenHubContext,
                            IHubContext<BanHub> banHubContext)
        {
            _context = context;
            _kitchenHubContext = kitchenHubContext;
            _banHubContext = banHubContext;
        }

        // Sinh mã hóa đơn
        private async Task<string> GenerateNewHoaDonIdAsync()
        {
            var lastHoaDon = await _context.HOADON.OrderByDescending(h => h.MaHD).FirstOrDefaultAsync();
            if (lastHoaDon == null) return "HD001";

            string numberPart = lastHoaDon.MaHD.Substring(2);
            if (int.TryParse(numberPart, out int lastNumber))
            {
                return $"HD{lastNumber + 1:D3}";
            }
            throw new Exception("Lỗi sinh mã hóa đơn.");
        }

        // Lấy danh sách đơn
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
                })
                .ToListAsync();
        }

        // Lấy đơn theo id
        public async Task<HoaDonDto> GetOrderByIdAsync(string id)
        {
            var h = await _context.HOADON
                .Include(x => x.ChiTietHoaDons).ThenInclude(ct => ct.MonAn)
                .FirstOrDefaultAsync(x => x.MaHD == id);

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

        // Tạo đơn
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

                    var chiTiet = new ChiTietHoaDon
                    {
                        MaHD = newMaHD,
                        MaMA = itemDto.MaMA,
                        SoLuong = itemDto.SoLuong,
                        DonGia = monAn.DonGia,
                        TrangThai = "Chờ làm"
                    };
                    chiTietEntities.Add(chiTiet);
                    tongTien += itemDto.SoLuong * monAn.DonGia;
                }

                var hoaDon = new HoaDon
                {
                    MaHD = newMaHD,
                    MaBan = createDto.MaBan,
                    MaNV = createDto.MaNV,
                    NgayLap = DateTime.Now,
                    TongTien = tongTien,
                    TrangThai = "Chờ xử lý"
                };

                await _context.HOADON.AddAsync(hoaDon);
                await _context.CHITIETHOADON.AddRangeAsync(chiTietEntities);

                // Cập nhật trạng thái bàn -> "Bàn bận"
                var ban = await _context.BAN.FindAsync(createDto.MaBan);
                if (ban != null)
                {
                    ban.TrangThai = "Bàn bận";
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Gửi SignalR cho Bếp
                var orderForKitchen = new HoaDonDto
                {
                    MaHD = hoaDon.MaHD,
                    MaBan = hoaDon.MaBan,
                    NgayLap = hoaDon.NgayLap,
                    TrangThai = hoaDon.TrangThai,
                    ChiTietHoaDons = chiTietEntities.Select(ct => new ChiTietHoaDonViewDto
                    {
                        MaMA = ct.MaMA,
                        TenMA = _context.MONAN.FirstOrDefault(m => m.MaMA == ct.MaMA)?.TenMA ?? "Món",
                        SoLuong = ct.SoLuong,
                        TrangThai = ct.TrangThai
                    }).ToList()
                };
                await _kitchenHubContext.Clients.All.SendAsync("ReceiveOrder", orderForKitchen);

                // Gửi SignalR cập nhật Bàn -> "BanUpdated"
                await _banHubContext.Clients.All.SendAsync("BanUpdated", new
                {
                    MaBan = createDto.MaBan,
                    TrangThai = "Bàn bận"
                });

                return orderForKitchen;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // CẬP NHẬT TRẠNG THÁI MÓN (UPDATE DISH)
        public async Task UpdateOrderItemStatusAsync(string maHD, string maMA, string newStatus)
        {
            var orderItem = await _context.CHITIETHOADON.FirstOrDefaultAsync(ct => ct.MaHD == maHD && ct.MaMA == maMA);
            if (orderItem == null) throw new Exception("Không tìm thấy món ăn");

            orderItem.TrangThai = newStatus;

            // Logic tự động hoàn thành đơn nếu tất cả món xong
            if (newStatus == "Đã xong")
            {
                var pendingItems = await _context.CHITIETHOADON
                    .Where(ct => ct.MaHD == maHD && (ct.TrangThai == "Chờ làm" || ct.TrangThai == "Đang làm"))
                    .AnyAsync();

                if (!pendingItems)
                {
                    var hoaDon = await _context.HOADON.FindAsync(maHD);
                    if (hoaDon != null) hoaDon.TrangThai = "Đã hoàn thành";
                }
            }
            await _context.SaveChangesAsync();
        }

        // CẬP NHẬT TRẠNG THÁI ĐƠN (UPDATE ORDER)
        public async Task UpdateOrderStatusAsync(string id, string newStatus)
        {
            var hoaDon = await _context.HOADON.FindAsync(id);
            if (hoaDon == null) throw new Exception("Không tìm thấy hóa đơn");

            hoaDon.TrangThai = newStatus;
            await _context.SaveChangesAsync();
        }

        // THANH TOÁN (CHECKOUT)
        public async Task<HoaDonDto> CheckoutAsync(string maHD, CheckoutRequestDto checkoutDto)
        {
            var hoaDon = await _context.HOADON.Include(h => h.Ban).Include(h => h.ChiTietHoaDons).ThenInclude(ct => ct.MonAn)
                                              .FirstOrDefaultAsync(h => h.MaHD == maHD);

            if (hoaDon == null) throw new Exception("Không tìm thấy hóa đơn");
            if (hoaDon.TrangThai == "Đã thanh toán") throw new Exception("Đơn này đã thanh toán rồi");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                hoaDon.TrangThai = "Đã thanh toán";
                hoaDon.PaymentMethod = checkoutDto.PaymentMethod;

                // Trả bàn về "Bàn trống"
                if (hoaDon.Ban != null)
                {
                    hoaDon.Ban.TrangThai = "Bàn trống";
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Gửi SignalR trả bàn -> "BanUpdated"
                await _banHubContext.Clients.All.SendAsync("BanUpdated", new
                {
                    MaBan = hoaDon.MaBan,
                    TrangThai = "Bàn trống"
                });

                // Map kết quả trả về để in hóa đơn
                return new HoaDonDto
                {
                    MaHD = hoaDon.MaHD,
                    MaBan = hoaDon.MaBan,
                    MaNV = hoaDon.MaNV,
                    NgayLap = hoaDon.NgayLap,
                    TongTien = hoaDon.TongTien,
                    TrangThai = hoaDon.TrangThai,
                    ChiTietHoaDons = hoaDon.ChiTietHoaDons.Select(ct => new ChiTietHoaDonViewDto
                    {
                        MaMA = ct.MaMA,
                        TenMA = ct.MonAn?.TenMA ?? "Món xóa",
                        SoLuong = ct.SoLuong,
                        DonGia = ct.DonGia,
                        ThanhTien = ct.ThanhTien
                    }).ToList()
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}