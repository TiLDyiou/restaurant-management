using System.Net.Http.Json;
using Microsoft.Maui.Storage;
using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Models;

namespace RestaurantManagementGUI
{
    public partial class EditUserPage : ContentPage
    {
        private readonly HttpClient _httpClient;
        private readonly string _maNV;

        public EditUserPage(UserModel user)
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
            _maNV = user.MaNV;

            MaNVLabel.Text = $"Mã nhân viên: {user.MaNV}";
            HoTenEntry.Text = user.HoTen;
            ChucVuEntry.Text = user.ChucVu;
            SDTEntry.Text = user.SDT;
            EmailEntry.Text = user.Email;
            QuyenPicker.SelectedItem = user.Quyen ?? "NhanVien";
        }

        private async void OnUpdateUserClicked(object sender, EventArgs e)
        {
            var email = EmailEntry.Text?.Trim();

            try
            {
                var token = await SecureStorage.Default.GetAsync("auth_token");
                if (!string.IsNullOrEmpty(token))
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                // 🔹 UpdateUser (Admin) 
                var updateUser = new UpdateUserRequestModel
                {
                    HoTen = HoTenEntry.Text?.Trim(),
                    ChucVu = ChucVuEntry.Text?.Trim(),
                    SDT = SDTEntry.Text?.Trim(),
                    Email = email,
                    MatKhau = MatKhauEntry.Text?.Trim(),
                    Quyen = QuyenPicker.SelectedItem?.ToString()?.Trim()
                };

                var response = await _httpClient.PutAsJsonAsync(ApiConfig.UpdateUser(_maNV), updateUser);

                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Lỗi", $"Không thể cập nhật:\n{err}", "OK");
                    return;
                }

                var result = await response.Content.ReadFromJsonAsync<UpdateUserResponse>();

                // 🔹 Nếu email chưa xác thực → prompt nhập OTP
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
                            // Gọi API resend-email-otp
                            var resendResp = await _httpClient.PostAsJsonAsync(ApiConfig.ResendEmailOtp, new { Email = email });
                            if (!resendResp.IsSuccessStatusCode)
                            {
                                var msg = await resendResp.Content.ReadAsStringAsync();
                                await DisplayAlert("Lỗi", $"Gửi lại OTP thất bại:\n{msg}", "OK");
                                return;
                            }
                            continue; // prompt lại
                        }

                        // Verify OTP
                        var verifyResponse = await _httpClient.PostAsJsonAsync(ApiConfig.VerifyEmailOtp, new { Email = email, OTP = otp });
                        if (verifyResponse.IsSuccessStatusCode)
                        {
                            await DisplayAlert("Thành công", "Email đã được xác thực!", "OK");
                            verified = true;
                        }
                        else
                        {
                            var err = await verifyResponse.Content.ReadAsStringAsync();
                            await DisplayAlert("Lỗi", $"OTP không hợp lệ: {err}", "OK");
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
        }
    }
}
