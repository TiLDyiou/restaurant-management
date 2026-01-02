using RestaurantManagementAPI.Data;
using RestaurantManagementAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace RestaurantManagementAPI.Data
{
    public class DataSeeder
    {
        public static async Task SeedAdminAsync(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<QLNHDbContext>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataSeeder>>();

                try
                {
                    if (await context.TAIKHOAN.AnyAsync(t => t.TenDangNhap == "admin"))
                        return;

                    logger.LogInformation("Không tìm thấy tài khoản admin. Bắt đầu tạo...");

                    var adminNhanVien = await context.NHANVIEN.FirstOrDefaultAsync(nv => nv.MaNV == "ADMIN");
                    if (adminNhanVien == null)
                    {
                        adminNhanVien = new NhanVien
                        {
                            MaNV = "ADMIN",
                            HoTen = "Nguyễn Trần Gia Bảo",
                            ChucVu = "Admin",
                            SDT = "0969390382",
                            NgayVaoLam = DateTime.Now,
                            TrangThai = "Đang làm"
                        };
                        context.NHANVIEN.Add(adminNhanVien);
                    }
                    string hashedPassword = BCrypt.Net.BCrypt.HashPassword("123456");
                    var adminTaiKhoan = new TaiKhoan
                    {
                        TenDangNhap = "admin",
                        MatKhau = hashedPassword,
                        MaNV = adminNhanVien.MaNV,
                        Quyen = "Admin",
                        Email = "nguyentrangiabao7100@gmail.com",
                        IsActive = true,
                        IsVerified = true
                    };

                    context.TAIKHOAN.Add(adminTaiKhoan);
                    await context.SaveChangesAsync();

                    logger.LogInformation("Tạo tài khoản admin thành công với email đã xác thực!");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Đã xảy ra lỗi khi seeding admin.");
                }
            }
        }
    }
}
