// Trong MenuNhaHang/Helpers/ApiConstants.cs
public static class ApiConstants
{
    // Đảm bảo bạn đã thay đổi thành HTTP và port 5276 cho Windows
#if ANDROID
        public const string BaseUrl = "http://10.0.2.2:5276";
#else
    public const string BaseUrl = "http://localhost:5276"; // <--- Dùng chính xác dòng này cho Windows
#endif

    // Bạn cần xác nhận endpoint này trong code API
    public const string MonAnEndpoint = "/api/monan";
    // ...
}