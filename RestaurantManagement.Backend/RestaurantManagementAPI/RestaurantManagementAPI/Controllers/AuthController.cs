using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RestaurentManagementAPI.Data;
using RestaurentManagementAPI.DTOs;
using RestaurentManagementAPI.Models.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RestaurentManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly QLNHDbContext _context;
        private readonly IConfiguration _config;

        public AuthController(QLNHDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        #region ===================== Public APIs =====================

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.TenDangNhap) || string.IsNullOrWhiteSpace(dto.MatKhau))
                return BadRequest("Tên đăng nhập và mật khẩu không được để trống.");

            if (await _context.TAIKHOAN.AnyAsync(t => t.TenDangNhap == dto.TenDangNhap))
                return Conflict("Tên đăng nhập đã tồn tại.");

            // Sinh MaNV tự động
            var newMaNV = await GenerateNewMaNV();

            // Tạo nhân viên
            var nv = new NhanVien
            {
                MaNV = newMaNV,
                HoTen = dto.HoTen,
                ChucVu = dto.ChucVu,
                SDT = dto.SDT,
                NgayVaoLam = DateTime.Now,
                TrangThai = "Đang làm"
            };
            _context.NHANVIEN.Add(nv);

            // Tạo tài khoản
            var tk = new TaiKhoan
            {
                TenDangNhap = dto.TenDangNhap,
                MatKhau = BCrypt.Net.BCrypt.HashPassword(dto.MatKhau),
                MaNV = newMaNV,
                Quyen = string.IsNullOrWhiteSpace(dto.Quyen) ? "NhanVien" : dto.Quyen
            };
            _context.TAIKHOAN.Add(tk);

            await _context.SaveChangesAsync();

            return Ok(new { message = "Đăng ký thành công", maNV = newMaNV });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.TenDangNhap) || string.IsNullOrWhiteSpace(dto.MatKhau))
                return BadRequest("Tên đăng nhập và mật khẩu không được để trống.");

            var user = await _context.TAIKHOAN.Include(t => t.NhanVien)
                .FirstOrDefaultAsync(t => t.TenDangNhap == dto.TenDangNhap);

            if (user == null)
                return Unauthorized("Sai tài khoản hoặc mật khẩu.");

            bool matched = false;
            try
            {
                matched = BCrypt.Net.BCrypt.Verify(dto.MatKhau, user.MatKhau);
            }
            catch { matched = false; }

            // Trường hợp mật khẩu chưa hash
            if (!matched && dto.MatKhau == user.MatKhau)
            {
                matched = true;
                user.MatKhau = BCrypt.Net.BCrypt.HashPassword(dto.MatKhau);
                _context.TAIKHOAN.Update(user);
                await _context.SaveChangesAsync();
            }

            if (!matched) return Unauthorized("Sai tài khoản hoặc mật khẩu.");

            var token = GenerateJwtToken(user);

            return Ok(new
            {
                message = "Đăng nhập thành công",
                token,
                username = user.TenDangNhap,
                role = user.Quyen,
                maNV = user.MaNV
            });
        }

        #endregion

        #region ===================== User APIs (Authenticated) =====================

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var username = User.Identity?.Name;
            if (username == null) return Unauthorized();

            var user = await _context.TAIKHOAN.Include(t => t.NhanVien)
                .FirstOrDefaultAsync(t => t.TenDangNhap == username);

            if (user == null) return NotFound();

            return Ok(new
            {
                tenDangNhap = user.TenDangNhap,
                quyen = user.Quyen,
                maNV = user.MaNV,
                hoTen = user.NhanVien?.HoTen,
                chucVu = user.NhanVien?.ChucVu,
                sdt = user.NhanVien?.SDT
            });
        }

        [Authorize]
        [HttpPut("update-profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var username = User.Identity?.Name;
            if (username == null) return Unauthorized();

            var user = await _context.TAIKHOAN.Include(t => t.NhanVien)
                .FirstOrDefaultAsync(t => t.TenDangNhap == username);

            if (user == null) return NotFound();

            // Đổi SDT
            if (!string.IsNullOrWhiteSpace(dto.SDT))
            {
                user.NhanVien.SDT = dto.SDT;
            }

            // Đổi mật khẩu
            if (!string.IsNullOrWhiteSpace(dto.NewPassword))
            {
                if (string.IsNullOrWhiteSpace(dto.CurrentPassword))
                    return BadRequest("Cần cung cấp mật khẩu hiện tại để đổi mật khẩu.");

                if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.MatKhau))
                    return BadRequest("Mật khẩu hiện tại không đúng.");

                user.MatKhau = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            }

            _context.TAIKHOAN.Update(user);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Cập nhật thông tin thành công",
                username = user.TenDangNhap,
                sdt = user.NhanVien.SDT
            });
        }

        #endregion

        #region ===================== Admin APIs =====================

        [Authorize(Roles = "Admin")]
        [HttpPut("admin-update/{maNV}")]
        public async Task<IActionResult> AdminUpdate(string maNV, [FromBody] AdminUpdateUserDto dto)
        {
            if (string.IsNullOrWhiteSpace(maNV))
                return BadRequest("MaNV không được để trống.");

            // Tìm nhân viên theo MaNV
            var user = await _context.NHANVIEN
                .Include(e => e.TaiKhoan)
                .FirstOrDefaultAsync(e => e.MaNV == maNV);

            if (user == null || user.TaiKhoan == null)
                return NotFound("Nhân viên không tồn tại.");

            // Cập nhật thông tin tài khoản
            if (!string.IsNullOrWhiteSpace(dto.MatKhau))
                user.TaiKhoan.MatKhau = BCrypt.Net.BCrypt.HashPassword(dto.MatKhau);
            if (!string.IsNullOrWhiteSpace(dto.Quyen))
                user.TaiKhoan.Quyen = dto.Quyen;

            // Cập nhật thông tin nhân viên
            if (!string.IsNullOrWhiteSpace(dto.HoTen))
                user.HoTen = dto.HoTen;
            if (!string.IsNullOrWhiteSpace(dto.ChucVu))
                user.ChucVu = dto.ChucVu;
            if (!string.IsNullOrWhiteSpace(dto.SDT))
                user.SDT = dto.SDT;

            _context.NHANVIEN.Update(user);
            _context.TAIKHOAN.Update(user.TaiKhoan);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cập nhật thành công" });
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{maNV}")]
        public async Task<IActionResult> DeleteUser(string maNV)
        {
            var employee = await _context.NHANVIEN
                .Include(e => e.TaiKhoan)
                .Include(e => e.HoaDons)
                .Include(e => e.PhieuNhapKhos)
                .FirstOrDefaultAsync(e => e.MaNV == maNV);

            if (employee == null)
                return NotFound(new { success = false, message = "Nhân viên không tồn tại" });

            _context.NHANVIEN.Remove(employee);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Xóa user thành công" });
        }

        #endregion

        #region ===================== Helper Methods =====================

        private async Task<string> GenerateNewMaNV()
        {
            var allMaNVs = await _context.NHANVIEN
                .Where(nv => nv.MaNV.StartsWith("NV"))
                .Select(nv => nv.MaNV)
                .ToListAsync();

            int lastNumber = allMaNVs
                .Where(maNV => maNV.Length == 5 && int.TryParse(maNV.Substring(2), out _))
                .Select(maNV => int.Parse(maNV.Substring(2)))
                .DefaultIfEmpty(0)
                .Max();

            return $"NV{(lastNumber + 1):D3}";
        }

        private string GenerateJwtToken(TaiKhoan user)
        {
            var jwt = _config.GetSection("Jwt");
            var key = jwt.GetValue<string>("Key");
            var issuer = jwt.GetValue<string>("Issuer");
            var audience = jwt.GetValue<string>("Audience");
            var expireHours = jwt.GetValue<int>("ExpireHours");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.TenDangNhap),
                new Claim(ClaimTypes.Role, user.Quyen ?? "NhanVien"),
                new Claim(ClaimTypes.NameIdentifier, user.MaNV ?? string.Empty)
            };

            var keyBytes = Encoding.UTF8.GetBytes(key);
            var creds = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(expireHours > 0 ? expireHours : 6),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        #endregion
    }
}