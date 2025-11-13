using System;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Devices;

namespace RestaurantManagementGUI.Helpers
{
    public static class ApiConfig
    {
        public static string BaseUrl =>
            DeviceInfo.Platform == DevicePlatform.Android
                ? "https://10.0.2.2:7004/api/"
                : "https://localhost:7004/api/";

        // Đăng ký tài khoản (chỉ dành cho admin)
        public static string Register => $"{BaseUrl}Auth/register";
        public static string SendRegisterOtp => $"{BaseUrl}Auth/send-register-otp";
        public static string VerifyRegisterOtp => $"{BaseUrl}Auth/verify-register-otp";

        // Đăng nhập
        public static string Login => $"{BaseUrl}Auth/login";

        // Lấy thông tin người dùng hiện tại
        public static string Me => $"{BaseUrl}Auth/me";

        // Danh sách tất cả user (adnin chỉ xem)
        public static string Users => $"{BaseUrl}Auth/users";

        // Cho nhân viên nghỉ việc (soft delete)
        public static string SoftDeleteUser(string maNV) => $"{BaseUrl}Auth/soft-delete/{maNV}";

        // Xóa hoàn toàn user khỏi hệ thống (hard delete)
        public static string HardDeleteUser(string maNV) => $"{BaseUrl}Auth/hard-delete/{maNV}";
        // cập nhật thông tin user
        public static string UpdateUser(string? maNV = null)
            => string.IsNullOrWhiteSpace(maNV)
                ? $"{BaseUrl}Auth/update-user"
                : $"{BaseUrl}Auth/update-user/{maNV}";
        public static string UpdateProfile => $"{BaseUrl}Auth/update-profile";

        // Quản lý bàn
        public static string GetAllTables => $"{BaseUrl}Ban";
        public static string UpdateTableStatus(string maBan) => $"{BaseUrl}Ban/{maBan}/trangthai";

        // Quên mật khẩu, gửi mã OTP và đặt lại mật khẩu
        public static string SendForgotOtp => $"{BaseUrl}Auth/forgot-password";
        public static string VerifyForgotOtp => $"{BaseUrl}Auth/verify-forgot-otp";
        public static string ResetPassword => $"{BaseUrl}Auth/reset-password";
        // Xác thực Email OTP (khi cập nhật email)
        public static string VerifyEmailOtp => $"{BaseUrl}Auth/verify-email-otp";
        // Gửi lại Email OTP
        public static string ResendEmailOtp => $"{BaseUrl}Auth/resend-email-otp";
    }
}
