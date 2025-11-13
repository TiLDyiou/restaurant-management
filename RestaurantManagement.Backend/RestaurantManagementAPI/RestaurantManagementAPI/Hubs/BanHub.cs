using Microsoft.AspNetCore.SignalR;

namespace RestaurentManagementAPI.Hubs
{
    public class BanHub : Hub
    {
        // Hub dùng để gửi thông báo bàn cập nhật
        public async Task GuiTrangThaiBan(string maBan, string trangThai)
        {
            await Clients.All.SendAsync("BanUpdated", new { MaBan = maBan, TrangThai = trangThai });
        }
    }
}
