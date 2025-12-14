using Microsoft.EntityFrameworkCore;
using RestaurantManagementAPI.Data;
using RestaurantManagementAPI.Models.Entities;
using RestaurantManagementAPI.Services.Interfaces;
using System.Text.Json;

namespace RestaurantManagementAPI.Services.Implements
{
    public class TableService : ITableService
    {
        private readonly QLNHDbContext _context;

        public TableService(QLNHDbContext context)
        {
            _context = context;
        }

        public async Task<List<Ban>> GetAllBanAsync()
        {
            return await _context.BAN.ToListAsync();
        }

        public async Task<(bool Success, string Message, Ban? Data)> UpdateStatusAsync(string maBan, string trangThai)
        {
            var ban = await _context.BAN.FindAsync(maBan);
            if (ban == null) 
                return (false, "Bàn không tồn tại", null);

            ban.TrangThai = trangThai;
            await _context.SaveChangesAsync();

            if (TcpSocketServer.Instance != null)
            {
                var payload = new { MaBan = maBan, TrangThai = trangThai };
                string jsonTable = JsonSerializer.Serialize(payload);
                await TcpSocketServer.Instance.BroadcastAsync($"TABLE|{jsonTable}");
            }

            return (true, "Cập nhật trạng thái thành công", ban);
        }
    }
}