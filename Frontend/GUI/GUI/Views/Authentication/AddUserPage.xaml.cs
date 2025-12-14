using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Maui.Storage;
using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Models;

namespace RestaurantManagementGUI
{
    public partial class AddUserPage : ContentPage
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public AddUserPage()
        {
            InitializeComponent();

#if DEBUG
            _httpClient = new HttpClient(GetInsecureHandler());
#else
            _httpClient = new HttpClient();
#endif
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        private async void OnAddUserClicked(object sender, EventArgs e)
        {
            var newUser = new AddUserRequestModel
            {
                HoTen = HoTenEntry.Text?.Trim(),
                TenDangNhap = UsernameEntry.Text?.Trim(),
                MatKhau = PasswordEntry.Text?.Trim(),
                SDT = SDTEntry.Text?.Trim(),
                Email = EmailEntry.Text?.Trim(),
                ChucVu = ChucVuEntry.Text?.Trim(),
                Quyen = QuyenPicker.SelectedItem?.ToString() ?? "NhanVien"
            };

            if (!ValidateInput(newUser)) return;

            AddButton.IsEnabled = false;
            AddButton.Text = "Đang thêm nhân viên...";

            try
            {
                var token = await SecureStorage.Default.GetAsync("auth_token");
                if (!string.IsNullOrEmpty(token))
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.PostAsJsonAsync(ApiConfig.Register, newUser);

                var result = await response.Content.ReadFromJsonAsync<ApiResponse<RegisterResponse>>(_jsonOptions);

                if (response.IsSuccessStatusCode && result != null && result.Success)
                {
                    await HandleOtpFlow(newUser.Email, newUser.HoTen, result.Data?.MaNV);
                }
                else
                {
                    await DisplayAlert("Thất bại", result?.Message ?? "Không thể thêm nhân viên.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi", $"Lỗi hệ thống: {ex.Message}", "OK");
            }
            finally
            {
                AddButton.IsEnabled = true;
                AddButton.Text = "➕ Thêm nhân viên";
            }
        }

        private async Task HandleOtpFlow(string email, string hoTen, string maNV)
        {
            bool verified = false;
            while (!verified)
            {
                string otp = await DisplayPromptAsync(
                    "Xác thực Email",
                    $"Mã OTP đã được gửi tới {email}.\nNhập OTP (hoặc gõ 'Resend' để gửi lại):",
                    "Xác nhận", "Hủy",
                    placeholder: "Nhập 6 số OTP",
                    maxLength: 6);

                if (string.IsNullOrWhiteSpace(otp))
                {
                    await DisplayAlert("Cảnh báo", "Bạn đã hủy xác thực. Tài khoản đã tạo nhưng chưa được kích hoạt.", "OK");
                    return;
                }

                if (otp.Trim().Equals("Resend", StringComparison.OrdinalIgnoreCase))
                {
                    await ResendOtp(email);
                    continue;
                }

                var verifyResponse = await _httpClient.PostAsJsonAsync(ApiConfig.VerifyRegisterOtp, new { Email = email, OTP = otp });
                var verifyResult = await verifyResponse.Content.ReadFromJsonAsync<ApiResponse<object>>(_jsonOptions);

                if (verifyResult != null && verifyResult.Success)
                {
                    await DisplayAlert("Thành công", $"Nhân viên {hoTen} ({maNV}) đã được thêm và kích hoạt thành công!", "OK");
                    verified = true;
                    ClearForm();
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert("Lỗi", verifyResult?.Message ?? "OTP không đúng", "OK");
                }
            }
        }

        private async Task ResendOtp(string email)
        {
            var response = await _httpClient.PostAsJsonAsync(ApiConfig.SendRegisterOtp, new { Email = email });
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(_jsonOptions);

            if (result != null && result.Success)
                await DisplayAlert("Thông báo", result.Message, "OK");
            else
                await DisplayAlert("Lỗi", result?.Message ?? "Gửi lại thất bại", "OK");
        }

        private bool ValidateInput(AddUserRequestModel user)
        {
            if (string.IsNullOrWhiteSpace(user.HoTen) || string.IsNullOrWhiteSpace(user.TenDangNhap) ||
                string.IsNullOrWhiteSpace(user.MatKhau) || string.IsNullOrWhiteSpace(user.SDT) ||
                string.IsNullOrWhiteSpace(user.Email) || string.IsNullOrWhiteSpace(user.ChucVu))
            {
                DisplayAlert("Thiếu thông tin", "Vui lòng nhập đầy đủ thông tin!", "OK");
                return false;
            }
            if (PasswordEntry.Text != ConfirmPassword.Text)
            {
                DisplayAlert("Lỗi", "Mật khẩu xác nhận không khớp!", "OK");
                return false;
            }

            if (!Regex.IsMatch(user.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                DisplayAlert("Lỗi", "Email không hợp lệ.", "OK");
                return false;
            }
            return true;
        }

        private void ClearForm()
        {
            HoTenEntry.Text = UsernameEntry.Text = PasswordEntry.Text = ConfirmPassword.Text = "";
            SDTEntry.Text = EmailEntry.Text = ChucVuEntry.Text = "";
            QuyenPicker.SelectedIndex = -1;
        }

        private HttpClientHandler GetInsecureHandler()
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, errors) => true;
            return handler;
        }
    }
}