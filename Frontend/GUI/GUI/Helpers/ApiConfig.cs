using Microsoft.Maui.Devices;

namespace RestaurantManagementGUI.Helpers
{
    public static class ApiConfig
    {
        public static string BaseUrl =>
            DeviceInfo.Platform == DevicePlatform.Android
                ? "https://10.0.2.2:7004/api/"
                : "https://localhost:7004/api/";

        public const string Register = "auth/register";
        public const string Login = "auth/login";
        public const string SendRegisterOtp = "auth/otp/register";
        public const string VerifyRegisterOtp = "auth/verify/register";
        public const string ForgotPassword = "auth/forgot-password";
        public const string VerifyResetPasswordOtp = "auth/verify/reset-password";
        public const string ResetPassword = "auth/reset-password";


        public const string UserProfile = "users/me";
        public const string Users = "users";
        public const string VerifyEmailOtp = "users/email/verify";
        public const string ResendEmailOtp = "users/email/resend-otp";

        public static string UserById(string id) => $"users/{id}";
        public static string ToggleUserStatus(string id) => $"users/{id}/status";


        public const string Tables = "tables";
        public static string UpdateTableStatus(string id) => $"tables/{id}/status";


        public const string Dishes = "dishes";                                  
        public static string DishById(string id) => $"dishes/{id}";

        public const string Orders = "orders";

        public static string OrderById(string id) => $"orders/{id}";
        public static string UpdateOrderItemStatus(string maHD, string maMA)
            => $"orders/{maHD}/items/{maMA}/status";
        public static string UpdateOrderStatus(string id) => $"orders/{id}/status";
        public static string Checkout(string id) => $"orders/{id}/checkout";

        public const string Reservations = "reservations";

        public const string RevenueReport = "reports/revenue";
    }
}