namespace RestaurantManagementGUI.Services
{
    /// <summary>
    /// Service để broadcast event thanh toán thành công
    /// Cho phép các page khác nhau lắng nghe và cập nhật realtime
    /// </summary>
    public static class PaymentEventService
    {
        // Event được trigger khi thanh toán thành công
        public static event EventHandler<PaymentCompletedEventArgs> PaymentCompleted;

        /// <summary>
        /// Gọi hàm này sau khi thanh toán thành công để notify tất cả listeners
        /// </summary>
        public static void NotifyPaymentCompleted(string maHD, decimal tongTien, string tableName, string paymentMethod)
        {
            PaymentCompleted?.Invoke(null, new PaymentCompletedEventArgs
            {
                MaHD = maHD,
                TongTien = tongTien,
                TableName = tableName,
                PaymentMethod = paymentMethod,
                Timestamp = DateTime.Now
            });
        }
    }

    /// <summary>
    /// Data được truyền khi event PaymentCompleted được trigger
    /// </summary>
    public class PaymentCompletedEventArgs : EventArgs
    {
        public string MaHD { get; set; }
        public decimal TongTien { get; set; }
        public string TableName { get; set; }
        public string PaymentMethod { get; set; }
        public DateTime Timestamp { get; set; }
    }
}