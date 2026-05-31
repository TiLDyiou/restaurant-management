using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RestaurantManagementAPI.Common.Wrappers;
using RestaurantManagementAPI.Data;
using RestaurantManagementAPI.DTOs;
using RestaurantManagementAPI.Interfaces;
using RestaurantManagementAPI.Models.Entities;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace RestaurantManagementAPI.Services
{
    public class AuthService : IAuthService
    {
        private readonly QLNHDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IJwtTokenGenerator _jwtGenerator;
        private readonly IConfiguration _config;

        private static readonly string DummyHash =
            BCrypt.Net.BCrypt.HashPassword("dummy-timing-defense-value");

        public AuthService(
            QLNHDbContext context,
            IEmailService emailService,
            IJwtTokenGenerator jwtGenerator,
            IConfiguration config)
        {
            _context = context;
            _emailService = emailService;
            _jwtGenerator = jwtGenerator;
            _config = config;
        }

        public async Task<ServiceResult<string>> RegisterAsync(RegisterDto dto)
        {
            if (!string.IsNullOrWhiteSpace(dto.Email) && !IsValidEmail(dto.Email))
                return ServiceResult<string>.Fail("Email không đúng định dạng.");

            if (!string.IsNullOrWhiteSpace(dto.SDT) && !IsValidPhoneNumber(dto.SDT))
                return ServiceResult<string>.Fail("Số điện thoại không hợp lệ (phải có 10-11 số, bắt đầu bằng 0).");

            if (!IsValidPassword(dto.MatKhau))
                return ServiceResult<string>.Fail("Mật khẩu phải có ít nhất 8 ký tự, bao gồm chữ và số.");

            if (await _context.TAIKHOAN.AnyAsync(t => t.TenDangNhap == dto.TenDangNhap))
                return ServiceResult<string>.Fail("Tên đăng nhập đã tồn tại.");

            if (!string.IsNullOrWhiteSpace(dto.Email) &&
                await _context.TAIKHOAN.AnyAsync(t => t.Email == dto.Email))
                return ServiceResult<string>.Fail("Email này đã được sử dụng.");

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
                    Quyen = "NhanVien", // Đăng ký công khai luôn là NhanVien. Admin tạo từ trang quản lý user.
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
                        return ServiceResult<string>.Fail("Lỗi gửi email xác thực.");
                    }
                }
                await transaction.CommitAsync();
                return ServiceResult<string>.Ok(newMaNV, "Đăng ký thành công, vui lòng kiểm tra email.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ServiceResult<string>.Fail("Lỗi hệ thống: " + ex.Message);
            }
        }

        public async Task<ServiceResult<object>> LoginAsync(LoginDto dto, string? ip)
        {
            var user = await _context.TAIKHOAN
                .Include(t => t.NhanVien)
                .FirstOrDefaultAsync(t => t.TenDangNhap == dto.TenDangNhap);

            bool matched = false;
            if (user != null)
            {
                try { matched = BCrypt.Net.BCrypt.Verify(dto.MatKhau, user.MatKhau); }
                catch { matched = false; }
            }
            else
            {
                // Verify với hash hợp lệ để giữ thời gian xử lý đồng đều, chống timing attack.
                BCrypt.Net.BCrypt.Verify(dto.MatKhau, DummyHash);
            }

            if (user == null || !matched)
                return ServiceResult<object>.Fail("Sai tài khoản hoặc mật khẩu.");
            if (!user.IsVerified)
                return ServiceResult<object>.Fail("Tài khoản chưa xác thực email.");
            if (!user.IsActive)
                return ServiceResult<object>.Fail("Tài khoản đã bị vô hiệu hóa.");

            user.Online = true;

            var accessToken = _jwtGenerator.GenerateAccessToken(user);
            var refreshToken = CreateRefreshTokenAsync(user.MaNV, ip);

            await _context.SaveChangesAsync();

            return ServiceResult<object>.Ok(new
            {
                accessToken,
                refreshToken = refreshToken.Token,
                refreshTokenExpiresAt = refreshToken.ExpiresAt,
                username = user.TenDangNhap,
                role = user.Quyen,
                maNV = user.MaNV
            }, "Đăng nhập thành công");
        }

        public async Task<ServiceResult<object>> RefreshTokenAsync(string refreshToken, string? ip)
        {
            var stored = await _context.REFRESHTOKEN
                .Include(r => r.NhanVien)
                    .ThenInclude(nv => nv!.TaiKhoan)
                .FirstOrDefaultAsync(r => r.Token == refreshToken);

            if (stored == null || !stored.IsActive)
                return ServiceResult<object>.Fail("Refresh token không hợp lệ hoặc đã hết hạn.");

            var taiKhoan = stored.NhanVien?.TaiKhoan;
            if (taiKhoan == null || !taiKhoan.IsActive)
                return ServiceResult<object>.Fail("Tài khoản không khả dụng.");

            // Token rotation: thu hồi refresh token cũ, cấp mới.
            stored.RevokedAt = DateTime.UtcNow;

            var newAccessToken = _jwtGenerator.GenerateAccessToken(taiKhoan);
            var newRefreshToken = CreateRefreshTokenAsync(stored.MaNV, ip);

            await _context.SaveChangesAsync();

            return ServiceResult<object>.Ok(new
            {
                accessToken = newAccessToken,
                refreshToken = newRefreshToken.Token,
                refreshTokenExpiresAt = newRefreshToken.ExpiresAt
            }, "Cấp token mới thành công.");
        }

        public async Task<ServiceResult> RevokeRefreshTokenAsync(string refreshToken)
        {
            var stored = await _context.REFRESHTOKEN
                .FirstOrDefaultAsync(r => r.Token == refreshToken);

            if (stored == null || !stored.IsActive)
                return ServiceResult.Fail("Refresh token không hợp lệ hoặc đã thu hồi.");

            stored.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return ServiceResult.Ok("Đã thu hồi token.");
        }

        public async Task<ServiceResult> LogoutAsync(string maNV)
        {
            var user = await _context.TAIKHOAN.FirstOrDefaultAsync(u => u.MaNV == maNV);
            if (user == null)
                return ServiceResult.Fail("Không tìm thấy người dùng.");

            user.Online = false;

            // Thu hồi tất cả refresh token còn hiệu lực của user này.
            var activeTokens = await _context.REFRESHTOKEN
                .Where(r => r.MaNV == maNV && r.RevokedAt == null && r.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();
            foreach (var t in activeTokens)
                t.RevokedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return ServiceResult.Ok("Đăng xuất thành công.");
        }

        public async Task<ServiceResult> SendRegisterOtpAsync(string email)
        {
            var user = await _context.TAIKHOAN.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return ServiceResult.Fail("Email không tồn tại.");
            if (user.IsVerified)
                return ServiceResult.Fail("Tài khoản này đã xác thực rồi.");

            bool sent = await SendOtpInternal(user, "OTP Xác Thực Email");
            return sent ? ServiceResult.Ok("OTP đã được gửi.") : ServiceResult.Fail("Gửi email thất bại.");
        }

        public async Task<ServiceResult> VerifyRegisterOtpAsync(string email, string otp)
        {
            var user = await _context.TAIKHOAN.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return ServiceResult.Fail("Email không tồn tại.");

            if (user.OTP?.Trim() != otp.Trim() || user.OTPExpireTime < DateTime.UtcNow)
                return ServiceResult.Fail("OTP sai hoặc hết hạn.");

            user.IsVerified = true;
            user.IsActive = true;
            user.OTP = null;
            user.OTPExpireTime = null;
            await _context.SaveChangesAsync();
            return ServiceResult.Ok("Xác thực thành công.");
        }

        public async Task<ServiceResult> ForgotPasswordAsync(string email)
        {
            var user = await _context.TAIKHOAN.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return ServiceResult.Fail("Email không tồn tại.");

            bool sent = await SendOtpInternal(user, "OTP Đổi Mật Khẩu");
            return sent ? ServiceResult.Ok("OTP đã được gửi.") : ServiceResult.Fail("Gửi email thất bại.");
        }

        public async Task<ServiceResult> VerifyForgotOtpAsync(string email, string otp)
        {
            var user = await _context.TAIKHOAN.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return ServiceResult.Fail("Email không tồn tại.");

            if (user.OTP?.Trim() != otp.Trim() || user.OTPExpireTime < DateTime.UtcNow)
                return ServiceResult.Fail("OTP sai hoặc hết hạn.");
            return ServiceResult.Ok("OTP hợp lệ.");
        }

        public async Task<ServiceResult> ResetPasswordAsync(ResetPasswordDto dto)
        {
            var user = await _context.TAIKHOAN.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
                return ServiceResult.Fail("Email không tồn tại.");

            if (user.OTP?.Trim() != dto.OTP.Trim() || user.OTPExpireTime < DateTime.UtcNow)
                return ServiceResult.Fail("OTP sai hoặc hết hạn.");

            if (!IsValidPassword(dto.NewPassword))
                return ServiceResult.Fail("Mật khẩu phải có ít nhất 8 ký tự, bao gồm chữ và số.");

            user.MatKhau = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.OTP = null;
            user.OTPExpireTime = null;

            // Thu hồi tất cả refresh token sau khi đổi mật khẩu — buộc đăng nhập lại trên mọi thiết bị.
            var activeTokens = await _context.REFRESHTOKEN
                .Where(r => r.MaNV == user.MaNV && r.RevokedAt == null)
                .ToListAsync();
            foreach (var t in activeTokens)
                t.RevokedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return ServiceResult.Ok("Đổi mật khẩu thành công.");
        }

        private RefreshToken CreateRefreshTokenAsync(string maNV, string? ip)
        {
            var jwtSettings = _config.GetSection("Jwt");
            var expireDays = jwtSettings.GetValue<int>("RefreshTokenExpireDays", 7);

            var entity = new RefreshToken
            {
                Token = _jwtGenerator.GenerateRefreshToken(),
                MaNV = maNV,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(expireDays),
                CreatedByIp = ip
            };
            _context.REFRESHTOKEN.Add(entity);
            return entity;
        }

        private async Task<bool> SendOtpInternal(TaiKhoan user, string subject)
        {
            var otp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
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
            const int maxRetries = 3;
            for (int i = 0; i < maxRetries; i++)
            {
                var lastNv = await _context.NHANVIEN
                    .Where(nv => nv.MaNV.StartsWith("NV"))
                    .OrderByDescending(nv => nv.MaNV.Length)
                    .ThenByDescending(nv => nv.MaNV)
                    .Select(nv => nv.MaNV)
                    .FirstOrDefaultAsync();

                int nextNumber = 1;
                if (lastNv != null && lastNv.Length > 2 &&
                    int.TryParse(lastNv.Substring(2), out int lastNum))
                {
                    nextNumber = lastNum + 1;
                }

                var newMaNV = $"NV{nextNumber:D3}";

                if (!await _context.NHANVIEN.AnyAsync(nv => nv.MaNV == newMaNV))
                    return newMaNV;
            }
            throw new InvalidOperationException("Không thể tạo mã nhân viên mới.");
        }

        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            return Regex.IsMatch(email, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
        }

        private static bool IsValidPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber)) return false;
            return Regex.IsMatch(phoneNumber, @"^0\d{9,10}$");
        }

        private static bool IsValidPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8) return false;
            return password.Any(char.IsLetter) && password.Any(char.IsDigit);
        }
    }
}
