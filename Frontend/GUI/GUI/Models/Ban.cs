

using CommunityToolkit.Mvvm.ComponentModel; 
using System.Text.Json.Serialization;

namespace RestaurantManagementGUI.Models
{
   
    public class Ban : ObservableObject
    {
        
        private string _maBan = string.Empty;
        [JsonPropertyName("maBan")]
        public string MaBan
        {
            get => _maBan;
            set => SetProperty(ref _maBan, value); 
        }

     
        private string? _tenBan;
        [JsonPropertyName("tenBan")]
        public string? TenBan
        {
            get => _tenBan;
            set => SetProperty(ref _tenBan, value); 
        }

   
        private string? _trangThai;
        [JsonPropertyName("trangThai")]
        public string? TrangThai
        {
            get => _trangThai;
            
            set => SetProperty(ref _trangThai, value);
        }
    }
}