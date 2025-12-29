using Microsoft.Maui.Devices;

namespace RestaurantManagementGUI.Helpers
{
    public static class ApiConfig
    {
        public static string BaseUrl =>
            DeviceInfo.Platform == DevicePlatform.Android
                ? "https://10.0.2.2:7004/api/"
                : "https://localhost:7004/api/";
        // --------------AUTH-----------------------------------
        public static string Register => $"{BaseUrl}auth/register";
        public static string Login => $"{BaseUrl}auth/login";
        public static string Logout => $"{BaseUrl}auth/logout";

        // OTP đăng kí
        public static string SendRegisterOtp => $"{BaseUrl}auth/otp/register";
        public static string VerifyRegisterOtp => $"{BaseUrl}auth/verify/register";

        // Quên mật khẩu
        public static string ForgotPassword => $"{BaseUrl}auth/forgot-password";
        public static string VerifyForgotOtp => $"{BaseUrl}auth/verify/reset-password";
        public static string ResetPassword => $"{BaseUrl}auth/reset-password";

        // -------------------- USERS -----------------------
        public static string Me => $"{BaseUrl}users/me";
        public static string GetAllUsers => $"{BaseUrl}users";
        public static string UpdateUser(string? id = null)
            => string.IsNullOrEmpty(id)
                ? $"{BaseUrl}users"
                : $"{BaseUrl}users/{id}";

        public static string VerifyEmailOtp => $"{BaseUrl}users/email/verify";
        public static string ResendEmailOtp => $"{BaseUrl}users/email/resend-otp";

        // Tải trạng thái (Soft Delete / Block)
        public static string ToggleUserStatus(string id) => $"{BaseUrl}users/{id}/status";

        // Xóa vĩnh viễn user
        public static string HardDeleteUser(string id) => $"{BaseUrl}users/{id}";

        // ------------------------------- DISHES -------------------------------------
        public static string Dishes => $"{BaseUrl}dishes";
        public static string DishById(string id) => $"{BaseUrl}dishes/{id}";

        // ------------------------------- TABLES (TableController) --------------------
        public static string Tables => $"{BaseUrl}tables";
        public static string UpdateTableStatus(string id) => $"{BaseUrl}tables/{id}/status";

        // ----------------------------- ORDERS -----------------------------------------
        public static string Orders => $"{BaseUrl}orders";
        public static string OrderById(string id) => $"{BaseUrl}orders/{id}";

        // Cập nhật trạng thái món
        public static string UpdateOrderItemStatus(string maHD, string maMA)
            => $"{BaseUrl}orders/{maHD}/items/{maMA}/status";

        // Cập nhật trạng thái hóa đơn
        public static string UpdateOrderStatus(string id) => $"{BaseUrl}orders/{id}/status";

        // Thanh toán
        public static string Checkout(string id) => $"{BaseUrl}orders/{id}/checkout";

        // ----------------------- REPORTS--------------------------
        // api/reports/revenue?startDate=...&endDate=...
        public static string RevenueReport => $"{BaseUrl}reports/revenue";

        // ----------------------- RESERVATIONS ---------------------------------------
        public static string Reservations => $"{BaseUrl}reservations";

        public static string Notifications = $"{BaseUrl}notifications";

        // ----------------------- CHAT & REALTIME -----------------------

        public static string ChatHubUrl => BaseUrl.Replace("/api/", "/restaurantChatHub");

        // Lấy danh sách Inbox (Chứa tin nhắn cuối + UnreadCount để làm in đậm)
        public static string GetInboxList(string maNV) => $"{BaseUrl}Chat/inbox-list/{maNV}";

        // Lấy lịch sử chat
        public static string GetChatHistory(string conversationId) => $"{BaseUrl}Chat/history/{conversationId}";

        // Đánh dấu đã đọc
        public static string MarkRead => $"{BaseUrl}Chat/mark-read";

        // Upload ảnh
        public static string UploadChatImage => $"{BaseUrl}Chat/upload-image";
    }
}