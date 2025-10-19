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

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.TenDangNhap) || string.IsNullOrWhiteSpace(dto.MatKhau))
                return BadRequest("Tên đăng nhập và mật khẩu không được để trống.");

            // Kiểm tra TenDangNhap tồn tại
            if (await _context.TAIKHOAN.AnyAsync(t => t.TenDangNhap == dto.TenDangNhap))
                return Conflict("Tên đăng nhập đã tồn tại.");

            // Nếu MaNV chưa có trong NHANVIEN thì tạo mới (theo file SQL, MaNV là PK)
            var nv = await _context.NHANVIEN.FindAsync(dto.MaNV);
            if (nv == null)
            {
                nv = new NhanVien
                {
                    MaNV = dto.MaNV,
                    HoTen = dto.HoTen,
                    ChucVu = dto.ChucVu,
                    SDT = dto.SDT,
                    NgayVaoLam = DateTime.Now,
                    TrangThai = "Đang làm"
                };
                _context.NHANVIEN.Add(nv);
            }

            // Hash mật khẩu
            var hashed = BCrypt.Net.BCrypt.HashPassword(dto.MatKhau);

            var tk = new TaiKhoan
            {
                TenDangNhap = dto.TenDangNhap,
                MatKhau = hashed,
                MaNV = dto.MaNV,
                Quyen = string.IsNullOrWhiteSpace(dto.Quyen) ? "NhanVien" : dto.Quyen
            };

            _context.TAIKHOAN.Add(tk);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đăng ký thành công" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.TenDangNhap) || string.IsNullOrWhiteSpace(dto.MatKhau))
                return BadRequest("Tên đăng nhập và mật khẩu không được để trống.");

            var user = await _context.TAIKHOAN.FirstOrDefaultAsync(t => t.TenDangNhap == dto.TenDangNhap);
            if (user == null) return Unauthorized("Sai tài khoản hoặc mật khẩu.");

            bool matched = false;

            try
            {
                // Thử so sánh hash
                matched = BCrypt.Net.BCrypt.Verify(dto.MatKhau, user.MatKhau);
            }
            catch
            {
                matched = false;
            }

            // Nếu DB đang lưu password thô (dữ liệu mẫu), so sánh trực tiếp
            if (!matched && dto.MatKhau == user.MatKhau)
            {
                matched = true;
                // Hãy hash và cập nhật DB để an toàn hơn
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

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var username = User.Identity?.Name;
            if (username == null) return Unauthorized();

            var user = await _context.TAIKHOAN.FirstOrDefaultAsync(t => t.TenDangNhap == username);
            if (user == null) return NotFound();

            return Ok(new
            {
                tenDangNhap = user.TenDangNhap,
                quyen = user.Quyen,
                maNV = user.MaNV
            });
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
    }
}
