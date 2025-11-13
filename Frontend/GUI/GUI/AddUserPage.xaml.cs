using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Maui.Storage;
using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Models;

namespace RestaurantManagementGUI
{
    public partial class AddUserPage : ContentPage
    {
        private readonly HttpClient _httpClient;

        public AddUserPage()
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

        private async void OnAddUserClicked(object sender, EventArgs e)
        {
            try
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

                if (string.IsNullOrWhiteSpace(newUser.HoTen) ||
                    string.IsNullOrWhiteSpace(newUser.TenDangNhap) ||
                    string.IsNullOrWhiteSpace(newUser.MatKhau) ||
                    string.IsNullOrWhiteSpace(newUser.SDT) ||
                    string.IsNullOrWhiteSpace(newUser.Email) ||
                    string.IsNullOrWhiteSpace(newUser.ChucVu))
                {
                    await DisplayAlert("Thiếu thông tin", "Vui lòng nhập đầy đủ thông tin!", "OK");
                    return;
                }

                var token = await SecureStorage.Default.GetAsync("auth_token");
                if (!string.IsNullOrEmpty(token))
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                // 🔹 Tạo user
                var response = await _httpClient.PostAsJsonAsync(ApiConfig.Register, newUser);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Lỗi", $"Không thể thêm nhân viên!\n{error}", "OK");
                    return;
                }

                var json = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<RegisterResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // 🔹 Prompt nhập OTP và hỗ trợ gửi lại
                bool verified = false;
                while (!verified)
                {
                    string otp = await DisplayPromptAsync(
                        "Xác thực Email",
                        $"Một mã OTP đã được gửi tới {newUser.Email}.\nNhập OTP hoặc gõ 'Resend' để gửi lại:",
                        "Xác nhận",
                        "Hủy",
                        placeholder: "Nhập OTP",
                        keyboard: Keyboard.Text,
                        maxLength: 6);

                    if (string.IsNullOrWhiteSpace(otp))
                    {
                        await DisplayAlert("Thông báo", "Bạn đã hủy xác thực Email.", "OK");
                        return;
                    }

                    if (otp.Equals("Resend", StringComparison.OrdinalIgnoreCase))
                    {
                        // Gọi API resend-email-otp
                        var resendResp = await _httpClient.PostAsJsonAsync(ApiConfig.ResendEmailOtp, new { Email = newUser.Email });
                        if (!resendResp.IsSuccessStatusCode)
                        {
                            var msg = await resendResp.Content.ReadAsStringAsync();
                            await DisplayAlert("Lỗi", $"Gửi lại OTP thất bại:\n{msg}", "OK");
                            return;
                        }
                        continue; // prompt lại
                    }

                    // Verify OTP
                    var verifyResponse = await _httpClient.PostAsJsonAsync(ApiConfig.VerifyRegisterOtp,
                        new { Email = newUser.Email, OTP = otp });
                    if (verifyResponse.IsSuccessStatusCode)
                    {
                        await DisplayAlert("Thành công",
                            $"Nhân viên {newUser.HoTen} đã được tạo và xác thực!\nMã NV: {data?.MaNV ?? "(chưa rõ)"}",
                            "OK");
                        verified = true;

                        // Xóa form
                        HoTenEntry.Text = UsernameEntry.Text = PasswordEntry.Text = SDTEntry.Text = EmailEntry.Text = ChucVuEntry.Text = "";
                        QuyenPicker.SelectedIndex = -1;
                    }
                    else
                    {
                        var err = await verifyResponse.Content.ReadAsStringAsync();
                        await DisplayAlert("Lỗi", $"OTP không hợp lệ: {err}", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi", $"Đã xảy ra lỗi: {ex.Message}", "OK");
            }
        }
    }
}
