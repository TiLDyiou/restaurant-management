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

        // ---------------------------------------------------------------------
        // HÀM TẠO MÃ HOÁ ĐƠN TỰ ĐỘNG (CẦN THIẾT)
        // ---------------------------------------------------------------------
        // Do MaHD của bạn là string(5) và không tự tăng, chúng ta cần 1 logic để tạo mã mới.
        // Đây là một ví dụ đơn giản. Bạn nên cải tiến nó để đảm bảo an toàn trong môi trường đa luồng.
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

        // ---------------------------------------------------------------------
        // API: GET /api/orders [cite: 74]
        // Lấy danh sách tất cả đơn hàng (đã bao gồm chi tiết)
        // ---------------------------------------------------------------------
        [HttpGet("get-all-orders-info")]
        public async Task<ActionResult<IEnumerable<HoaDonDto>>> GetOrders()
        {
            var hoaDons = await _context.HOADON
                .AsNoTracking() // Hiệu quả hơn cho việc chỉ đọc
                .Include(h => h.ChiTietHoaDons) // Tải ChiTietHoaDon
                    .ThenInclude(ct => ct.MonAn) // Tải MonAn từ ChiTietHoaDon
                .OrderByDescending(h => h.NgayLap) // Mới nhất lên trước
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
                // (Logic nâng cao):
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

                // (SignalR Nâng cao): 
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

        // ---------------------------------------------------------------------
        // API: POST /api/orders 
        // Tạo một đơn hàng mới (Hoá đơn)
        // ---------------------------------------------------------------------
        [HttpPost("api/create-and-send-orders")]
        public async Task<ActionResult<HoaDonDto>> CreateOrder([FromBody] CreateHoaDonDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Sử dụng Transaction để đảm bảo tính toàn vẹn dữ liệu
            // Hoặc tạo HoaDon và ChiTietHoaDon thành công, hoặc rollback tất cả.
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                decimal tongTien = 0;
                var chiTietEntities = new List<ChiTietHoaDon>();
                var newMaHD = await GenerateNewHoaDonIdAsync();
                HoaDonDto hoaDonDtoToReturn;
                // 1. Xử lý các chi tiết hoá đơn
                foreach (var itemDto in createDto.ChiTietHoaDons)
                {
                    var monAn = await _context.MONAN.FindAsync(itemDto.MaMA);

                    // Kiểm tra món ăn có tồn tại và còn bán không
                    if (monAn == null || !monAn.TrangThai)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest($"Món ăn với mã {itemDto.MaMA} không tồn tại hoặc đã ngừng bán.");
                    }

                    // Tính thành tiền (Dựa trên DonGia từ DB, không tin tưởng client)
                    // ThanhTien trong DB của bạn là cột tự tính, nhưng ta cần DonGia

                    var chiTiet = new ChiTietHoaDon
                    {
                        MaHD = newMaHD, // Gán mã HD mới
                        MaMA = itemDto.MaMA,
                        SoLuong = itemDto.SoLuong,
                        DonGia = monAn.DonGia, // Lấy đơn giá từ DB
                        // ThanhTien sẽ được DB tự tính (như trong OnModelCreating)
                        TrangThai = "Chờ làm"
                    };

                    chiTietEntities.Add(chiTiet);
                    tongTien += itemDto.SoLuong * monAn.DonGia;
                }

                // 2. Tạo đối tượng Hoá Đơn
                var hoaDon = new HoaDon
                {
                    MaHD = newMaHD,
                    MaBan = createDto.MaBan,
                    MaNV = createDto.MaNV,
                    NgayLap = DateTime.UtcNow, // Giờ server (UTC)
                    TongTien = tongTien,
                    TrangThai = "Chờ xử lý" // Trạng thái ban đầu
                };

                // 3. Lưu vào Database
                await _context.HOADON.AddAsync(hoaDon);
                await _context.CHITIETHOADON.AddRangeAsync(chiTietEntities);

                // 4. Cập nhật trạng thái Bàn
                var ban = await _context.BAN.FindAsync(createDto.MaBan);
                if (ban != null)
                {
                    ban.TrangThai = "Đang có khách";
                }

                // 5. Lưu tất cả thay đổi
                await _context.SaveChangesAsync();

                // 6. Commit transaction
                await transaction.CommitAsync();
                hoaDonDtoToReturn = new HoaDonDto
                {
                    MaHD = hoaDon.MaHD,
                    MaBan = hoaDon.MaBan,
                    MaNV = hoaDon.MaNV,
                    NgayLap = hoaDon.NgayLap,
                    TongTien = hoaDon.TongTien,
                    TrangThai = hoaDon.TrangThai, // Trạng thái tổng
                    ChiTietHoaDons = chiTietEntities.Select(ct => new ChiTietHoaDonViewDto
                    {
                        MaMA = ct.MaMA,
                        TenMA = _context.MONAN.Find(ct.MaMA)?.TenMA ?? "N/A",
                        SoLuong = ct.SoLuong,
                        DonGia = ct.DonGia,
                        // ThanhTien sẽ là 0 vì chưa có trong DB,
                        // Cần tính thủ công nếu muốn hiển thị ngay
                        ThanhTien = ct.SoLuong * ct.DonGia,

                        // GỬI TRẠNG THÁI MÓN ĂN:
                        TrangThai = ct.TrangThai
                    }).ToList()
                };

                // Gửi qua SignalR
                await _kitchenHubContext.Clients.Group("Kitchen")
                    .SendAsync("ReceiveOrder", hoaDonDtoToReturn);

                // Lấy lại dữ liệu vừa tạo để trả về (nếu cần)
                // (Bỏ qua bước này để đơn giản, trả về CreatedAtAction)

                return CreatedAtAction(nameof(GetOrderById), new { id = hoaDon.MaHD }, hoaDonDtoToReturn);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Lỗi nội bộ server: {ex.Message}");
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

                // (Nâng cao): Bạn cũng có thể dùng SignalR tại đây
                // để thông báo cho Nhân viên/Quản lý biết là Bếp đã làm xong.
                // Ví dụ: await _staffHubContext.Clients.All.SendAsync("OrderReady", id);

                return NoContent(); // Trả về 204 No Content (thành công, không có nội dung)
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi cập nhật trạng thái: {ex.Message}");
            }
        }
    }
}