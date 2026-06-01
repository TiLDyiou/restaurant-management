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

        [ObservableProperty]
        [JsonPropertyName("trangThai")]
        private string _trangThai;

        [JsonPropertyName("sucChua")]
        public int SucChua { get; set; } = 4;

        [JsonPropertyName("khuVuc")]
        public string KhuVuc { get; set; } = string.Empty;

        [JsonPropertyName("isDeleted")]
        public bool IsDeleted { get; set; }

        [ObservableProperty]
        [JsonPropertyName("maBanGop")]
        private string _maBanGop;

        [JsonIgnore]
        public bool IsMerged => !string.IsNullOrEmpty(MaBanGop);
    }

    public class TableUpdatePayload
    {
        public string MaBan { get; set; }
        public string TrangThai { get; set; }
    }
}