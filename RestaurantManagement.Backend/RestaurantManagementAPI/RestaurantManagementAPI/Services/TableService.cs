using Microsoft.EntityFrameworkCore;
using RestaurantManagementAPI.Common.Wrappers;
using RestaurantManagementAPI.Data;
using RestaurantManagementAPI.Infrastructure.Sockets;
using RestaurantManagementAPI.Interfaces;
using RestaurantManagementAPI.Models.Entities;
using System.Text.Json;

namespace RestaurantManagementAPI.Services
{
    public class TableService : ITableService
    {
        private readonly QLNHDbContext _context;
        public TableService(QLNHDbContext context) { _context = context; }

        public async Task<ServiceResult<List<Ban>>> GetAllBanAsync()
        {
            var list = await _context.BAN.ToListAsync();
            return ServiceResult<List<Ban>>.Ok(list);
        }

        public async Task<ServiceResult<Ban>> UpdateStatusAsync(string maBan, string trangThai)
        {
            var ban = await _context.BAN.FindAsync(maBan);
            if (ban == null) 
                return ServiceResult<Ban>.Fail("Bàn không tồn tại");

            ban.TrangThai = trangThai;
            await _context.SaveChangesAsync();
            if (TcpSocketServer.Instance != null)
            {
                var payload = new { MaBan = maBan, TrangThai = trangThai };
                await TcpSocketServer.Instance.BroadcastAsync($"TABLE|{JsonSerializer.Serialize(payload)}");
            }

            return ServiceResult<Ban>.Ok(ban, "Cập nhật thành công");
        }
    }
}