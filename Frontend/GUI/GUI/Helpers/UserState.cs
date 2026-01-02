public static class UserState
{
    public static string AccessToken { get; set; } = "";

    public static string CurrentMaNV { get; set; } = "";
    public static string CurrentTenNV { get; set; } = "";
    public static string CurrentRole { get; set; } = "Staff";

    public static bool IsAdmin => CurrentRole == "Admin";

    public static void Clear()
    {
        AccessToken = "";
        CurrentMaNV = "";
        CurrentTenNV = "";
        CurrentRole = "";
    }
}