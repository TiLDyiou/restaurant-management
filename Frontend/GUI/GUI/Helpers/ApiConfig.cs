using Microsoft.Maui.Devices;

namespace RestaurantManagementGUI.Helpers
{
    public static class ApiConfig
    {
        public static string BaseUrl =>
            DeviceInfo.Platform == DevicePlatform.Android
                ? "https://10.0.2.2:7004/api/"
                : "https://localhost:7004/api/";

        public static string Register => $"{BaseUrl}auth/register";
        public static string Login => $"{BaseUrl}auth/login";

        // OTP ??ng ký
        public static string SendRegisterOtp => $"{BaseUrl}auth/otp/register";
        public static string VerifyRegisterOtp => $"{BaseUrl}auth/verify/register";

        // Quên m?t kh?u
        public static string ForgotPassword => $"{BaseUrl}auth/forgot-password";
        public static string VerifyForgotOtp => $"{BaseUrl}auth/verify/reset-password";
        public static string ResetPassword => $"{BaseUrl}auth/reset-password";

        // ================= USERS (UserController) =================
        public static string Me => $"{BaseUrl}users/me";
        public static string GetAllUsers => $"{BaseUrl}users";
        public static string UpdateUser(string? id = null)
            => string.IsNullOrEmpty(id)
                ? $"{BaseUrl}users" 
                : $"{BaseUrl}users/{id}";

        public static string VerifyEmailOtp => $"{BaseUrl}users/email/verify";
        public static string ResendEmailOtp => $"{BaseUrl}users/email/resend-otp";

        // ??i tr?ng thái (Soft Delete / Block)
        public static string ToggleUserStatus(string id) => $"{BaseUrl}users/{id}/status";

        // Xóa v?nh vi?n
        public static string HardDeleteUser(string id) => $"{BaseUrl}users/{id}";

        // ================= DISHES (DishesController) =================
        // RESTful API: Dùng chung URL g?c, khác nhau ? Method (GET, POST, PUT, DELETE)
        public static string Dishes => $"{BaseUrl}dishes";

        public static string DishById(string id) => $"{BaseUrl}dishes/{id}";

        // ================= TABLES (TableController) =================
        public static string Tables => $"{BaseUrl}tables";
        public static string UpdateTableStatus(string id) => $"{BaseUrl}tables/{id}/status";

        // ================= ORDERS (OrdersController) =================
        public static string Orders => $"{BaseUrl}orders";
        public static string OrderById(string id) => $"{BaseUrl}orders/{id}";

        // C?p nh?t tr?ng thái t?ng món trong ??n
        public static string UpdateOrderItemStatus(string maHD, string maMA)
            => $"{BaseUrl}orders/{maHD}/items/{maMA}/status";

        // C?p nh?t tr?ng thái c? hóa ??n
        public static string UpdateOrderStatus(string id) => $"{BaseUrl}orders/{id}/status";

        // Thanh toán
        public static string Checkout(string id) => $"{BaseUrl}orders/{id}/checkout";

        // ================= REPORTS (ReportController) =================
        // Ví d?: api/reports/revenue?startDate=...&endDate=...
        public static string RevenueReport => $"{BaseUrl}reports/revenue";

        // ================= RESERVATIONS (ReservationsController) =================
        public static string Reservations => $"{BaseUrl}reservations";
    }
}