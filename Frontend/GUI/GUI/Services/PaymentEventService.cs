namespace RestaurantManagementGUI.Services
{
    public static class PaymentEventService
    {
        public static event EventHandler<PaymentCompletedEventArgs> PaymentCompleted;

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

    public class PaymentCompletedEventArgs : EventArgs
    {
        public string MaHD { get; set; }
        public decimal TongTien { get; set; }
        public string TableName { get; set; }
        public string PaymentMethod { get; set; }
        public DateTime Timestamp { get; set; }
    }
}