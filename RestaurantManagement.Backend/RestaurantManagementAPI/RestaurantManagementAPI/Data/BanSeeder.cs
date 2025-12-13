using RestaurentManagementAPI.Data;
using RestaurentManagementAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace RestaurentManagementAPI.Seeders
{
    public class BanSeeder
    {
        public static async Task SeedTableAsync(IServiceProvider serviceProvider, int soBan = 12)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<QLNHDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<BanSeeder>>();

            var existingBan = await context.BAN.CountAsync();
            if (existingBan >= soBan) return;

            for (int i = 1; i <= soBan; i++)
            {
                var maBan = $"B{i:D2}";
                if (!await context.BAN.AnyAsync(b => b.MaBan == maBan))
                {
                    context.BAN.Add(new Ban
                    {
                        MaBan = maBan,
                        TenBan = $"Bàn {i}",
                        TrangThai = "Trống"
                    });
                }
            }

            await context.SaveChangesAsync();
            logger.LogInformation($"Tạo {soBan} bàn mặc định thành công!");
        }
    }
}
