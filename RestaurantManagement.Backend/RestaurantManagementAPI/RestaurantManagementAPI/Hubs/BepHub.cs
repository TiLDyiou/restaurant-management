using Microsoft.AspNetCore.SignalR;

namespace RestaurentManagementAPI.Hubs
{
    // Hub này sẽ quản lý các kết nối từ client Bếp
    public class KitchenHub : Hub
    {
        // Nhân viên bếp khi kết nối sẽ tham gia vào nhóm "Kitchen"
        public async Task JoinKitchenGroup()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Kitchen");
        }

        // (Tùy chọn) Khi client bếp ngắt kết nối
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Kitchen");
            await base.OnDisconnectedAsync(exception);
        }
    }
}