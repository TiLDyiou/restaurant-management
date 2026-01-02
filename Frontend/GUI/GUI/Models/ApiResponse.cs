using System.Text.Json.Serialization;

namespace RestaurantManagementGUI.Models
{
    // Dùng cho API có dữ liệu trả về (Login, GetOrders,...)
    public class ApiResponse<T>
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public T? Data { get; set; }
    }

    // Dùng cho API chỉ trả về thông báo (Logout, UpdateStatus, Delete...)
    public class ApiResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}