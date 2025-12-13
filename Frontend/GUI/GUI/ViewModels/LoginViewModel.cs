using System.Windows.Input;
using RestaurantManagementGUI.Models;
using RestaurantManagementGUI.Services;
using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Views;
using RestaurantManagementGUI.Views.Staff;
using RestaurantManagementGUI.Views.Authentication;
using RestaurantManagementGUI.Views.Admin;

namespace RestaurantManagementGUI.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly ApiService _apiService;
        private readonly IUserSession _userSession;

        // Properties Binding với View
        private string _username;
        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); }
        }

        private string _password;
        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(); }
        }

        private bool _isPassword;
        public bool IsPassword
        {
            get => _isPassword;
            set { _isPassword = value; OnPropertyChanged(); }
        }

        private string _passwordIcon;
        public string PasswordIcon
        {
            get => _passwordIcon;
            set { _passwordIcon = value; OnPropertyChanged(); }
        }

        private string _loginButtonText;
        public string LoginButtonText
        {
            get => _loginButtonText;
            set { _loginButtonText = value; OnPropertyChanged(); }
        }

        // Commands
        public ICommand LoginCommand { get; }
        public ICommand TogglePasswordCommand { get; }
        public ICommand ForgotPasswordCommand { get; }

        public LoginViewModel(ApiService apiService, IUserSession userSession)
        {
            _apiService = apiService;
            _userSession = userSession;

            // Giá trị mặc định
            IsPassword = true;
            PasswordIcon = "eye_hide.png";
            LoginButtonText = "Đăng nhập";

            LoginCommand = new Command(async () => await ExecuteLogin());
            TogglePasswordCommand = new Command(ExecuteTogglePassword);
            ForgotPasswordCommand = new Command(async () => await ExecuteForgotPassword());
        }

        private void ExecuteTogglePassword()
        {
            IsPassword = !IsPassword;
            PasswordIcon = IsPassword ? "eye_hide.png" : "eye_show.png";
        }

        private async Task ExecuteForgotPassword()
        {
            await Application.Current.MainPage.Navigation.PushAsync(new ForgotPasswordPage());
        }

        private async Task ExecuteLogin()
        {
            if (IsBusy) return;

            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                await App.Current.MainPage.DisplayAlert("Lỗi", "Vui lòng nhập đầy đủ thông tin", "OK");
                return;
            }

            IsBusy = true;
            LoginButtonText = "Đang xử lý...";

            var loginData = new { TenDangNhap = Username, MatKhau = Password };

            // Gọi API qua Service mới
            var response = await _apiService.PostAsync<LoginResponseModel>(ApiConfig.Login, loginData);

            if (response.Success && response.Data != null)
            {
                var data = response.Data;

                // 1. Lưu Session vào Memory (để dùng ngay)
                _userSession.Token = data.Token;
                _userSession.MaNV = data.MaNV;
                _userSession.TenNV = data.Username; // Hoặc HoTen nếu API trả về
                _userSession.Role = data.Role;

                // 2. Lưu vào SecureStorage (để lần sau tự login)
                await SecureStorage.Default.SetAsync("auth_token", data.Token);
                await SecureStorage.Default.SetAsync("user_role", data.Role);

                // 3. Kết nối Socket (Nếu có)
                // _ = SocketListener.Instance.ConnectAsync();

                // 4. Điều hướng
                string chucVu = data.ChucVu?.Trim().ToLower() ?? "";
                Page nextPage;

                if (chucVu.Contains("đầu bếp") || chucVu.Contains("dau bep"))
                    nextPage = new ChefDashboardPage();
                else if (chucVu.Contains("admin"))
                    nextPage = new DashboardPage();
                else
                    nextPage = new StaffDashboardPage();

                Application.Current.MainPage = new NavigationPage(nextPage);
            }
            else
            {
                await App.Current.MainPage.DisplayAlert("Đăng nhập thất bại", response.Message, "OK");
            }

            IsBusy = false;
            LoginButtonText = "Đăng nhập";
        }
    }
}