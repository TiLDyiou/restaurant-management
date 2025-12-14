using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace RestaurantManagementGUI.Models
{
    public partial class Ban : ObservableObject
    {
        [JsonPropertyName("maBan")]
        public string MaBan { get; set; }

        [JsonPropertyName("tenBan")]
        public string TenBan { get; set; }

        // Khi Socket cập nhật biến này, UI sẽ tự đổi màu
        [ObservableProperty]
        [JsonPropertyName("trangThai")]
        private string _trangThai;
    }
}