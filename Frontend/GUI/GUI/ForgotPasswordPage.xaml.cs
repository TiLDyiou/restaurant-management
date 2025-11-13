using System.Net.Http.Json;
using RestaurantManagementGUI.Helpers;

namespace RestaurantManagementGUI
{
    public partial class ForgotPasswordPage : ContentPage
    {
        private readonly HttpClient _httpClient;
        private string _email;
        private string _otp;

        public ForgotPasswordPage()
        {
            InitializeComponent();

#if DEBUG
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) =>
                    msg?.RequestUri?.Host == "10.0.2.2" || msg?.RequestUri?.IsLoopback == true
            };
            _httpClient = new HttpClient(handler);
#else
            _httpClient = new HttpClient();
#endif
            _httpClient.BaseAddress = new Uri(ApiConfig.BaseUrl);
        }

        // Gửi OTP
        private async void OnSendOtpClicked(object sender, EventArgs e)
        {
            _email = EmailEntry.Text?.Trim();
            if (string.IsNullOrWhiteSpace(_email))
            {
                await DisplayAlert("Lỗi", "Vui lòng nhập email.", "OK");
                return;
            }
            SendOtpButton.IsEnabled = false;
            SendOtpButton.Text = "Đang gửi mã...";
            try
            {
                var response = await _httpClient.PostAsJsonAsync(ApiConfig.SendForgotOtp, new { email = _email });
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlert("Thành công", "OTP đã được gửi đến email.", "OK");
                    Step1Layout.IsVisible = false;
                    Step2VerifyLayout.IsVisible = true;
                }
                else
                {
                    await DisplayAlert("Lỗi", $"Gửi OTP thất bại: {result}", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi kết nối", $"Không thể kết nối: {ex.Message}", "OK");
            }
        }

        // Xác minh OTP
        private async void OnVerifyOtpClicked(object sender, EventArgs e)
        {
            _otp = OtpEntry.Text?.Trim();
            if (string.IsNullOrWhiteSpace(_otp))
            {
                await DisplayAlert("Lỗi", "Vui lòng nhập mã OTP.", "OK");
                return;
            }
            VerifyOtpButton.IsEnabled = false;
            VerifyOtpButton.Text = "Đang xác minh...";
            await Task.Delay(500);
            try
            {
                var response = await _httpClient.PostAsJsonAsync(ApiConfig.VerifyForgotOtp, new { email = _email, otp = _otp });
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlert("Thành công", "OTP hợp lệ. Hãy nhập mật khẩu mới.", "OK");
                    Step2VerifyLayout.IsVisible = false;
                    Step3ResetLayout.IsVisible = true;
                }
                else
                {
                    await DisplayAlert("Lỗi", $"OTP không hợp lệ hoặc đã hết hạn: {result}", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi kết nối", $"Không thể xác minh OTP: {ex.Message}", "OK");
            }
        }

        // Đổi mật khẩu
        private async void OnResetPasswordClicked(object sender, EventArgs e)
        {
            string newPassword = NewPasswordEntry.Text?.Trim();
            string confirmPassword = ConfirmPasswordEntry.Text?.Trim();

            if (string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                await DisplayAlert("Lỗi", "Vui lòng nhập đầy đủ mật khẩu mới.", "OK");
                return;
            }

            if (newPassword != confirmPassword)
            {
                await DisplayAlert("Lỗi", "Mật khẩu xác nhận không trùng khớp.", "OK");
                return;
            }
            ResetPasswordButton.IsEnabled = false;
            ResetPasswordButton.Text = "Đang đặt lại mật khẩu...";
            try
            {
                var response = await _httpClient.PostAsJsonAsync(ApiConfig.ResetPassword, new
                {
                    email = _email,
                    otp = _otp,
                    newPassword
                });
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlert("Thành công", "Đặt mật khẩu mới thành công!", "OK");
                    await Navigation.PopToRootAsync();
                }
                else
                {
                    await DisplayAlert("Lỗi", $"Đặt mật khẩu thất bại: {result}", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi kết nối", $"Không thể kết nối: {ex.Message}", "OK");
            }
        }
    }
}
