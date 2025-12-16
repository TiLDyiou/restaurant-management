using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Maui.Storage;
using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Models;


namespace RestaurantManagementGUI
{
    public partial class EditUserPage : ContentPage
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly string _maNV;
        private readonly string _currentEmail;
        public EditUserPage(UserModel user)
        {
            InitializeComponent();

#if DEBUG

            _httpClient = new HttpClient(GetInsecureHandler());
#else

            _httpClient = new HttpClient();
#endif

            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            _maNV = user.MaNV;
            _currentEmail = user.Email;
            MaNVLabel.Text = $"Mã nhân viên: {user.MaNV}";
            HoTenEntry.Text = user.HoTen;
            ChucVuEntry.Text = user.ChucVu;
            SDTEntry.Text = user.SDT;
            EmailEntry.Text = user.Email;
            string role = user.Quyen?.Trim() ?? "NhanVien";
            QuyenPicker.SelectedItem = role;
        }

        private async void OnUpdateUserClicked(object sender, EventArgs e)
        {
            string newEmail = EmailEntry.Text?.Trim();
            string password = MatKhauEntry.Text?.Trim();
            string confirmPassword = ConfirmPasswordEntry.Text?.Trim();
            if (!string.IsNullOrEmpty(password))
            {
                if (string.IsNullOrEmpty(confirmPassword))
                {
                    await DisplayAlert("Lỗi", "Vui lòng nhập xác nhận mật khẩu.", "OK");
                    return;
                }
                if (password != confirmPassword)
                {
                    await DisplayAlert("Lỗi", "Mật khẩu xác nhận không khớp.", "OK");
                    return;
                }
            }

            UpdateButton.IsEnabled = false;

            UpdateButton.Text = "Đang cập nhật...";


            try
            {
                var token = await SecureStorage.Default.GetAsync("auth_token");
                if (string.IsNullOrEmpty(token)) return;

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var updateUserReq = new UpdateUserRequestModel
                {
                    HoTen = HoTenEntry.Text?.Trim(),
                    ChucVu = ChucVuEntry.Text?.Trim(),
                    SDT = SDTEntry.Text?.Trim(),
                    Email = newEmail,
                    MatKhau = password,
                    Quyen = QuyenPicker.SelectedItem?.ToString()
                };

                var url = ApiConfig.UpdateUser(_maNV);
                var response = await _httpClient.PutAsJsonAsync(url, updateUserReq);
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(_jsonOptions);

                if (response.IsSuccessStatusCode && result != null && result.Success)
                {
                    if (!string.Equals(_currentEmail, newEmail, StringComparison.OrdinalIgnoreCase))
                    {
                        await HandleOtpFlow(newEmail);
                    }
                    else
                    {
                        await DisplayAlert("Thành công", result.Message ?? "Cập nhật thành công!", "OK");
                        await Navigation.PopAsync();
                    }
                }
                else
                {
                    await DisplayAlert("Lỗi", result?.Message ?? "Cập nhật thất bại.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi", $"Lỗi kết nối: {ex.Message}", "OK");
            }
            finally
            {
                UpdateButton.IsEnabled = true;
                UpdateButton.Text = "Cập nhật nhân viên";
            }
        }
        private async Task HandleOtpFlow(string email)
        {
            bool verified = false;
            while (!verified)
            {
                string otp = await DisplayPromptAsync(
                    "Xác thực Email Mới",
                    $"Mã OTP đã được gửi tới {email}.\nVui lòng nhập mã để hoàn tất:",
                    "Xác nhận", "Hủy",
                    placeholder: "Nhập 6 số OTP",
                    keyboard: Keyboard.Numeric,
                    maxLength: 6);
                if (string.IsNullOrWhiteSpace(otp))
                {
                    await DisplayAlert("Thông báo", "Bạn đã hủy xác thực. Email chưa được cập nhật.", "OK");
                    return;
                }
                if (otp.Trim().ToLower() == "resend")
                {
                    await ResendOtp(email);
                    continue;
                }
                var verifyReq = new VerifyOtpDto { Email = email, OTP = otp };
                var response = await _httpClient.PostAsJsonAsync(ApiConfig.VerifyEmailOtp, verifyReq);
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(_jsonOptions);
                if (result != null && result.Success)
                {
                    await DisplayAlert("Thành công", "Email đã được xác thực!", "OK");
                    verified = true;
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert("Lỗi", result?.Message ?? "OTP sai.", "OK");
                }
            }
        }
        private async Task ResendOtp(string email)
        {
            var req = new EmailDto { Email = email };
            var response = await _httpClient.PostAsJsonAsync(ApiConfig.ResendEmailOtp, req);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(_jsonOptions);

            if (result != null && result.Success)
                await DisplayAlert("Đã gửi lại", result.Message, "OK");
            else
                await DisplayAlert("Lỗi", result?.Message ?? "Không thể gửi lại OTP", "OK");
        }
        private HttpClientHandler GetInsecureHandler()
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, errors) => true;
            return handler;
        }
    }
}