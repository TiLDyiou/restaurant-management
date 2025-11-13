using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RestaurentManagementAPI.Data;
using RestaurentManagementAPI.DTOs;
using RestaurentManagementAPI.Models.Entities;
using RestaurentManagementAPI.Services;
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
        private readonly EmailService _emailService;

        public AuthController(QLNHDbContext context, IConfiguration config, EmailService emailService)
        {
            _context = context;
            _config = config;
            _emailService = emailService;
        }

        #region ===================== Public APIs =====================

        [HttpPost("register")]
        
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.TenDangNhap) || string.IsNullOrWhiteSpace(dto.MatKhau))
                return BadRequest("Tên đăng nhập và mật khẩu không được để trống.");

            if (await _context.TAIKHOAN.AnyAsync(t => t.TenDangNhap == dto.TenDangNhap))
                return Conflict("Tên đăng nhập đã tồn tại.");

            var newMaNV = await GenerateNewMaNV();

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

            var tk = new TaiKhoan
            {
                TenDangNhap = dto.TenDangNhap,
                MatKhau = BCrypt.Net.BCrypt.HashPassword(dto.MatKhau),
                MaNV = newMaNV,
                Quyen = string.IsNullOrWhiteSpace(dto.Quyen) ? "NhanVien" : dto.Quyen,
                IsActive = true,
                Email = dto.Email,
                IsVerified = true
            };
            _context.TAIKHOAN.Add(tk);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                try
                {
                    var otp = new Random().Next(100000, 999999).ToString();
                    tk.OTP = otp;
                    tk.OTPExpireTime = DateTime.UtcNow.AddMinutes(5);
                    await _context.SaveChangesAsync();

                    await _emailService.SendEmailAsync(dto.Email, "OTP Xác Thực Email", $"Mã OTP của bạn là: {otp}");
                }
                catch
                {
                    return Ok(new { message = "Tài khoản đã tạo, nhưng gửi email OTP thất bại.", maNV = newMaNV });
                }
            }

            return Ok(new { message = "Tài khoản đã tạo, vui lòng kiểm tra email để xác thực.", maNV = newMaNV });
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

            if (!user.IsActive)
                return Unauthorized("Tài khoản của bạn đã bị vô hiệu hóa.");

            bool matched = false;
            try { matched = BCrypt.Net.BCrypt.Verify(dto.MatKhau, user.MatKhau); }
            catch { matched = false; }

            if (!matched && dto.MatKhau == user.MatKhau)
            {
                matched = true;
                user.MatKhau = BCrypt.Net.BCrypt.HashPassword(dto.MatKhau);
                _context.TAIKHOAN.Update(user);
                await _context.SaveChangesAsync();
            }

            if (!matched) return Unauthorized("Sai tài khoản hoặc mật khẩu.");
            if (!user.IsVerified) return Unauthorized("Tài khoản chưa xác thực email.");

            var token = GenerateJwtToken(user);

            return Ok(new
            {
                message = "Đăng nhập thành công",
                token,
                username = user.TenDangNhap,
                role = user.Quyen,
                maNV = user.MaNV,
                chucVu = user.NhanVien?.ChucVu
            });
        }

        [HttpPost("send-register-otp")]
        public async Task<IActionResult> SendRegisterOtp([FromBody] EmailDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
                return BadRequest("Email không được để trống.");

            var user = await _context.TAIKHOAN.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null) return NotFound("Email không tồn tại.");

            var otp = new Random().Next(100000, 999999).ToString();
            user.OTP = otp;
            user.OTPExpireTime = DateTime.UtcNow.AddMinutes(5);
            await _context.SaveChangesAsync();

            await _emailService.SendEmailAsync(user.Email!, "OTP Xác Thực Email", $"Mã OTP: {otp}");
            return Ok(new { message = "OTP đã được gửi đến email." });
        }

        [HttpPost("verify-register-otp")]
        public async Task<IActionResult> VerifyRegisterOtp([FromBody] VerifyOtpDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.OTP))
                return BadRequest("Email và OTP không được để trống.");

            var user = await _context.TAIKHOAN.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null) return NotFound("Email không tồn tại.");

            if (user.OTP?.Trim() != dto.OTP.Trim() || user.OTPExpireTime < DateTime.UtcNow)
                return BadRequest("OTP không hợp lệ hoặc đã hết hạn.");

            user.IsVerified = true;
            user.IsActive = true;
            user.OTP = null;
            user.OTPExpireTime = null;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Xác thực đăng ký thành công." });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] EmailDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email)) return BadRequest("Email không được để trống.");

            var user = await _context.TAIKHOAN.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null) return NotFound("Email không tồn tại.");

            var otp = new Random().Next(100000, 999999).ToString();
            user.OTP = otp;
            user.OTPExpireTime = DateTime.UtcNow.AddMinutes(5);
            await _context.SaveChangesAsync();

            await _emailService.SendEmailAsync(user.Email!, "OTP đổi mật khẩu", $"Mã OTP: {otp}");
            return Ok(new { message = "OTP đổi mật khẩu đã gửi đến email." });
        }

        [HttpPost("verify-forgot-otp")]
        public async Task<IActionResult> VerifyForgotOtp([FromBody] VerifyOtpDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.OTP))
                return BadRequest("Email và OTP không được để trống.");

            var user = await _context.TAIKHOAN.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null) return NotFound("Email không tồn tại.");

            if (user.OTP?.Trim() != dto.OTP.Trim() || user.OTPExpireTime < DateTime.UtcNow)
                return BadRequest("OTP không hợp lệ hoặc đã hết hạn.");

            return Ok(new { message = "OTP hợp lệ, bạn có thể đặt lại mật khẩu." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.OTP) || string.IsNullOrWhiteSpace(dto.NewPassword))
                return BadRequest("Email, OTP và mật khẩu mới không được để trống.");

            var user = await _context.TAIKHOAN.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null) return NotFound("Email không tồn tại.");

            if (user.OTP?.Trim() != dto.OTP.Trim() || user.OTPExpireTime < DateTime.UtcNow)
                return BadRequest("OTP không hợp lệ hoặc đã hết hạn.");

            user.MatKhau = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.OTP = null;
            user.OTPExpireTime = null;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đổi mật khẩu thành công." });
        }

        [Authorize]
        [HttpPost("verify-email-otp")]
        public async Task<IActionResult> VerifyEmailOtp([FromBody] VerifyOtpDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.OTP))
                return BadRequest("Email và OTP không được để trống.");

            // Lấy tài khoản theo email mới
            var user = await _context.TAIKHOAN.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null) return NotFound("Email không tồn tại.");

            // Kiểm tra OTP
            if (user.OTP?.Trim() != dto.OTP.Trim() || user.OTPExpireTime < DateTime.UtcNow)
                return BadRequest("OTP không hợp lệ hoặc đã hết hạn.");

            // Xác thực email thành công
            user.IsVerified = true;
            user.OTP = null;
            user.OTPExpireTime = null;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Xác thực email thành công." });
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
                sdt = user.NhanVien?.SDT,
                email = user.Email,
                isVerified = user.IsVerified
            });
        }

        /*[Authorize]
        [HttpPut("update-profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var username = User.Identity?.Name;
            if (username == null) return Unauthorized();

            var user = await _context.TAIKHOAN.Include(t => t.NhanVien)
                .FirstOrDefaultAsync(t => t.TenDangNhap == username);

            if (user == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(dto.SDT))
                user.NhanVien.SDT = dto.SDT;

            if (!string.IsNullOrWhiteSpace(dto.NewPassword))
            {
                if (string.IsNullOrWhiteSpace(dto.CurrentPassword))
                    return BadRequest("Cần cung cấp mật khẩu hiện tại để đổi mật khẩu.");

                if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.MatKhau))
                    return BadRequest("Mật khẩu hiện tại không đúng.");

                user.MatKhau = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            }

            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                user.Email = dto.Email;
                user.IsVerified = false;
                var otp = new Random().Next(100000, 999999).ToString();
                user.OTP = otp;
                user.OTPExpireTime = DateTime.UtcNow.AddMinutes(5);
                try
                {
                    await _emailService.SendEmailAsync(dto.Email, "OTP Xác Thực Email", $"Mã OTP của bạn là: {otp}");
                }
                catch
                {
                    return BadRequest("Gửi email OTP thất bại.");
                }
            }

            _context.TAIKHOAN.Update(user);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Cập nhật thông tin thành công",
                username = user.TenDangNhap,
                sdt = user.NhanVien.SDT,
                email = user.Email,
                isVerified = user.IsVerified
            });
        }*/

        #endregion

        #region ===================== Admin APIs =====================

        [Authorize(Roles = "Admin")]
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.NHANVIEN
                .Include(nv => nv.TaiKhoan)
                .Select(nv => new
                {
                    maNV = nv.MaNV,
                    hoTen = nv.HoTen,
                    chucVu = nv.ChucVu,
                    sdt = nv.SDT,
                    quyen = nv.TaiKhoan.Quyen,
                    tenDangNhap = nv.TaiKhoan.TenDangNhap,
                    trangThai = nv.TrangThai,
                    hoatDong = nv.TaiKhoan.IsActive,
                    email = nv.TaiKhoan.Email,
                    isVerified = nv.TaiKhoan.IsVerified
                })
                .ToListAsync();

            return Ok(users);
        }

        /*[Authorize(Roles = "Admin")]
        [HttpPut("admin-update/{maNV}")]
        public async Task<IActionResult> AdminUpdateUser(string maNV, [FromBody] AdminUpdateUserDto dto)
        {
            var employee = await _context.NHANVIEN
                .Include(e => e.TaiKhoan)
                .FirstOrDefaultAsync(e => e.MaNV == maNV);

            if (employee == null) return NotFound("Nhân viên không tồn tại.");

            if (!string.IsNullOrWhiteSpace(dto.HoTen)) employee.HoTen = dto.HoTen;
            if (!string.IsNullOrWhiteSpace(dto.ChucVu)) employee.ChucVu = dto.ChucVu;
            if (!string.IsNullOrWhiteSpace(dto.SDT)) employee.SDT = dto.SDT;
            if (!string.IsNullOrWhiteSpace(dto.TrangThai))
            {
                employee.TrangThai = dto.TrangThai;
                if (employee.TaiKhoan != null)
                    employee.TaiKhoan.IsActive = dto.TrangThai == "Đang làm";
            }

            if (employee.TaiKhoan != null)
            {
                if (!string.IsNullOrWhiteSpace(dto.Quyen)) employee.TaiKhoan.Quyen = dto.Quyen;
                if (!string.IsNullOrWhiteSpace(dto.MatKhau)) employee.TaiKhoan.MatKhau = BCrypt.Net.BCrypt.HashPassword(dto.MatKhau);
                if (!string.IsNullOrWhiteSpace(dto.Email)) employee.TaiKhoan.Email = dto.Email;
            }

            _context.NHANVIEN.Update(employee);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                maNV = employee.MaNV,
                hoTen = employee.HoTen,
                chucVu = employee.ChucVu,
                sdt = employee.SDT,
                trangThai = employee.TrangThai,
                quyen = employee.TaiKhoan?.Quyen
            });
        }*/

        [Authorize]
        [HttpPost("resend-email-otp")]
        public async Task<IActionResult> ResendEmailOtp([FromBody] EmailDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
                return BadRequest("Email không được để trống.");

            var user = await _context.TAIKHOAN.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null) return NotFound("Email không tồn tại.");

            if (user.IsVerified)
                return BadRequest("Email đã được xác thực.");

            // Tạo OTP mới
            var otp = new Random().Next(100000, 999999).ToString();
            user.OTP = otp;
            user.OTPExpireTime = DateTime.UtcNow.AddMinutes(5);

            try
            {
                await _emailService.SendEmailAsync(user.Email, "OTP Xác Thực Email", $"Mã OTP của bạn là: {otp}");
                await _context.SaveChangesAsync();
                return Ok(new { message = "OTP đã được gửi lại đến email." });
            }
            catch
            {
                return BadRequest("Gửi email OTP thất bại.");
            }
        }

        [Authorize]
        [HttpPut("update-user/{maNV?}")]
        public async Task<IActionResult> UpdateUser(string? maNV, [FromBody] UpdateUserDto dto)
        {
            TaiKhoan user;
            bool isAdmin = User.IsInRole("Admin");

            if (isAdmin && !string.IsNullOrEmpty(maNV))
            {
                // Admin cập nhật cho bất kỳ nhân viên nào
                user = await _context.TAIKHOAN.Include(t => t.NhanVien)
                         .FirstOrDefaultAsync(u => u.MaNV == maNV);
                if (user == null) return NotFound("Nhân viên không tồn tại.");
            }
            else
            {
                // Người dùng cập nhật thông tin cá nhân
                var username = User.Identity?.Name;
                if (username == null) return Unauthorized();
                user = await _context.TAIKHOAN.Include(t => t.NhanVien)
                         .FirstOrDefaultAsync(u => u.TenDangNhap == username);
                if (user == null) return NotFound();
            }

            // Cập nhật thông tin
            if (!string.IsNullOrWhiteSpace(dto.HoTen))
                user.NhanVien.HoTen = dto.HoTen.Trim();
            if (!string.IsNullOrWhiteSpace(dto.ChucVu) && isAdmin)
                user.NhanVien.ChucVu = dto.ChucVu.Trim();
            if (!string.IsNullOrWhiteSpace(dto.SDT))
                user.NhanVien.SDT = dto.SDT.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Quyen) && isAdmin)
                user.Quyen = dto.Quyen.Trim();
            if (!string.IsNullOrWhiteSpace(dto.MatKhau))
                user.MatKhau = BCrypt.Net.BCrypt.HashPassword(dto.MatKhau);

            // Nếu đổi email → gửi OTP và đánh dấu chưa xác thực
            if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != user.Email)
            {
                user.Email = dto.Email.Trim();
                user.IsVerified = false;
                var otp = new Random().Next(100000, 999999).ToString();
                user.OTP = otp;
                user.OTPExpireTime = DateTime.UtcNow.AddMinutes(5);

                try
                {
                    await _emailService.SendEmailAsync(dto.Email, "OTP Xác Thực Email", $"Mã OTP của bạn là: {otp}");
                }
                catch
                {
                    return BadRequest("Gửi email OTP thất bại.");
                }
            }

            _context.TAIKHOAN.Update(user);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Cập nhật thông tin thành công",
                tenDangNhap = user.TenDangNhap,
                email = user.Email,
                isVerified = user.IsVerified
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("soft-delete/{maNV}")]
        public async Task<IActionResult> ToggleStatus(string maNV)
        {
            var employee = await _context.NHANVIEN
                .Include(e => e.TaiKhoan)
                .FirstOrDefaultAsync(e => e.MaNV == maNV);

            if (employee == null) return NotFound("Nhân viên không tồn tại.");

            employee.TrangThai = employee.TrangThai == "Đang làm" ? "Đã nghỉ" : "Đang làm";
            if (employee.TaiKhoan != null)
                employee.TaiKhoan.IsActive = employee.TrangThai == "Đang làm";

            _context.NHANVIEN.Update(employee);
            await _context.SaveChangesAsync();

            return Ok(new { maNV = employee.MaNV, TrangThai = employee.TrangThai, HoatDong = employee.TaiKhoan?.IsActive });
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("hard-delete/{maNV}")]
        public async Task<IActionResult> HardDelete(string maNV)
        {
            var employee = await _context.NHANVIEN
                .Include(e => e.TaiKhoan)
                .Include(e => e.HoaDons)
                .Include(e => e.PhieuNhapKhos)
                .FirstOrDefaultAsync(e => e.MaNV == maNV);

            if (employee == null) return NotFound();

            if (employee.TaiKhoan != null) _context.TAIKHOAN.Remove(employee.TaiKhoan);
            _context.NHANVIEN.Remove(employee);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Nhân viên đã bị xóa vĩnh viễn" });
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
