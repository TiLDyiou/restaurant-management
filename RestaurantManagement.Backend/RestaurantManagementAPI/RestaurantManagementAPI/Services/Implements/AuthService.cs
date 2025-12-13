using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RestaurantManagementAPI.Data;
using RestaurantManagementAPI.DTOs;
using RestaurantManagementAPI.Models.Entities;
using RestaurantManagementAPI.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RestaurantManagementAPI.Services.Implements
{
    public class AuthService : IAuthService
    {
        private readonly QLNHDbContext _context;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;

        public AuthService(QLNHDbContext context, IConfiguration config, IEmailService emailService)
        {
            _context = context;
            _config = config;
            _emailService = emailService;
        }

        public async Task<(bool Success, string Message, string? MaNV)> RegisterAsync(RegisterDto dto)
        {
            if (await _context.TAIKHOAN.AnyAsync(t => t.TenDangNhap == dto.TenDangNhap))
                return (false, "Tên đăng nhập đã tồn tại.", null);

            if (!string.IsNullOrWhiteSpace(dto.Email) && await _context.TAIKHOAN.AnyAsync(t => t.Email == dto.Email))
                return (false, "Email này đã được sử dụng bởi tài khoản khác.", null);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
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
                    IsActive = false,
                    Email = dto.Email,
                    IsVerified = false
                };
                _context.TAIKHOAN.Add(tk);

                await _context.SaveChangesAsync();
                if (!string.IsNullOrWhiteSpace(dto.Email))
                {
                    bool emailSent = await SendOtpInternal(tk, "OTP Xác Thực Email");
                    if (!emailSent)
                    {
                        await transaction.RollbackAsync();
                        return (false, "Tạo tài khoản thất bại do lỗi hệ thống gửi email. Vui lòng thử lại sau.", null);
                    }
                }

                await transaction.CommitAsync();
                return (true, "Đăng ký thành công, vui lòng kiểm tra email.", newMaNV);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, "Lỗi hệ thống khi đăng ký: " + ex.Message, null);
            }
        }

        public async Task<(bool Success, string Message, object? Data)> LoginAsync(LoginDto dto)
        {
            var user = await _context.TAIKHOAN.Include(t => t.NhanVien)
                                     .FirstOrDefaultAsync(t => t.TenDangNhap == dto.TenDangNhap);
            bool matched = false;
            if (user != null)
            {
                try { matched = BCrypt.Net.BCrypt.Verify(dto.MatKhau, user.MatKhau); } catch { matched = false; }
            }
            else
            {
                BCrypt.Net.BCrypt.Verify("dummy", "$2a$11$Ou9z/k/y...dummyhash...");
            }

            if (user == null || !matched)
                return (false, "Sai tài khoản hoặc mật khẩu.", null);

            if (!user.IsVerified) return (false, "Tài khoản chưa xác thực email.", null);
            if (!user.IsActive) return (false, "Tài khoản đã bị vô hiệu hóa.", null);

            var token = GenerateJwtToken(user);
            return (true, "Đăng nhập thành công", new { token, username = user.TenDangNhap, role = user.Quyen, maNV = user.MaNV });
        }

        public async Task<(bool Success, string Message)> SendRegisterOtpAsync(string email)
        {
            var user = await _context.TAIKHOAN.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return (false, "Email không tồn tại.");

            if (user.IsVerified) return (false, "Tài khoản này đã được xác thực rồi.");

            bool sent = await SendOtpInternal(user, "OTP Xác Thực Email");
            return sent ? (true, "OTP đã được gửi.") : (false, "Gửi email thất bại.");
        }

        public async Task<(bool Success, string Message)> VerifyRegisterOtpAsync(string email, string otp)
        {
            var user = await _context.TAIKHOAN.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return (false, "Email không tồn tại.");

            if (user.OTP?.Trim() != otp.Trim() || user.OTPExpireTime < DateTime.UtcNow)
                return (false, "OTP sai hoặc hết hạn.");

            user.IsVerified = true;
            user.IsActive = true;
            user.OTP = null;
            user.OTPExpireTime = null;
            await _context.SaveChangesAsync();
            return (true, "Xác thực thành công.");
        }

        public async Task<(bool Success, string Message)> ForgotPasswordAsync(string email)
        {
            var user = await _context.TAIKHOAN.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return (false, "Email không tồn tại.");

            bool sent = await SendOtpInternal(user, "OTP Đổi Mật Khẩu");
            return sent ? (true, "OTP đã được gửi.") : (false, "Gửi email thất bại.");
        }

        public async Task<(bool Success, string Message)> VerifyForgotOtpAsync(string email, string otp)
        {
            var user = await _context.TAIKHOAN.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return (false, "Email không tồn tại.");
            if (user.OTP?.Trim() != otp.Trim() || user.OTPExpireTime < DateTime.UtcNow) return (false, "OTP sai hoặc hết hạn.");
            return (true, "OTP hợp lệ.");
        }

        public async Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordDto dto)
        {
            var user = await _context.TAIKHOAN.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null) return (false, "Email không tồn tại.");
            if (user.OTP?.Trim() != dto.OTP.Trim() || user.OTPExpireTime < DateTime.UtcNow) return (false, "OTP sai hoặc hết hạn.");

            user.MatKhau = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.OTP = null;
            user.OTPExpireTime = null;
            await _context.SaveChangesAsync();
            return (true, "Đổi mật khẩu thành công.");
        }

        private async Task<bool> SendOtpInternal(TaiKhoan user, string subject)
        {
            var otp = new Random().Next(100000, 999999).ToString();
            user.OTP = otp;
            user.OTPExpireTime = DateTime.UtcNow.AddMinutes(5);
            await _context.SaveChangesAsync();

            try
            {
                await _emailService.SendEmailAsync(user.Email!, subject, $"Mã OTP: {otp}");
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<string> GenerateNewMaNV()
        {
            var lastNv = await _context.NHANVIEN
                .Where(nv => nv.MaNV.StartsWith("NV"))
                .OrderByDescending(nv => nv.MaNV.Length)
                .ThenByDescending(nv => nv.MaNV)
                .Select(nv => nv.MaNV)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastNv != null && lastNv.Length > 2 && int.TryParse(lastNv.Substring(2), out int lastNum))
            {
                nextNumber = lastNum + 1;
            }

            return $"NV{nextNumber:D3}";
        }

        private string GenerateJwtToken(TaiKhoan user)
        {
            var jwt = _config.GetSection("Jwt");
            var keyStr = jwt["Key"];
            if (string.IsNullOrEmpty(keyStr)) throw new ArgumentNullException("Jwt:Key is missing in appsettings.json");

            var key = Encoding.UTF8.GetBytes(keyStr);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.TenDangNhap),
                new Claim(ClaimTypes.Role, user.Quyen ?? "NhanVien"),
                new Claim(ClaimTypes.NameIdentifier, user.MaNV ?? "")
            };
            var token = new JwtSecurityToken(
                jwt["Issuer"], jwt["Audience"], claims,
                expires: DateTime.UtcNow.AddHours(jwt.GetValue<int>("ExpireHours")),
                signingCredentials: new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256));
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}