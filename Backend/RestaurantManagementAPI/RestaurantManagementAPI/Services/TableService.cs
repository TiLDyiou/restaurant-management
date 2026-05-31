using Microsoft.EntityFrameworkCore;
using RestaurantManagementAPI.Common.Wrappers;
using RestaurantManagementAPI.Data;
using RestaurantManagementAPI.Interfaces;
using RestaurantManagementAPI.Models.Entities;

namespace RestaurantManagementAPI.Services
{
    public class TableService : ITableService
    {
        private readonly QLNHDbContext _context;
        private readonly IRealtimeNotifier _notifier;

        public TableService(QLNHDbContext context, IRealtimeNotifier notifier)
        {
            _context = context;
            _notifier = notifier;
        }

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
            
            await _notifier.NotifyTableStatusChangedAsync(maBan, trangThai);

            return ServiceResult<Ban>.Ok(ban, "Cập nhật thành công");
        }
    }
}