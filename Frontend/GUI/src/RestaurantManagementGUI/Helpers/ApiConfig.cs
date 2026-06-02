using Microsoft.Maui.Devices;

namespace RestaurantManagementGUI.Helpers
{
    public static class ApiConfig
    {
        public const string DomainUrl = "https://qlnhnhom2.me/";
        public static string BaseUrl => $"{DomainUrl}api/";
        public static string Register => $"{BaseUrl}auth/register";
        public static string Login => $"{BaseUrl}auth/login";
        public static string Logout => $"{BaseUrl}auth/logout";

        public static string SendRegisterOtp => $"{BaseUrl}auth/otp/register";
        public static string VerifyRegisterOtp => $"{BaseUrl}auth/verify/register";

        public static string ForgotPassword => $"{BaseUrl}auth/forgot-password";
        public static string VerifyForgotOtp => $"{BaseUrl}auth/verify/reset-password";
        public static string ResetPassword => $"{BaseUrl}auth/reset-password";

        public static string Me => $"{BaseUrl}users/me";
        public static string GetAllUsers => $"{BaseUrl}users";
        public static string UpdateUser(string? id = null)
            => string.IsNullOrEmpty(id)
                ? $"{BaseUrl}users"
                : $"{BaseUrl}users/{id}";

        public static string VerifyEmailOtp => $"{BaseUrl}users/email/verify";
        public static string ResendEmailOtp => $"{BaseUrl}users/email/resend-otp";

        public static string ToggleUserStatus(string id) => $"{BaseUrl}users/{id}/status";

        public static string HardDeleteUser(string id) => $"{BaseUrl}users/{id}";

        public static string Dishes => $"{BaseUrl}dishes";
        public static string DishById(string id) => $"{BaseUrl}dishes/{id}";
        public static string UploadDishImage => $"{BaseUrl}dishes/upload-image";

        public static string Tables => $"{BaseUrl}tables";
        public static string UpdateTableStatus(string id) => $"{BaseUrl}tables/{id}/status";
        public static string MergeTables => $"{BaseUrl}tables/merge";
        public static string SplitTables(string id) => $"{BaseUrl}tables/{id}/split";
        public static string TransferOrder => $"{BaseUrl}tables/transfer";
        public static string TableHistory(string id) => $"{BaseUrl}tables/{id}/history";

        public static string Orders => $"{BaseUrl}orders";
        public static string OrderById(string id) => $"{BaseUrl}orders/{id}";

        public static string UpdateOrderItemStatus(string maHD, string maMA)
            => $"{BaseUrl}orders/{maHD}/items/{maMA}/status";

        public static string UpdateOrderStatus(string id) => $"{BaseUrl}orders/{id}/status";

        public static string Checkout(string id) => $"{BaseUrl}orders/{id}/checkout";

        public static string RevenueReport => $"{BaseUrl}reports/revenue";

        public static string Reservations => $"{BaseUrl}reservations";

        public static string Notifications = $"{BaseUrl}notifications";

        public static string ChatHubUrl => $"{DomainUrl}restaurantChatHub";
        public static string RestaurantHubUrl => $"{DomainUrl}restaurantHub";

        public static string GetInboxList(string maNV) => $"{BaseUrl}Chat/inbox-list/{maNV}";

        public static string GetChatHistory(string conversationId) => $"{BaseUrl}Chat/history/{conversationId}";

        public static string MarkRead => $"{BaseUrl}Chat/mark-read";

        public static string UploadChatImage => $"{BaseUrl}Chat/upload-image";
    }
}