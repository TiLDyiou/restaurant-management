using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementAPI.Data;
using RestaurantManagementAPI.DTOs;
using RestaurantManagementAPI.Models.Entities;

[ApiController]
[Route("api/dishes")]
public class DishesController : ControllerBase
{
    private readonly QLNHDbContext _context;
    private readonly ILogger<DishesController> _logger;

    public DishesController(QLNHDbContext context, ILogger<DishesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET /api/dishes
    [HttpGet("get-dishes")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<MonAnDto>>> GetDishes()
    {
        var dishes = await _context.MONAN
            .Where(m => m.TrangThai == true)
            .Select(m => new MonAnDto
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

    // GET /api/dishes/{maMA}
    [HttpGet("get-dish-info")]
    [AllowAnonymous]
    public async Task<ActionResult<MonAnDto>> GetDish(string maMA)
    {
        var dish = await _context.MONAN
            .Where(m => m.MaMA == maMA && m.TrangThai == true)
            .Select(m => new MonAnDto
            {
                MaMA = m.MaMA,
                TenMA = m.TenMA,
                DonGia = m.DonGia,
                Loai = m.Loai,
                HinhAnh = m.HinhAnh
            })
            .FirstOrDefaultAsync();

        if (dish == null) return NotFound();

        return Ok(dish);
    }

    // POST /api/add_dish
    [HttpPost("/api/add_dish")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<MonAnDto>> PostDish([FromBody] CreateMonAnDto createDto)
    {
        // Sinh mã món ăn
        string prefix = SanitizePrefix(createDto.Loai);
        var allMaMAs = await _context.MONAN
            .Where(m => m.MaMA.StartsWith(prefix))
            .Select(m => m.MaMA)
            .ToListAsync();

        int maxSo = allMaMAs
            .Select(m => int.TryParse(m.Substring(prefix.Length), out int n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        string newMaMA = $"{prefix}{(maxSo + 1):D3}";

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

    // PUT /api/update_dish/{maMA}
    [HttpPut("/api/update_dish/{maMA}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> PutDish(string maMA, [FromBody] UpdateMonAnDto updateDto)
    {
        var monAn = await _context.MONAN.FirstOrDefaultAsync(m => m.MaMA == maMA);
        if (monAn == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(updateDto.TenMA))
            monAn.TenMA = updateDto.TenMA;
        if (updateDto.DonGia.HasValue)
            monAn.DonGia = updateDto.DonGia.Value;
        if (!string.IsNullOrWhiteSpace(updateDto.Loai))
            monAn.Loai = updateDto.Loai;
        if (!string.IsNullOrWhiteSpace(updateDto.HinhAnh))
            monAn.HinhAnh = updateDto.HinhAnh;
        if (updateDto.TrangThai.HasValue)
            monAn.TrangThai = updateDto.TrangThai.Value;

        _context.Entry(monAn).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE /api/softdelete_dish/{maMA}
    [HttpDelete("/api/softdelete_dish/{maMA}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteDish(string maMA)
    {
        var monAn = await _context.MONAN.FindAsync(maMA);
        if (monAn == null || monAn.TrangThai == false) return NotFound();

        monAn.TrangThai = false;
        _context.Entry(monAn).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private string SanitizePrefix(string loai)
    {
        string normalized = loai.Normalize(System.Text.NormalizationForm.FormD);
        var regex = new System.Text.RegularExpressions.Regex(@"\p{IsCombiningDiacriticalMarks}+");
        string noDiacritics = regex.Replace(normalized, string.Empty);
        return noDiacritics.Replace(" ", string.Empty).ToUpper();
    }
}
