using Microsoft.AspNetCore.SignalR;
using RestaurantManagementAPI.Interfaces;
using System.Text.Json;
using System.Threading.Tasks;

namespace RestaurantManagementAPI.Infrastructure.Sockets
{
    public class SignalRNotifier : IRealtimeNotifier
    {
        private readonly IHubContext<RestaurantHub> _hubContext;

        public SignalRNotifier(IHubContext<RestaurantHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyTableStatusChangedAsync(string maBan, string trangThai)
        {
            var payload = new { MaBan = maBan, TrangThai = trangThai };

            // 1. SignalR broadcast
            await _hubContext.Clients.All.SendAsync("TableStatusChanged", payload);

            // 2. Backward compatibility: TCP socket broadcast
            if (TcpSocketServer.Instance != null)
            {
                await TcpSocketServer.Instance.BroadcastAsync($"TABLE|{JsonSerializer.Serialize(payload)}");
            }
        }

        public async Task NotifyOrderCreatedAsync(string maHD)
        {
            // 1. SignalR broadcast
            await _hubContext.Clients.All.SendAsync("OrderCreated", maHD);

            // 2. Backward compatibility: TCP socket broadcast
            if (TcpSocketServer.Instance != null)
            {
                await TcpSocketServer.Instance.BroadcastAsync($"ORDER|{maHD}");
            }
        }

        public async Task NotifyKitchenItemReadyAsync(string msg)
        {
            // 1. SignalR broadcast
            await _hubContext.Clients.All.SendAsync("KitchenItemReady", msg);

            // 2. Backward compatibility: TCP socket broadcast
            if (TcpSocketServer.Instance != null)
            {
                await TcpSocketServer.Instance.BroadcastAsync($"KITCHEN_DONE|{msg}");
            }
        }

        public async Task NotifyUserStatusChangedAsync(string maNV, bool isOnline)
        {
            // 1. SignalR broadcast
            await _hubContext.Clients.All.SendAsync("UserStatusChanged", maNV, isOnline);

            // 2. Backward compatibility: TCP socket broadcast
            if (TcpSocketServer.Instance != null)
            {
                string statusStr = isOnline ? "TRUE" : "FALSE";
                await TcpSocketServer.Instance.BroadcastAsync($"STATUS|{maNV}|{statusStr}");
            }
        }
        public async Task NotifyPaymentSuccessAsync(string maHD, decimal amount)
        {
            await _hubContext.Clients.All.SendAsync("PaymentSuccess", maHD, amount);
            
            if (TcpSocketServer.Instance != null)
            {
                var payload = new { MaHD = maHD, Amount = amount };
                await TcpSocketServer.Instance.BroadcastAsync($"PAYMENT_SUCCESS|{JsonSerializer.Serialize(payload)}");
            }
        }
    }
}
