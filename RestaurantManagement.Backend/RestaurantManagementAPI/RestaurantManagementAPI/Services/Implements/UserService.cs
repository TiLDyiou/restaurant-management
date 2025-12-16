using Microsoft.EntityFrameworkCore;
using RestaurantManagementAPI.Data;
using RestaurantManagementAPI.DTOs;
using RestaurantManagementAPI.Models.Entities;
using RestaurantManagementAPI.Services.Interfaces;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RestaurantManagementAPI.Services.Implements
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

        public async Task<object?> GetUserProfileAsync(string username)
        {
            var user = await _context.TAIKHOAN
                .Include(t => t.NhanVien)
                .FirstOrDefaultAsync(t => t.TenDangNhap == username);
            if (user == null) 
                return null;
            return new 
            { 
                user.TenDangNhap, 
                user.Quyen, user.MaNV, 
                user.NhanVien?.HoTen, 
                user.NhanVien?.ChucVu, 
                user.NhanVien?.SDT, 
                user.Email, user.IsVerified,
                user.Online
            };
        }

        public async Task<List<object>> GetAllUsersAsync()
        {
            return await _context.NHANVIEN
                .Include(nv => nv.TaiKhoan)
                .Select(nv => new { nv.MaNV, nv.HoTen, nv.ChucVu, nv.SDT, nv.TaiKhoan.Quyen, nv.TaiKhoan.TenDangNhap, nv.TrangThai, nv.TaiKhoan.IsActive, nv.TaiKhoan.Email, Online = nv.TaiKhoan.Online })
                .ToListAsync<object>();
        }

        public async Task<(bool Success, string Message, object? Data)> UpdateUserAsync(string requesterUsername, bool isAdmin, string? targetMaNV, UpdateUserDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                TaiKhoan? user;
                if (isAdmin && !string.IsNullOrEmpty(targetMaNV))
                    user = await _context.TAIKHOAN
                        .Include(t => t.NhanVien)
                        .FirstOrDefaultAsync(u => u.MaNV == targetMaNV);
                else
                    user = await _context.TAIKHOAN
                        .Include(t => t.NhanVien)
                        .FirstOrDefaultAsync(u => u.TenDangNhap == requesterUsername);

                if (user == null) 
                    return (false, "Người dùng không tồn tại.", null);

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

                bool emailChanged = false;
                if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != user.Email)
                {
                    if (await _context.TAIKHOAN.AnyAsync(u => u.Email == dto.Email && u.TenDangNhap != user.TenDangNhap))
                    {
                        return (false, "Email này đã được sử dụng bởi tài khoản khác.", null);
                    }

                    user.Email = dto.Email.Trim();
                    user.IsVerified = false;

                    var otp = GenerateSecureOtp();
                    user.OTP = otp;
                    user.OTPExpireTime = DateTime.UtcNow.AddMinutes(5);

                    try
                    {
                        await _emailService.SendEmailAsync(dto.Email, "OTP Xác Thực Email", $"Mã OTP: {otp}");
                        emailChanged = true;
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        return (false, "Không thể gửi OTP đến email mới. Vui lòng kiểm tra lại địa chỉ email.", null);
                    }
                }

                _context.TAIKHOAN.Update(user);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, emailChanged ? "Cập nhật thành công. Vui lòng kiểm tra email để xác thực lại." : "Cập nhật thành công.", new { user.TenDangNhap, user.Email });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, "Lỗi hệ thống: " + ex.Message, null);
            }
        }

        public async Task<(bool Success, string Message)> VerifyEmailOtpAsync(string email, string otp)
        {
            var user = await _context.TAIKHOAN.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) 
                return (false, "Email không tồn tại.");

            if (user.OTP?.Trim() != otp.Trim() || user.OTPExpireTime < DateTime.UtcNow)
                return (false, "OTP không hợp lệ hoặc đã hết hạn.");

            user.IsVerified = true;
            user.OTP = null;
            user.OTPExpireTime = null;

            await _context.SaveChangesAsync();
            return (true, "Xác thực email thành công.");
        }

        public async Task<(bool Success, string Message)> ResendEmailOtpAsync(string email)
        {
            var user = await _context.TAIKHOAN.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) 
                return (false, "Email không tồn tại.");

            if (user.IsVerified) 
                return (false, "Email này đã xác thực, không cần gửi lại.");

            if (user.OTPExpireTime.HasValue && user.OTPExpireTime.Value > DateTime.UtcNow.AddMinutes(4))
            {
                return (false, "Vui lòng đợi 1 phút trước khi gửi lại.");
            }

            var otp = GenerateSecureOtp();
            user.OTP = otp;
            user.OTPExpireTime = DateTime.UtcNow.AddMinutes(5);
            await _context.SaveChangesAsync();

            try
            {
                await _emailService.SendEmailAsync(user.Email!, "OTP Xác Thực Email", $"Mã OTP: {otp}");
            }
            catch (Exception ex)
            {
                return (false, "Lỗi gửi mail: " + ex.Message);
            }
            return (true, "Đã gửi lại OTP.");
        }
        public async Task<(bool Success, string Message, object? Data)> ToggleUserStatusAsync(string maNV)
        {
            var employee = await _context.NHANVIEN
                .Include(e => e.TaiKhoan)
                .FirstOrDefaultAsync(e => e.MaNV == maNV);

            if (employee == null) 
                return (false, "Không tìm thấy NV.", null);

            employee.TrangThai = employee.TrangThai == "Đang làm" ? "Đã nghỉ" : "Đang làm";
            if (employee.TaiKhoan != null) 
                employee.TaiKhoan.IsActive = employee.TrangThai == "Đang làm";

            _context.NHANVIEN.Update(employee);
            await _context.SaveChangesAsync();
            return (true, "Đổi trạng thái thành công.", new { employee.TrangThai, Active = employee.TaiKhoan?.IsActive });
        }

        public async Task<(bool Success, string Message)> HardDeleteUserAsync(string maNV)
        {
            var employee = await _context.NHANVIEN
                .Include(e => e.TaiKhoan)
                .FirstOrDefaultAsync(e => e.MaNV == maNV);
            if (employee == null) 
                return (false, "Không tìm thấy NV.");

            if (employee.TaiKhoan != null) 
                _context.TAIKHOAN.Remove(employee.TaiKhoan);
            _context.NHANVIEN.Remove(employee);
            await _context.SaveChangesAsync();
            return (true, "Đã xóa vĩnh viễn.");
        }
        private string GenerateSecureOtp()
        {
            return RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        }
    }
}