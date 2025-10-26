using Microsoft.AspNetCore.Authorization; // using Phân quyền
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurentManagementAPI.Data; // using DbContext
using RestaurentManagementAPI.DTOs; // using DTOs
using RestaurentManagementAPI.Models.Entities; // using Models
using System.Text.RegularExpressions;
namespace RestaurentManagementAPI.Controllers
{
 //   [Route("api/dishes")]
    [ApiController]
    public class DishesController : ControllerBase
    {
        private readonly QLNHDbContext _context;
        private readonly ILogger<DishesController> _logger;
        // 1. Tiêm DbContext
        public DishesController(QLNHDbContext context, ILogger<DishesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // --- GET /api/dishes ---
        // (Lấy danh sách các món CÒN BÁN)
        [HttpGet("api/get-dishes")]
        [AllowAnonymous] // Ai cũng xem được menu
        public async Task<ActionResult<IEnumerable<MonAnDto>>> GetDishes()
        {
            var dishes = await _context.MONAN // Dùng DbSet "MONAN"
                .Where(m => m.TrangThai == true) // Chỉ lấy món "TrangThai" = true
                .Select(m => new MonAnDto // Chuyển đổi sang DTO
                {
                    MaMA = m.MaMA,
                    TenMA = m.TenMA,
                    DonGia = m.DonGia,
                    Loai = m.Loai,
                    HinhAnh = m.HinhAnh
                })
                .ToListAsync();

            return Ok(dishes);
        }

        // --- GET /api/dishes/{maMA} ---
        // (Lấy chi tiết 1 món CÒN BÁN)
        [HttpGet("api/get-dishes-info")]
        [AllowAnonymous]
        public async Task<ActionResult<MonAnDto>> GetDish(string maMA)
        {
            var dishDto = await _context.MONAN
                .Where(m => m.MaMA == maMA && m.TrangThai == true) // Phải đúng mã và "TrangThai" = true
                .Select(m => new MonAnDto
                {
                    MaMA = m.MaMA,
                    TenMA = m.TenMA,
                    DonGia = m.DonGia,
                    Loai = m.Loai,
                    HinhAnh = m.HinhAnh
                })
                .FirstOrDefaultAsync();

            if (dishDto == null)
            {
                return NotFound(); // Không tìm thấy (hoặc món đã bị "xóa")
            }

            return Ok(dishDto);
        }

        // --- POST /api/dishes ---
        // (Thêm món ăn mới)
        [HttpPost("api/add-dishes")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<MonAnDto>> PostDish([FromBody] CreateMonAnDto createDto)
        {
            var prefix = SanitizePrefix(createDto.Loai);
            _logger.LogInformation("=== BẮT ĐẦU THÊM MÓN ===");
            _logger.LogInformation("Loại gốc: {Loai}, Prefix sau sanitize: {Prefix}", createDto.Loai, prefix);

            try
            {
                // Lấy TẤT CẢ các mã hiện có
                var allMaMAs = await _context.MONAN
                    .Where(m => m.MaMA.StartsWith(prefix))
                    .Select(m => m.MaMA)
                    .ToListAsync();

                _logger.LogInformation("Tìm thấy {Count} mã với prefix '{Prefix}': [{Codes}]",
                    allMaMAs.Count, prefix, string.Join(", ", allMaMAs));

                // Tách và parse số
                var numbers = allMaMAs
                    .Select(ma => ma.Substring(prefix.Length))
                    .ToList();

                _logger.LogInformation("Phần số sau khi tách: [{Numbers}]", string.Join(", ", numbers));

                int maxSoThuTu = allMaMAs
                    .Select(ma => ma.Substring(prefix.Length))
                    .Where(numPart => {
                        bool canParse = int.TryParse(numPart, out _);
                        if (!canParse) _logger.LogWarning("Không parse được: '{NumPart}'", numPart);
                        return canParse;
                    })
                    .Select(numPart => int.Parse(numPart))
                    .DefaultIfEmpty(0)
                    .Max();

                int newSoThuTu = maxSoThuTu + 1;
                string newMaMA = $"{prefix}{newSoThuTu:D3}";

                _logger.LogInformation("maxSoThuTu: {Max}, newSoThuTu: {New}, MÃ MỚI: {NewMaMA}",
                    maxSoThuTu, newSoThuTu, newMaMA);

                // Kiểm tra trùng lặp trước khi thêm
                var exists = await _context.MONAN.AnyAsync(m => m.MaMA == newMaMA);
                if (exists)
                {
                    _logger.LogError("MÃ {NewMaMA} ĐÃ TỒN TẠI TRONG DATABASE!", newMaMA);
                    return Conflict($"Mã {newMaMA} đã tồn tại. Logic sinh mã có vấn đề!");
                }

                var monAn = new MonAn
                {
                    MaMA = newMaMA,
                    TenMA = createDto.TenMA,
                    DonGia = createDto.DonGia,
                    Loai = createDto.Loai,
                    HinhAnh = createDto.HinhAnh,
                    TrangThai = true
                };

                _context.MONAN.Add(monAn);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✓ THÊM THÀNH CÔNG món {MaMA}", newMaMA);

                var monAnDto = new MonAnDto
                {
                    MaMA = monAn.MaMA,
                    TenMA = monAn.TenMA,
                    DonGia = monAn.DonGia,
                    Loai = monAn.Loai,
                    HinhAnh = monAn.HinhAnh
                };

                return CreatedAtAction(nameof(GetDish), new { maMA = monAn.MaMA }, monAnDto);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "✗ LỖI DbUpdateException. InnerException: {Inner}",
                    ex.InnerException?.Message);
                return Conflict($"Lỗi trùng lặp mã: {ex.InnerException?.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "✗ LỖI KHÔNG XÁC ĐỊNH");
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }
        private string SanitizePrefix(string loai)
        {
            // Chuyển "Món Nước" -> "Mon Nuoc"
            string normalized = loai.Normalize(System.Text.NormalizationForm.FormD);
            var regex = new Regex(@"\p{IsCombiningDiacriticalMarks}+");
            string noDiacritics = regex.Replace(normalized, string.Empty);

            // Chuyển "Mon Nuoc" -> "MONNUOC"
            return noDiacritics.Replace(" ", string.Empty).ToUpper();
        }
        // --- PUT /api/dishes/{maMA} ---
        // (Cập nhật món ăn)
        [HttpPut("api/update-dishes")]
        [Authorize(Roles = "Admin")] // Chỉ Admin được sửa
        public async Task<IActionResult> PutDish(string maMA, [FromBody] UpdateMonAnDto updateDto)
        {
            // Tìm món ăn đang còn "TrangThai" = true để sửa
            var monAn = await _context.MONAN.FirstOrDefaultAsync(m => m.MaMA == maMA && m.TrangThai == true);

            if (monAn == null)
            {
                return NotFound();
            }

            // Cập nhật thông tin
            monAn.TenMA = updateDto.TenMA;
            monAn.DonGia = updateDto.DonGia;
            monAn.Loai = updateDto.Loai;
            monAn.HinhAnh = updateDto.HinhAnh;

            _context.Entry(monAn).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent(); // Trả về 204 (Thành công, không cần nội dung)
        }

        // --- DELETE /api/dishes/{maMA} ---
        // (Xóa MỀM món ăn)
        [HttpDelete("api/delete-dishes")]
        [Authorize(Roles = "Admin")] // Chỉ Admin được xóa
        public async Task<IActionResult> DeleteDish(string maMA)
        {
            var monAn = await _context.MONAN.FindAsync(maMA);

            // Nếu không tìm thấy, hoặc món này đã bị "xóa" rồi, thì báo lỗi
            if (monAn == null || monAn.TrangThai == false)
            {
                return NotFound();
            }

            // *** LOGIC XÓA MỀM ***
            // Chỉ đổi TrangThai, không xóa thật
            monAn.TrangThai = false;
            _context.Entry(monAn).State = EntityState.Modified;

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}