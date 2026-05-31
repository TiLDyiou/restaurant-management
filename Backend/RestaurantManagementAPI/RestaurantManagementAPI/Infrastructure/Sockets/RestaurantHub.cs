using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace RestaurantManagementAPI.Infrastructure.Sockets
{
    [Authorize]
    public class RestaurantHub : Hub
    {
        // This Hub is primarily used for broadcasting notifications from the backend to clients.
        // Client methods that can be listened to:
        // - TableStatusChanged (payload: json string or object)
        // - OrderCreated (maHD: string)
        // - KitchenItemReady (msg: string)
        // - UserStatusChanged (maNV: string, isOnline: bool)
    }
}
