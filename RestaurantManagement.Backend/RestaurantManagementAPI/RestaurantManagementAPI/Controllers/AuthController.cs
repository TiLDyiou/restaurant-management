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

            if (await _context.TAIKHOAN.AnyAsync(t => t.TenDangNhap == dto.TenDangNhap))
                return Conflict("Tên đăng nhập đã tồn tại.");

            // Tự sinh mã nhân viên (ví dụ NV001, NV002,...)
            var lastNV = await _context.NHANVIEN
                .OrderByDescending(nv => nv.MaNV)
                .FirstOrDefaultAsync();

            string newMaNV;
            if (lastNV == null || string.IsNullOrEmpty(lastNV.MaNV))
                newMaNV = "NV001";
            else
            {
                int lastNumber = int.Parse(lastNV.MaNV.Substring(2)); // Bỏ "NV"
                newMaNV = $"NV{(lastNumber + 1).ToString("D3")}"; // D3 -> luôn có 3 chữ số
            }

            // Tạo mới nhân viên
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

            // Tạo tài khoản kèm theo
            var tk = new TaiKhoan
            {
                TenDangNhap = dto.TenDangNhap,
                MatKhau = BCrypt.Net.BCrypt.HashPassword(dto.MatKhau),
                MaNV = newMaNV, // dùng mã mới sinh
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
            if (user == null) return Unauthorized("Sai tài khoản hoặc mật khẩu.");

            bool matched = false;
            try
            {
                matched = BCrypt.Net.BCrypt.Verify(dto.MatKhau, user.MatKhau);
            }
            catch { matched = false; }

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
        [HttpPut("update")]
        public async Task<IActionResult> Update([FromBody] UpdateUserDto dto)
        {
            var username = User.Identity?.Name;
            if (username == null) return Unauthorized();

            var user = await _context.TAIKHOAN
                .Include(t => t.NhanVien)
                .FirstOrDefaultAsync(t => t.TenDangNhap == username);
            if (user == null) return NotFound();
            if (!string.IsNullOrWhiteSpace(dto.TenDangNhap))
                user.TenDangNhap = dto.TenDangNhap;

            if (!string.IsNullOrWhiteSpace(dto.SDT))
                user.NhanVien.SDT = dto.SDT;

            _context.TAIKHOAN.Update(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cập nhật thông tin cá nhân thành công" });
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("admin-update")]
        public async Task<IActionResult> AdminUpdate([FromBody] AdminUpdateUserDto dto)
        {
            var user = await _context.TAIKHOAN.Include(t => t.NhanVien)
                .FirstOrDefaultAsync(t => t.TenDangNhap == dto.TenDangNhap);
            if (user == null) return NotFound();
            if (!string.IsNullOrWhiteSpace(dto.MatKhau))
                user.MatKhau = BCrypt.Net.BCrypt.HashPassword(dto.MatKhau);
            if (!string.IsNullOrWhiteSpace(dto.Quyen))
                user.Quyen = dto.Quyen;
            if (!string.IsNullOrWhiteSpace(dto.HoTen))
                user.NhanVien.HoTen = dto.HoTen;
            if (!string.IsNullOrWhiteSpace(dto.ChucVu))
                user.NhanVien.ChucVu = dto.ChucVu;
            if (!string.IsNullOrWhiteSpace(dto.SDT))
                user.NhanVien.SDT = dto.SDT;

            _context.TAIKHOAN.Update(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cập nhật thành công" });
        }

        [HttpPost("DeleteUser")]
        public async Task<IActionResult> DeleteUser([FromBody] DeleteUserDto request)
        {
            var employee = await _context.NHANVIEN
                .Include(e => e.TaiKhoan)
                .Include(e => e.HoaDons)
                .Include(e => e.PhieuNhapKhos)
                .FirstOrDefaultAsync(e => e.MaNV == request.MaNV && e.HoTen == request.HoTen);

            if (employee == null)
                return NotFound(new { success = false, message = "Nhân viên không tồn tại hoặc họ tên không khớp" });

            _context.NHANVIEN.Remove(employee);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Xóa user thành công" });
        }


        [Authorize]
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var username = User.Identity?.Name;
            if (username == null) return Unauthorized();

            var user = await _context.TAIKHOAN.FirstOrDefaultAsync(t => t.TenDangNhap == username);
            if (user == null) return NotFound();

            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.MatKhau))
                return BadRequest("Mật khẩu hiện tại không đúng.");

            user.MatKhau = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            _context.TAIKHOAN.Update(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đổi mật khẩu thành công" });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var user = await _context.TAIKHOAN.FirstOrDefaultAsync(t => t.TenDangNhap == dto.TenDangNhap);
            if (user == null) return NotFound();

            user.MatKhau = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            _context.TAIKHOAN.Update(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Reset mật khẩu thành công" });
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
