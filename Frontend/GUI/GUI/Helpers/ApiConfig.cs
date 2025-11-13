// File: Helpers/ApiConfig.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Devices;

namespace RestaurantManagementGUI.Helpers
{
    public static class ApiConfig
    {
        // SỬA 1: Xóa /api/ khỏi BaseUrl (vì API Controller của bạn dùng route tuyệt đối)
        public static string BaseUrl =>
            DeviceInfo.Platform == DevicePlatform.Android
                ? "https://10.0.2.2:7004/" // Xóa /api/
                : "https://localhost:7004/"; // Xóa /api/


        // SỬA 2: Thêm "api/" vào tất cả các route Auth cũ
        public static string Register => $"{BaseUrl}api/Auth/register";
        public static string Login => $"{BaseUrl}api/Auth/login";
        public static string Me => $"{BaseUrl}api/Auth/me";
        public static string UpdateProfile => $"{BaseUrl}api/Auth/update-profile";
        public static string Users => $"{BaseUrl}api/Auth/users";
        public static string SoftDeleteUser(string maNV) => $"{BaseUrl}api/Auth/soft-delete/{maNV}";
        public static string HardDeleteUser(string maNV) => $"{BaseUrl}api/Auth/hard-delete/{maNV}";
        public static string AdminUpdateUser(string maNV) => $"{BaseUrl}api/Auth/admin-update/{maNV}";


        // --- SỬA LỖI API MÓN ĂN (KHỚP VỚI DISHESCONTROLLER.CS) ---

        // GET /api/dishes
        public static string GetFoodMenu => $"{BaseUrl}api/dishes";

        // POST /api/add_dish
        public static string AddDish => $"{BaseUrl}api/add_dish"; // <-- SỬA Ở ĐÂY

        // PUT /api/update_dish/{maMA}
        public static string UpdateDish(string maMA) => $"{BaseUrl}api/update_dish/{maMA}"; // <-- SỬA Ở ĐÂY

        // DELETE /api/softdelete_dish/{maMA}
        public static string SoftDeleteDish(string maMA) => $"{BaseUrl}api/softdelete_dish/{maMA}"; // <-- SỬA Ở ĐÂY

        // POST /api/Orders/submit
        public static string SubmitOrder => $"{BaseUrl}api/Orders/submit";
    }
}