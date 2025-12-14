
namespace RestaurantManagementGUI.Helpers
{
    public static class UserState
    {
        // Lưu mã nhân viên đang đăng nhập
        public static string CurrentMaNV { get; set; } = "";

        // Lưu tên nhân viên để hiển thị "Xin chào..."
        public static string CurrentTenNV { get; set; } = "";

        // Lưu quyền hạn: "Admin" hoặc "Staff" (hoặc "NhanVien")
        public static string CurrentRole { get; set; } = "Staff";

        // Hàm kiểm tra nhanh có phải Admin không
        public static bool IsAdmin => CurrentRole == "Admin";

        // Hàm xóa dữ liệu khi đăng xuất
        public static void Clear()
        {
            CurrentMaNV = "";
            CurrentTenNV = "";
            CurrentRole = "";
        }
    }
}
