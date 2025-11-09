using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Devices;
using Microsoft.Maui;
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

        // Đăng nhập
        public static string Login => $"{BaseUrl}Auth/login";

        // Lấy thông tin người dùng hiện tại
        public static string Me => $"{BaseUrl}Auth/me";

        // Cập nhật hồ sơ cá nhân
        public static string UpdateProfile => $"{BaseUrl}Auth/update-profile";

        // Danh sách tất cả user
        public static string Users => $"{BaseUrl}Auth/users";

        // Cho nhân viên nghỉ việc (soft delete)
        public static string SoftDeleteUser(string maNV) => $"{BaseUrl}Auth/soft-delete/{maNV}";

        // Xóa hoàn toàn user khỏi hệ thống (hard delete)
        public static string HardDeleteUser(string maNV) => $"{BaseUrl}Auth/hard-delete/{maNV}";

        // Admin cập nhật thông tin cho user
        public static string AdminUpdateUser(string maNV) => $"{BaseUrl}Auth/admin-update/{maNV}";
        public static string GetAllTables => $"{BaseUrl}Ban";
        public static string UpdateTableStatus(string maBan) => $"{BaseUrl}Ban/{maBan}/trangthai";
    }
}
