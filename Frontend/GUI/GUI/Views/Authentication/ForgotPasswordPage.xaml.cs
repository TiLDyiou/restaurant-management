using System.Net.Http.Json;
using System.Text.Json;
using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Models;

namespace RestaurantManagementGUI
{
    public partial class ForgotPasswordPage : ContentPage
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        private string _email;
        private string _otp;

        public ForgotPasswordPage()
        {
            InitializeComponent();

#if DEBUG
            _httpClient = new HttpClient(GetInsecureHandler());
#else
            _httpClient = new HttpClient();
#endif
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }
        private async void OnSendOtpClicked(object sender, EventArgs e)
        {
            _email = EmailEntry.Text?.Trim();
            if (string.IsNullOrWhiteSpace(_email))
            {
                await DisplayAlert("Lỗi", "Vui lòng nhập email.", "OK");
                return;
            }

            SetLoading(true, SendOtpButton, "Đang gửi...");

            try
            {
                var req = new EmailDto { Email = _email };
                var response = await _httpClient.PostAsJsonAsync(ApiConfig.ForgotPassword, req);
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(_jsonOptions);

                if (result != null && result.Success)
                {
                    await DisplayAlert("Thành công", result.Message, "OK");
                    Step1Layout.IsVisible = false;
                    Step2VerifyLayout.IsVisible = true;
                }
                else
                {
                    await DisplayAlert("Thất bại", result?.Message ?? "Gửi OTP thất bại", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi kết nối", ex.Message, "OK");
            }
            finally
            {
                SetLoading(false, SendOtpButton, "Gửi OTP");
            }
        }
        private async void OnVerifyOtpClicked(object sender, EventArgs e)
        {
            _otp = OtpEntry.Text?.Trim();
            if (string.IsNullOrWhiteSpace(_otp))
            {
                await DisplayAlert("Lỗi", "Vui lòng nhập mã OTP.", "OK");
                return;
            }

            SetLoading(true, VerifyOtpButton, "Đang xác minh...");

            try
            {
                var req = new VerifyOtpDto { Email = _email, OTP = _otp };
                var response = await _httpClient.PostAsJsonAsync(ApiConfig.VerifyForgotOtp, req);
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(_jsonOptions);

                if (result != null && result.Success)
                {
                    await DisplayAlert("Thành công", result.Message, "OK");
                    Step2VerifyLayout.IsVisible = false;
                    Step3ResetLayout.IsVisible = true;
                }
                else
                {
                    await DisplayAlert("Lỗi", result?.Message ?? "Mã OTP không đúng", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi kết nối", ex.Message, "OK");
            }
            finally
            {
                SetLoading(false, VerifyOtpButton, "Xác nhận OTP");
            }
        }

        private async void OnResendOtpClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_email)) return;

            ResendOtpButton.Opacity = 0.5;

            try
            {
                var req = new EmailDto { Email = _email };
                var response = await _httpClient.PostAsJsonAsync(ApiConfig.ForgotPassword, req);
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(_jsonOptions);

                if (result != null && result.Success)
                    await DisplayAlert("Đã gửi lại", result.Message, "OK");
                else
                    await DisplayAlert("Lỗi", result?.Message ?? "Không thể gửi lại", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi", ex.Message, "OK");
            }
            finally
            {
                ResendOtpButton.Opacity = 1.0;
            }
        }
        private async void OnResetPasswordClicked(object sender, EventArgs e)
        {
            string newPassword = NewPasswordEntry.Text?.Trim();
            string confirmPassword = ConfirmPasswordEntry.Text?.Trim();

            if (string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                await DisplayAlert("Lỗi", "Vui lòng nhập mật khẩu mới.", "OK");
                return;
            }

            if (newPassword != confirmPassword)
            {
                await DisplayAlert("Lỗi", "Mật khẩu xác nhận không khớp.", "OK");
                return;
            }

            SetLoading(true, ResetPasswordButton, "Đang xử lý...");

            try
            {
                var req = new ResetPasswordDto
                {
                    Email = _email,
                    OTP = _otp,
                    NewPassword = newPassword
                };

                var response = await _httpClient.PostAsJsonAsync(ApiConfig.ResetPassword, req);
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(_jsonOptions);

                if (result != null && result.Success)
                {
                    await DisplayAlert("Thành công", "Đổi mật khẩu thành công. Vui lòng đăng nhập lại.", "OK");
                    await Navigation.PopToRootAsync();
                }
                else
                {
                    await DisplayAlert("Thất bại", result?.Message ?? "Không thể đổi mật khẩu", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi kết nối", ex.Message, "OK");
            }
            finally
            {
                SetLoading(false, ResetPasswordButton, "Đổi mật khẩu");
            }
        }

        private void SetLoading(bool isLoading, Button btn, string text)
        {
            btn.IsEnabled = !isLoading;
            btn.Text = text;
        }

        private HttpClientHandler GetInsecureHandler()
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, errors) =>
            {
                if (sender is HttpRequestMessage request)
                {
                    return request.RequestUri.IsLoopback ||
                           (DeviceInfo.Platform == DevicePlatform.Android && request.RequestUri.Host == "10.0.2.2");
                }
                return errors == System.Net.Security.SslPolicyErrors.None;
            };
            return handler;
        }
    }
}