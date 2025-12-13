using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Maui.Storage;
using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Models;
using System.Net.Http.Headers;

namespace RestaurantManagementGUI
{
    public partial class EditChefAndUserProfilePage : ContentPage
    {
        private readonly HttpClient _httpClient;
        private UserModel _user;

        public EditChefAndUserProfilePage(UserModel user)
        {
            InitializeComponent();
            _user = user;

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

            // Load data
            MaNVLabel.Text = _user.MaNV;
            HoTenEntry.Text = _user.HoTen;
            ChucVuEntry.Text = _user.ChucVu;
            SDTEntry.Text = _user.SDT;
            EmailEntry.Text = _user.Email;
        }

        private async void OnUpdateUserClicked(object sender, EventArgs e)
        {
            string email = EmailEntry.Text?.Trim();
            string password = MatKhauEntry.Text?.Trim();
            string confirmPassword = ConfirmPasswordEntry.Text?.Trim();

            UpdateButton.IsEnabled = false;
            UpdateButton.Text = "Đang cập nhật...";

            try
            {
                if (!string.IsNullOrEmpty(password))
                {
                    if (string.IsNullOrEmpty(confirmPassword))
                    {
                        await DisplayAlert("Lỗi", "Vui lòng nhập xác nhận mật khẩu.", "OK");
                        return;
                    }

                    if (password != confirmPassword)
                    {
                        await DisplayAlert("Lỗi", "Mật khẩu và xác nhận mật khẩu không khớp.", "OK");
                        return;
                    }
                }

                var token = await SecureStorage.Default.GetAsync("auth_token");
                if (!string.IsNullOrEmpty(token))
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);

                var updateUser = new UpdateUserRequestModel
                {
                    HoTen = HoTenEntry.Text?.Trim(),
                    ChucVu = ChucVuEntry.Text?.Trim(),
                    SDT = SDTEntry.Text?.Trim(),
                    Email = email,
                    MatKhau = password 
                };

                var response = await _httpClient.PutAsJsonAsync(ApiConfig.UserById(_user.MaNV), updateUser);

                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Lỗi", $"Cập nhật thất bại:\n{err}", "OK");
                    return;
                }

                var result = await response.Content.ReadFromJsonAsync<UpdateUserResponse>();

                // gửi OTP
                if (result != null && !result.IsVerified)
                {
                    bool verified = false;

                    while (!verified)
                    {
                        string otp = await DisplayPromptAsync(
                            "Xác thực Email",
                            $"Một mã OTP đã được gửi tới {email}.\nNhập OTP (hoặc gõ 'Resend' để gửi lại):",
                            "Xác nhận",
                            "Hủy",
                            placeholder: "Nhập OTP",
                            keyboard: Keyboard.Numeric,
                            maxLength: 6);

                        if (string.IsNullOrWhiteSpace(otp))
                        {
                            await DisplayAlert("Thông báo", "Bạn đã hủy xác thực Email.", "OK");
                            return;
                        }

                        if (otp.Equals("Resend", StringComparison.OrdinalIgnoreCase))
                        {
                            var resendResp = await _httpClient.PostAsJsonAsync(
                                ApiConfig.ResendEmailOtp, new { Email = email });

                            if (!resendResp.IsSuccessStatusCode)
                            {
                                var msg = await resendResp.Content.ReadAsStringAsync();
                                await DisplayAlert("Lỗi", $"Gửi lại OTP thất bại:\n{msg}", "OK");
                                return;
                            }

                            continue;
                        }

                        var verifyResp = await _httpClient.PostAsJsonAsync(
                            ApiConfig.VerifyEmailOtp, new { Email = email, OTP = otp });

                        if (verifyResp.IsSuccessStatusCode)
                        {
                            await DisplayAlert("Thành công", "Email đã được xác thực!", "OK");
                            verified = true;
                        }
                        else
                        {
                            var errMsg = await verifyResp.Content.ReadAsStringAsync();
                            await DisplayAlert("Lỗi", $"OTP không hợp lệ: {errMsg}", "OK");
                        }
                    }
                }

                await DisplayAlert("Thành công", "Cập nhật thông tin thành công!", "OK");
                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi", $"Không thể kết nối đến máy chủ:\n{ex.Message}", "OK");
            }
            finally
            {
                UpdateButton.IsEnabled = true;
                UpdateButton.Text = "Cập nhật thông tin";
            }
        }
    }
}
