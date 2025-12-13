using Microsoft.EntityFrameworkCore;
using RestaurantManagementAPI.Data;
using RestaurantManagementAPI.DTOs;
using RestaurantManagementAPI.Models.Entities;
using RestaurantManagementAPI.Services.Interfaces;
using System.Text;
using System.Text.RegularExpressions;

namespace RestaurantManagementAPI.Services.Implements
{
    public class DishService : IDishService
    {
        private readonly QLNHDbContext _context;

        public DishService(QLNHDbContext context)
        {
            _context = context;
        }

        public async Task<List<MonAnDto>> GetAllDishesAsync()
        {
            return await _context.MONAN
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
        }

        public async Task<MonAnDto?> GetDishByIdAsync(string maMA)
        {
            return await _context.MONAN
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
        }

        public async Task<(bool Success, string Message, MonAnDto? Data)> CreateDishAsync(CreateMonAnDto dto)
        {
            string prefix = SanitizePrefix(dto.Loai);
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
                TenMA = dto.TenMA,
                DonGia = dto.DonGia,
                Loai = dto.Loai,
                HinhAnh = dto.HinhAnh,
                TrangThai = true
            };

            _context.MONAN.Add(monAn);
            await _context.SaveChangesAsync();

            var resultDto = new MonAnDto
            {
                MaMA = monAn.MaMA,
                TenMA = monAn.TenMA,
                DonGia = monAn.DonGia,
                Loai = monAn.Loai,
                HinhAnh = monAn.HinhAnh
            };

            return (true, "Thêm món ăn thành công", resultDto);
        }

        public async Task<(bool Success, string Message)> UpdateDishAsync(string maMA, UpdateMonAnDto dto)
        {
            var monAn = await _context.MONAN.FirstOrDefaultAsync(m => m.MaMA == maMA);
            if (monAn == null) return (false, "Món ăn không tồn tại");

            if (!string.IsNullOrWhiteSpace(dto.TenMA)) monAn.TenMA = dto.TenMA;
            if (dto.DonGia.HasValue) monAn.DonGia = dto.DonGia.Value;
            if (!string.IsNullOrWhiteSpace(dto.Loai)) monAn.Loai = dto.Loai;
            if (!string.IsNullOrWhiteSpace(dto.HinhAnh)) monAn.HinhAnh = dto.HinhAnh;
            if (dto.TrangThai.HasValue) monAn.TrangThai = dto.TrangThai.Value;

            _context.Entry(monAn).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return (true, "Cập nhật thành công");
        }

        public async Task<(bool Success, string Message)> SoftDeleteDishAsync(string maMA)
        {
            var monAn = await _context.MONAN.FindAsync(maMA);
            if (monAn == null || monAn.TrangThai == false) return (false, "Món ăn không tồn tại hoặc đã bị xóa");

            monAn.TrangThai = false;
            _context.Entry(monAn).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return (true, "Xóa món ăn thành công");
        }

        private string SanitizePrefix(string loai)
        {
            string normalized = loai.Normalize(NormalizationForm.FormD);
            var regex = new Regex(@"\p{IsCombiningDiacriticalMarks}+");
            string noDiacritics = regex.Replace(normalized, string.Empty);
            return noDiacritics.Replace(" ", string.Empty).ToUpper();
        }
    }
}