using RestaurentManagementAPI.Data;
using RestaurentManagementAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net; // Thêm using cho BCrypt

namespace RestaurentManagementAPI.Data
{
    public class DataSeeder
    {
        public static async Task SeedAdminAsync(IServiceProvider serviceProvider)
        {
            // Dùng serviceProvider để lấy DbContext và Logger
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<QLNHDbContext>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataSeeder>>();

                try
                {
                    // 1. Kiểm tra xem tài khoản "admin" đã tồn tại chưa
                    if (await context.TAIKHOAN.AnyAsync(t => t.TenDangNhap == "admin"))
                    {
                        return; // Đã có "admin", không làm gì cả
                    }

                    logger.LogInformation("Không tìm thấy tài khoản admin. Bắt đầu tạo...");

                    // 2. Kiểm tra xem nhân viên "ADMIN" đã tồn tại chưa (phòng trường hợp lỗi)
                    var adminNhanVien = await context.NHANVIEN.FirstOrDefaultAsync(nv => nv.MaNV == "ADMIN");

                    if (adminNhanVien == null)
                    {
                        // Nếu nhân viên "ADMIN" chưa có, tạo mới
                        adminNhanVien = new NhanVien
                        {
                            MaNV = "ADMIN", // Dùng mã 5 ký tự
                            HoTen = "Nguyễn Đức Đại", // Đây là [Required]
                            ChucVu = "Admin",
                            NgayVaoLam = DateTime.Now,
                            TrangThai = "Đang làm"
                        };
                        context.NHANVIEN.Add(adminNhanVien);
                    }

                    // 3. Hash mật khẩu (dùng chính xác thư viện của bạn)
                    string hashedPassword = BCrypt.Net.BCrypt.HashPassword("123456");

                    // 4. Tạo TaiKhoan
                    var adminTaiKhoan = new TaiKhoan
                    {
                        TenDangNhap = "admin",
                        MatKhau = hashedPassword,
                        MaNV = adminNhanVien.MaNV, // Gán khóa ngoại
                        Quyen = "Admin" // Đặt quyền là "Admin"
                    };
                    context.TAIKHOAN.Add(adminTaiKhoan);

                    // 5. Lưu tất cả thay đổi vào Database
                    await context.SaveChangesAsync();

                    logger.LogInformation("Tạo tài khoản admin thành công!");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Đã xảy ra lỗi khi seeding admin.");
                }
            }
        }
    }
}