using Microsoft.EntityFrameworkCore;
using RestaurantManagementAPI.Common.Wrappers;
using RestaurantManagementAPI.Data;
using RestaurantManagementAPI.DTOs;
using RestaurantManagementAPI.Interfaces;
using RestaurantManagementAPI.Models.Entities;

namespace RestaurantManagementAPI.Services
{
    public class AuthService : IAuthService
    {
        private readonly QLNHDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IJwtTokenGenerator _jwtGenerator;

        public AuthService(QLNHDbContext context, IEmailService emailService, IJwtTokenGenerator jwtGenerator)
        {
            _context = context;
            _emailService = emailService;
            _jwtGenerator = jwtGenerator;
        }

        public async Task<ServiceResult<string>> RegisterAsync(RegisterDto dto)
        {
            if (await _context.TAIKHOAN.AnyAsync(t => t.TenDangNhap == dto.TenDangNhap))
                return ServiceResult<string>.Fail("Tên đăng nhập đã tồn tại.");

            if (!string.IsNullOrWhiteSpace(dto.Email) && await _context.TAIKHOAN.AnyAsync(t => t.Email == dto.Email))
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

        public async Task<ServiceResult<object>> LoginAsync(LoginDto dto)
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
                BCrypt.Net.BCrypt.Verify("dummy", "$2a$11$Ou9z/k/y...dummyhash...");
            }

            if (user == null || !matched) return ServiceResult<object>.Fail("Sai tài khoản hoặc mật khẩu.");
            if (!user.IsVerified) return ServiceResult<object>.Fail("Tài khoản chưa xác thực email.");
            if (!user.IsActive) return ServiceResult<object>.Fail("Tài khoản đã bị vô hiệu hóa.");

            user.Online = true;
            await _context.SaveChangesAsync();

            var token = _jwtGenerator.GenerateToken(user);

            
            return ServiceResult<object>.Ok(new
            {
                token,
                username = user.TenDangNhap,
                role = user.Quyen,
                maNV = user.MaNV,
                hoTen = user.NhanVien.HoTen
            }, "Đăng nhập thành công");
        }

        public async Task<ServiceResult> LogoutAsync(string maNV)
        {
            var user = await _context.TAIKHOAN.FirstOrDefaultAsync(u => u.MaNV == maNV);
            if (user == null) return ServiceResult.Fail("Không tìm thấy người dùng.");

            user.Online = false;
            await _context.SaveChangesAsync();
            return ServiceResult.Ok("Đăng xuất thành công.");
        }

        public async Task<ServiceResult> SendRegisterOtpAsync(string email)
        {
            var user = await _context.TAIKHOAN.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return ServiceResult.Fail("Email không tồn tại.");
            if (user.IsVerified) return ServiceResult.Fail("Tài khoản này đã xác thực rồi.");

            bool sent = await SendOtpInternal(user, "OTP Xác Thực Email");
            return sent ? ServiceResult.Ok("OTP đã được gửi.") : ServiceResult.Fail("Gửi email thất bại.");
        }

        public async Task<ServiceResult> VerifyRegisterOtpAsync(string email, string otp)
        {
            var user = await _context.TAIKHOAN.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return ServiceResult.Fail("Email không tồn tại.");

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
            if (user == null) return ServiceResult.Fail("Email không tồn tại.");

            bool sent = await SendOtpInternal(user, "OTP Đổi Mật Khẩu");
            return sent ? ServiceResult.Ok("OTP đã được gửi.") : ServiceResult.Fail("Gửi email thất bại.");
        }

        public async Task<ServiceResult> VerifyForgotOtpAsync(string email, string otp)
        {
            var user = await _context.TAIKHOAN.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return ServiceResult.Fail("Email không tồn tại.");

            if (user.OTP?.Trim() != otp.Trim() || user.OTPExpireTime < DateTime.UtcNow)
                return ServiceResult.Fail("OTP sai hoặc hết hạn.");

            return ServiceResult.Ok("OTP hợp lệ.");
        }

        public async Task<ServiceResult> ResetPasswordAsync(ResetPasswordDto dto)
        {
            var user = await _context.TAIKHOAN.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null) return ServiceResult.Fail("Email không tồn tại.");

            if (user.OTP?.Trim() != dto.OTP.Trim() || user.OTPExpireTime < DateTime.UtcNow)
                return ServiceResult.Fail("OTP sai hoặc hết hạn.");

            user.MatKhau = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.OTP = null;
            user.OTPExpireTime = null;
            await _context.SaveChangesAsync();
            return ServiceResult.Ok("Đổi mật khẩu thành công.");
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
    }
}