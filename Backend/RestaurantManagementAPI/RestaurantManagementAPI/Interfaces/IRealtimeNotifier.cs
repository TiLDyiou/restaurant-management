using System.Threading.Tasks;

namespace RestaurantManagementAPI.Interfaces
{
    public interface IRealtimeNotifier
    {
        Task NotifyTableStatusChangedAsync(string maBan, string trangThai);
        Task NotifyOrderCreatedAsync(string maHD);
        Task NotifyKitchenItemReadyAsync(string msg);
        Task NotifyUserStatusChangedAsync(string maNV, bool isOnline);
        Task NotifyPaymentSuccessAsync(string maHD, decimal amount);
    }
}
