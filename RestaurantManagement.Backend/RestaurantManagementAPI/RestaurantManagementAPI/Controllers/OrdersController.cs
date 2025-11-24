using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurentManagementAPI.Data;
using RestaurentManagementAPI.DTOs.MonAnDtos;
using RestaurentManagementAPI.Models.Entities;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR; // <-- Thêm thư viện SignalR
using RestaurentManagementAPI.Hubs;
namespace RestaurentManagementAPI.Controllers
{
    [Route("api/orders")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly QLNHDbContext _context;
        private readonly IHubContext<KitchenHub> _kitchenHubContext;
        public OrdersController(QLNHDbContext context, IHubContext<KitchenHub> kitchenHubContext)
        {
            _context = context;
            _kitchenHubContext = kitchenHubContext;
        }

        
        private async Task<string> GenerateNewHoaDonIdAsync()
        {
            // Lấy MaHD cuối cùng, ví dụ: "HD001" -> "HD999"
            var lastHoaDon = await _context.HOADON
                                .OrderByDescending(h => h.MaHD)
                                .FirstOrDefaultAsync();

            if (lastHoaDon == null)
            {
                return "HD001"; // Bắt đầu
            }

            // Tách phần số "001"
            string numberPart = lastHoaDon.MaHD.Substring(2); // Bỏ "HD"
            if (int.TryParse(numberPart, out int lastNumber))
            {
                int newNumber = lastNumber + 1;
                return $"HD{newNumber:D3}"; // Format thành 3 chữ số, ví dụ "HD002", "HD010"
            }

            // Fallback nếu logic lỗi (ví dụ: MaHD không đúng định dạng)
            throw new Exception("Không thể tạo mã hoá đơn mới.");
        }

        // API: GET /api/orders 
        // Lấy danh sách tất cả đơn hàng (đã bao gồm chi tiết)
        [HttpGet("get-all-orders-info")]
        public async Task<ActionResult<IEnumerable<HoaDonDto>>> GetOrders()
        {
            var hoaDons = await _context.HOADON
                .AsNoTracking() 
                .Include(h => h.ChiTietHoaDons) // Tải ChiTietHoaDon
                    .ThenInclude(ct => ct.MonAn) // Tải MonAn từ ChiTietHoaDon
                .OrderByDescending(h => h.NgayLap) 
                .Select(h => new HoaDonDto // Sử dụng Select (Projection) để map sang DTO
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
                        TenMA = ct.MonAn != null ? ct.MonAn.TenMA : "N/A", // Lấy tên món ăn
                        SoLuong = ct.SoLuong,
                        DonGia = ct.DonGia,
                        ThanhTien = ct.ThanhTien,
                        TrangThai = ct.TrangThai
                    }).ToList()
                })
                .ToListAsync();

            return Ok(hoaDons);
        }
        [HttpPut("update-dishes-status")]
        public async Task<IActionResult> UpdateOrderItemStatus(string maHD, string maMA, [FromBody] UpdateOrderItemStatusDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Tìm ChiTietHoaDon bằng Khóa chính phức hợp (MaHD, MaMA)
            var orderItem = await _context.CHITIETHOADON
                                .FirstOrDefaultAsync(ct => ct.MaHD == maHD && ct.MaMA == maMA);

            if (orderItem == null)
            {
                return NotFound($"Không tìm thấy món ăn {maMA} trong hóa đơn {maHD}.");
            }

            // Cập nhật trạng thái của món ăn
            orderItem.TrangThai = updateDto.NewStatus;

            try
            {
                
                // Sau khi cập nhật món này, kiểm tra xem có phải
                // tất cả món khác trong đơn này đều "Đã xong" không.
                if (updateDto.NewStatus == "Đã xong")
                {
                    // Đếm xem còn món nào "Chờ làm" hoặc "Đang làm" không
                    var pendingItems = await _context.CHITIETHOADON
                        .Where(ct => ct.MaHD == maHD &&
                                     (ct.TrangThai == "Chờ làm" || ct.TrangThai == "Đang làm"))
                        .AnyAsync();

                    // Nếu KHÔNG còn món nào chờ (pendingItems == false)
                    if (!pendingItems)
                    {
                        // Cập nhật trạng thái của HÓA ĐƠN TỔNG
                        var hoaDon = await _context.HOADON.FindAsync(maHD);
                        if (hoaDon != null)
                        {
                            hoaDon.TrangThai = "Đã hoàn thành";
                        }
                    }
                }

                await _context.SaveChangesAsync();

                
                // Gửi thông báo real-time về cho Nhân viên/Quản lý
                // biết món ăn {maMA} đã xong.
                // await _staffHub.Clients.All.SendAsync("ItemStatusUpdated", maHD, maMA, updateDto.NewStatus);

                return NoContent(); // 204 No Content
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi cập nhật trạng thái món ăn: {ex.Message}");
            }
        }

        // API: POST /api/orders 
        // Tạo một đơn hàng mới (Hoá đơn)
        [HttpPost("api/create-and-send-orders")]
        public async Task<ActionResult<HoaDonDto>> CreateOrder([FromBody] CreateHoaDonDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                decimal tongTien = 0;
                var chiTietEntities = new List<ChiTietHoaDon>();
                var newMaHD = await GenerateNewHoaDonIdAsync();

                // 1. Xử lý chi tiết hóa đơn
                foreach (var itemDto in createDto.ChiTietHoaDons)
                {
                    var monAn = await _context.MONAN.FindAsync(itemDto.MaMA);
                    if (monAn == null)
                    {
                        await transaction.RollbackAsync(); // Quan trọng: Rollback nếu lỗi
                        return BadRequest($"Món ăn {itemDto.MaMA} không tồn tại.");
                    }

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

                // 2. Tạo Hóa Đơn
                var hoaDon = new HoaDon
                {
                    MaHD = newMaHD,
                    MaBan = createDto.MaBan,
                    MaNV = createDto.MaNV,
                    NgayLap = DateTime.Now, // Dùng giờ hiện tại của Server
                    TongTien = tongTien,
                    TrangThai = "Chờ xử lý"
                };

                await _context.HOADON.AddAsync(hoaDon);
                await _context.CHITIETHOADON.AddRangeAsync(chiTietEntities);

                // 3. Cập nhật trạng thái Bàn
                var ban = await _context.BAN.FindAsync(createDto.MaBan);
                if (ban != null)
                {
                    ban.TrangThai = "Có người";
                }

                // 4. Lưu vào DB
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // =========================================================
                // 5. GỬI SIGNALR NGAY TẠI ĐÂY (Đảm bảo chạy 100%)
                // =========================================================

                // Tạo dữ liệu để gửi sang Bếp (Map từ Entity sang DTO)
                var orderForKitchen = new HoaDonDto
                {
                    MaHD = hoaDon.MaHD,
                    MaBan = hoaDon.MaBan,
                    NgayLap = hoaDon.NgayLap,
                    TrangThai = hoaDon.TrangThai,
                    ChiTietHoaDons = chiTietEntities.Select(ct => new ChiTietHoaDonViewDto
                    {
                        MaMA = ct.MaMA,
                        TenMA = _context.MONAN.FirstOrDefault(m => m.MaMA == ct.MaMA)?.TenMA ?? "Món lạ",
                        SoLuong = ct.SoLuong,
                        TrangThai = ct.TrangThai
                    }).ToList()
                };

                // GỬI CHO TẤT CẢ (Clients.All) ĐỂ CHẮC CHẮN BẾP NHẬN ĐƯỢC
                await _kitchenHubContext.Clients.All.SendAsync("ReceiveOrder", orderForKitchen);

                // =========================================================

                return Ok(orderForKitchen);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Ghi log ra cửa sổ Output để bạn thấy lỗi nếu có
                System.Diagnostics.Debug.WriteLine("LỖI CREATE ORDER: " + ex.ToString());
                return StatusCode(500, "Lỗi Server: " + ex.Message);
            }
        }


        // Hàm hỗ trợ cho CreatedAtAction (Bạn có thể tạo API Get theo ID đầy đủ sau)
        [HttpGet("api/orders/get-{id}-order-info")]
        public async Task<ActionResult<HoaDonDto>> GetOrderById(string id)
        {
            var hoaDon = await _context.HOADON.FindAsync(id);
            if (hoaDon == null) return NotFound();
            // Tạm thời trả về entity, nên map sang DTO như hàm GetOrders()
            return Ok(hoaDon);
        }
        [HttpPut("update-all-dishes-in-{id}-order-status")]
        public async Task<IActionResult> UpdateOrderStatus(string id, [FromBody] UpdateOrderStatusDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var hoaDon = await _context.HOADON.FindAsync(id);

            if (hoaDon == null)
            {
                return NotFound($"Không tìm thấy hoá đơn với mã: {id}");
            }

            // Cập nhật trạng thái
            hoaDon.TrangThai = updateDto.NewStatus;

            try
            {
                await _context.SaveChangesAsync();

                
               

                return NoContent(); // Trả về 204 No Content (thành công, không có nội dung)
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi cập nhật trạng thái: {ex.Message}");
            }
        }
    }
}
