using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Models;
using RestaurantManagementGUI.Services;

namespace RestaurantManagementGUI.ViewModels
{
    public partial class EditUserViewModel : BaseViewModel
    {
        private readonly ApiService _apiService;
        private readonly string _maNV;

        [ObservableProperty] private string _hoTen;
        [ObservableProperty] private string _chucVu;
        [ObservableProperty] private string _sdt;
        [ObservableProperty] private string _email;
        [ObservableProperty] private string _matKhau; // Mật khẩu mới (optional)
        [ObservableProperty] private string _confirmPassword;
        [ObservableProperty] private string _quyen;

        public List<string> Roles { get; } = new() { "NhanVien", "Admin", "Dau Bep" };

        public EditUserViewModel(ApiService apiService, UserModel user)
        {
            _apiService = apiService;
            _maNV = user.MaNV;

            // Fill data
            HoTen = user.HoTen;
            ChucVu = user.ChucVu;
            Sdt = user.SDT;
            Email = user.Email;
            Quyen = user.Quyen;
        }

        [RelayCommand]
        public async Task UpdateUserAsync()
        {
            // Validate password nếu có nhập
            if (!string.IsNullOrEmpty(MatKhau) && MatKhau != ConfirmPassword)
            {
                await Application.Current.MainPage.DisplayAlert("Lỗi", "Mật khẩu xác nhận không khớp", "OK");
                return;
            }

            IsBusy = true;

            var updateDto = new UpdateUserRequestModel
            {
                HoTen = HoTen,
                ChucVu = ChucVu,
                SDT = SDT,
                Email = Email,
                Quyen = Quyen,
                MatKhau = string.IsNullOrEmpty(MatKhau) ? null : MatKhau
            };

            var response = await _apiService.PutAsync<object>(ApiConfig.UserById(_maNV), updateDto);

            if (response.Success)
            {
                await Application.Current.MainPage.DisplayAlert("Thành công", "Đã cập nhật thông tin.", "OK");
                await Application.Current.MainPage.Navigation.PopAsync();
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Lỗi", response.Message, "OK");
            }

            IsBusy = false;
        }
    }
}