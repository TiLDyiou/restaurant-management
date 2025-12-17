namespace RestaurantManagementAPI.Common.Constants
{
    public static class SystemConstants
    {
        // Trạng thái Món ăn
        public const string ItemWaiting = "Đang chờ";
        public const string ItemCooking = "Đang chế biến";
        public const string ItemReady = "Đã xong";

        // Trạng thái Bàn
        public const string TableEmpty = "Trống";
        public const string TableOccupied = "Có khách";
        public const string TableReserved = "Bàn đã đặt";

        // Trạng thái Hóa đơn
        public const string OrderUnpaid = "Chưa thanh toán";
        public const string OrderPaid = "Đã thanh toán";

        // Loại thông báo
        public const string NotiKitchen = "BEP";
        public const string NotiService = "PHUCVU";
    }
}