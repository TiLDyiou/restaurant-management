using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Models;
using RestaurantManagementGUI.Services;

namespace RestaurantManagementGUI.ViewModels
{
    public partial class AddUserViewModel : BaseViewModel
    {
        private readonly ApiService _apiService;

        [ObservableProperty] private string _hoTen;
        [ObservableProperty] private string _username;
        [ObservableProperty] private string _password;
        [ObservableProperty] private string _confirmPassword;
        [ObservableProperty] private string _sdt;
        [ObservableProperty] private string _email;
        [ObservableProperty] private string _chucVu;
        [ObservableProperty] private string _quyen = "NhanVien";

        public List<string> Roles { get; } = new() { "NhanVien", "Admin", "Dau Bep" };

        public AddUserViewModel(ApiService apiService)
        {
            _apiService = apiService;
        }

        [RelayCommand]
        public async Task AddUserAsync()
        {
            if (!ValidateForm()) return;

            IsBusy = true;

            var newUser = new AddUserRequestModel
            {
                HoTen = HoTen,
                TenDangNhap = Username,
                MatKhau = Password,
                SDT = SDT,
                Email = Email,
                ChucVu = ChucVu,
                Quyen = Quyen
            };

            var response = await _apiService.PostAsync<RegisterResponse>(ApiConfig.Register, newUser);

            if (response.Success)
            {
                await HandleOtpVerification(newUser.Email);
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Lỗi", response.Message, "OK");
            }

            IsBusy = false;
        }

        private async Task HandleOtpVerification(string email)
        {
            bool verified = false;
            while (!verified)
            {
                string otp = await Application.Current.MainPage.DisplayPromptAsync(
                    "Xác thực Email", $"Nhập OTP gửi về {email} (gõ 'Resend' để gửi lại):", "OK", "Hủy");

                if (string.IsNullOrWhiteSpace(otp)) return; // Hủy

                if (otp.Trim().Equals("Resend", StringComparison.OrdinalIgnoreCase))
                {
                    await _apiService.PostAsync<object>(ApiConfig.ResendEmailOtp, new { Email = email });
                    continue;
                }

                var verifyResp = await _apiService.PostAsync<object>(ApiConfig.VerifyRegisterOtp, new { Email = email, OTP = otp });

                if (verifyResp.Success)
                {
                    await Application.Current.MainPage.DisplayAlert("Thành công", "Tạo nhân viên thành công!", "OK");
                    await Application.Current.MainPage.Navigation.PopAsync();
                    verified = true;
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Lỗi", verifyResp.Message, "OK");
                }
            }
        }

        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(HoTen) || string.IsNullOrWhiteSpace(Username) ||
                string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(Email))
            {
                Application.Current.MainPage.DisplayAlert("Lỗi", "Vui lòng nhập đủ thông tin", "OK");
                return false;
            }
            if (Password != ConfirmPassword)
            {
                Application.Current.MainPage.DisplayAlert("Lỗi", "Mật khẩu xác nhận không khớp", "OK");
                return false;
            }
            return true;
        }
    }
}