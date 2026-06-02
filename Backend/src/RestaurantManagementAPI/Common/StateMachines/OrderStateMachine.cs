using RestaurantManagementAPI.Common.Constants;

namespace RestaurantManagementAPI.Common.StateMachines
{
    public static class OrderStateMachine
    {
        // Order Statuses
        public const string OrderUnpaid = SystemConstants.OrderUnpaid; // "Chưa thanh toán"
        public const string OrderPaid = SystemConstants.OrderPaid;     // "Đã thanh toán"
        public const string OrderCanceled = "Đã huỷ";

        // Order Item Statuses
        public const string ItemWaiting = SystemConstants.ItemWaiting;   // "Đang chờ"
        public const string ItemCooking = SystemConstants.ItemCooking;   // "Đang chế biến"
        public const string ItemReady = SystemConstants.ItemReady;       // "Đã xong"

        /// <summary>
        /// Validates if an Order status transition is allowed.
        /// </summary>
        public static bool IsOrderTransitionAllowed(string currentStatus, string newStatus)
        {
            if (currentStatus == newStatus) return true;

            // Terminal states: Paid and Canceled cannot transition to anything else
            if (currentStatus == OrderPaid || currentStatus == OrderCanceled)
            {
                return false;
            }

            // Unpaid can transition to Paid or Canceled
            if (currentStatus == OrderUnpaid)
            {
                return newStatus == OrderPaid || newStatus == OrderCanceled;
            }

            return false;
        }

        /// <summary>
        /// Validates if an Order Item status transition is allowed.
        /// </summary>
        public static bool IsItemTransitionAllowed(string currentStatus, string newStatus)
        {
            if (currentStatus == newStatus) return true;

            if (currentStatus == ItemWaiting)
            {
                return newStatus == ItemCooking || newStatus == ItemReady;
            }

            if (currentStatus == ItemCooking)
            {
                return newStatus == ItemReady;
            }

            // Ready is terminal
            if (currentStatus == ItemReady)
            {
                return false;
            }

            return false;
        }
    }
}
