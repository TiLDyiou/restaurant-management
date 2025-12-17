namespace RestaurantManagementGUI.Constants
{
    public static class SystemConstants
    {
        // Trạng thái Bàn (Phải khớp 100% với Backend TableController)
        public const string TableEmpty = "Trống";
        public const string TableOccupied = "Có khách";
        public const string TableReserved = "Bàn đã đặt";

        // Trạng thái Món (ChiTietHoaDon)
        public const string ItemWaiting = "Đang chờ";
        public const string ItemCooking = "Đang chế biến"; // Nếu có
        public const string ItemDone = "Đã xong"; // Khớp với ChefViewModel

        // Trạng thái Hóa Đơn
        public const string OrderPending = "Chưa thanh toán";
        public const string OrderCompleted = "Đã hoàn thành";
        public const string OrderPaid = "Đã thanh toán";
    }
}