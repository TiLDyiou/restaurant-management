namespace RestaurantManagementGUI.Services
{
    public interface IUserSession
    {
        string Token { get; set; }
        string MaNV { get; set; }
        string TenNV { get; set; }
        string Role { get; set; }
        bool IsAdmin { get; }
        bool IsAuthenticated { get; }

        void Clear();
    }
}