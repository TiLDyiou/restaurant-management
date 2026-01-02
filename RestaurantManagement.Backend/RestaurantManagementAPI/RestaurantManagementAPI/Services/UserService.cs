using Microsoft.EntityFrameworkCore;
using RestaurantManagementAPI.Common.Wrappers;
using RestaurantManagementAPI.Data;
using RestaurantManagementAPI.DTOs;
using RestaurantManagementAPI.Infrastructure.Email;
using RestaurantManagementAPI.Interfaces;
using RestaurantManagementAPI.Models.Entities;
using System.Security.Cryptography;

namespace RestaurantManagementAPI.Services
{
    public class UserService : IUserService
    {
        private readonly QLNHDbContext _context;
        private readonly IEmailService _emailService;

        public UserService(QLNHDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<ServiceResult<object>> GetUserProfileAsync(string username)
        {
            var user = await _context.TAIKHOAN.Include(t => t.NhanVien).FirstOrDefaultAsync(t => t.TenDangNhap == username);
            if (user == null) return ServiceResult<object>.Fail("Không tìm thấy user");

            return ServiceResult<object>.Ok(new
            {
                user.TenDangNhap,
                user.Quyen,
                user.MaNV,
                user.NhanVien?.HoTen,
                user.NhanVien?.ChucVu,
                user.NhanVien?.SDT,
                user.Email,
                user.IsVerified,
                user.Online
            });
        }

        public async Task<ServiceResult<List<object>>> GetAllUsersAsync()
        {
            var list = await _context.NHANVIEN
                .Include(nv => nv.TaiKhoan)
                .Select(nv => new {
                    nv.MaNV,
                    nv.HoTen,
                    nv.ChucVu,
                    nv.SDT,
                    nv.TrangThai,
                    nv.TaiKhoan.Quyen,
                    nv.TaiKhoan.TenDangNhap,
                    nv.TaiKhoan.IsActive,
                    nv.TaiKhoan.Email,
                    nv.TaiKhoan.Online
                })
                .ToListAsync<object>();
            return ServiceResult<List<object>>.Ok(list);
        }

        public async Task<ServiceResult<object>> UpdateUserAsync(string requesterUsername, bool isAdmin, string? targetMaNV, UpdateUserDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                TaiKhoan? user;
                if (isAdmin && !string.IsNullOrEmpty(targetMaNV))
                    user = await _context.TAIKHOAN.Include(t => t.NhanVien).FirstOrDefaultAsync(u => u.MaNV == targetMaNV);
                else
                    user = await _context.TAIKHOAN.Include(t => t.NhanVien).FirstOrDefaultAsync(u => u.TenDangNhap == requesterUsername);

                if (user == null) return ServiceResult<object>.Fail("User không tồn tại");

                if (!string.IsNullOrWhiteSpace(dto.HoTen)) user.NhanVien.HoTen = dto.HoTen.Trim();
                if (!string.IsNullOrWhiteSpace(dto.ChucVu) && isAdmin) user.NhanVien.ChucVu = dto.ChucVu.Trim();
                if (!string.IsNullOrWhiteSpace(dto.SDT)) user.NhanVien.SDT = dto.SDT.Trim();
                if (!string.IsNullOrWhiteSpace(dto.Quyen) && isAdmin) user.Quyen = dto.Quyen.Trim();
                if (!string.IsNullOrWhiteSpace(dto.MatKhau)) user.MatKhau = BCrypt.Net.BCrypt.HashPassword(dto.MatKhau);

                bool emailChanged = false;
                if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != user.Email)
                {
                    if (await _context.TAIKHOAN.AnyAsync(u => u.Email == dto.Email && u.TenDangNhap != user.TenDangNhap))
                        return ServiceResult<object>.Fail("Email đã tồn tại");

                    user.Email = dto.Email.Trim();
                    user.IsVerified = false;
                    var otp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
                    user.OTP = otp;
                    user.OTPExpireTime = DateTime.UtcNow.AddMinutes(5);

                    try { await _emailService.SendEmailAsync(dto.Email, "OTP Verify", $"OTP: {otp}"); emailChanged = true; }
                    catch { await transaction.RollbackAsync(); return ServiceResult<object>.Fail("Lỗi gửi mail"); }
                }

                _context.TAIKHOAN.Update(user);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return ServiceResult<object>.Ok(new { user.TenDangNhap, user.Email }, emailChanged ? "Cập nhật thành công (Cần verify email)" : "Cập nhật thành công");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ServiceResult<object>.Fail("Lỗi: " + ex.Message);
            }
        }

        public async Task<ServiceResult> VerifyEmailOtpAsync(string email, string otp)
        {
            var user = await _context.TAIKHOAN.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return ServiceResult.Fail("Email không tồn tại");
            if (user.OTP?.Trim() != otp.Trim() || user.OTPExpireTime < DateTime.UtcNow) return ServiceResult.Fail("OTP lỗi hoặc hết hạn");

            user.IsVerified = true;
            user.OTP = null; user.OTPExpireTime = null;
            await _context.SaveChangesAsync();
            return ServiceResult.Ok("Xác thực thành công");
        }

        public async Task<ServiceResult> ResendEmailOtpAsync(string email)
        {
            var user = await _context.TAIKHOAN.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return ServiceResult.Fail("Email không tồn tại");
            if (user.IsVerified) return ServiceResult.Fail("Đã xác thực rồi");

            var otp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            user.OTP = otp;
            user.OTPExpireTime = DateTime.UtcNow.AddMinutes(5);
            await _context.SaveChangesAsync();

            try { await _emailService.SendEmailAsync(user.Email!, "OTP Verify", $"OTP: {otp}"); }
            catch (Exception ex) { return ServiceResult.Fail("Lỗi gửi mail: " + ex.Message); }
            return ServiceResult.Ok("Đã gửi lại OTP");
        }

        public async Task<ServiceResult<object>> ToggleUserStatusAsync(string maNV)
        {
            var nv = await _context.NHANVIEN.Include(e => e.TaiKhoan).FirstOrDefaultAsync(e => e.MaNV == maNV);
            if (nv == null) return ServiceResult<object>.Fail("Không tìm thấy NV");

            nv.TrangThai = nv.TrangThai == "Đang làm" ? "Đã nghỉ" : "Đang làm";
            if (nv.TaiKhoan != null) nv.TaiKhoan.IsActive = (nv.TrangThai == "Đang làm");

            await _context.SaveChangesAsync();
            return ServiceResult<object>.Ok(new { nv.TrangThai, Active = nv.TaiKhoan?.IsActive });
        }

        public async Task<ServiceResult> HardDeleteUserAsync(string maNV)
        {
            var nv = await _context.NHANVIEN.Include(e => e.TaiKhoan).FirstOrDefaultAsync(e => e.MaNV == maNV);
            if (nv == null) return ServiceResult.Fail("Không tìm thấy NV");

            if (nv.TaiKhoan != null) _context.TAIKHOAN.Remove(nv.TaiKhoan);
            _context.NHANVIEN.Remove(nv);
            await _context.SaveChangesAsync();
            return ServiceResult.Ok("Đã xóa vĩnh viễn");
        }
    }
}