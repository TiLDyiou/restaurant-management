namespace RestaurantManagementGUI.Services
{
    public class UserSession : IUserSession
    {
        public string Token { get; set; }
        public string MaNV { get; set; }
        public string TenNV { get; set; }
        public string Role { get; set; }
        public bool IsAdmin => Role == "Admin";
        public bool IsAuthenticated => !string.IsNullOrEmpty(Token);

        public void Clear()
        {
            Token = string.Empty;
            MaNV = string.Empty;
            TenNV = string.Empty;
            Role = string.Empty;
        }
    }
}