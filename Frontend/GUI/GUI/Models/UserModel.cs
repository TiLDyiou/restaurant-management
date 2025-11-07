using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace RestaurantManagementGUI.Models
{
    public class UserModel : INotifyPropertyChanged
    {
        private string _trangThai = "Đang làm";

        [JsonPropertyName("maNV")]
        public string MaNV { get; set; }

        [JsonPropertyName("hoTen")]
        public string HoTen { get; set; }

        [JsonPropertyName("chucVu")]
        public string ChucVu { get; set; }

        [JsonPropertyName("sdt")]
        public string SDT { get; set; }

        [JsonPropertyName("quyen")]
        public string Quyen { get; set; }

        [JsonPropertyName("tenDangNhap")]
        public string TenDangNhap { get; set; }

        [JsonPropertyName("trangThai")]
        public string TrangThai
        {
            get => _trangThai;
            set
            {
                if (_trangThai != value)
                {
                    _trangThai = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
