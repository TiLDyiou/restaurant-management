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
            string email = EmailEntry.Text?.Trim();
            string password = MatKhauEntry.Text?.Trim();
            string confirmPassword = ConfirmPasswordEntry.Text?.Trim();

            UpdateButton.IsEnabled = false;
            UpdateButton.Text = "Đang cập nhật thông tin...";

            try
            {
                // kiểm tra mật khẩu
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

                // Lấy token
                var token = await SecureStorage.Default.GetAsync("auth_token");
                if (!string.IsNullOrEmpty(token))
                {
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }

                var updateUser = new UpdateUserRequestModel
                {
                    HoTen = HoTenEntry.Text?.Trim(),
                    ChucVu = ChucVuEntry.Text?.Trim(),
                    SDT = SDTEntry.Text?.Trim(),
                    Email = email,
                    MatKhau = password,
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

                // Nếu cần OTP
                if (result != null && !result.IsVerified)
                {
                    bool verified = false;

                    while (!verified)
                    {
                        string otp = await DisplayPromptAsync(
                            "Xác thực Email",
                            $"Một mã OTP đã được gửi tới {email}.\nNhập OTP (hoặc 'Resend' để gửi lại):",
                            "Xác nhận",
                            "Hủy",
                            placeholder: "Nhập OTP",
                            keyboard: Keyboard.Numeric,
                            maxLength: 6
                        );

                        if (string.IsNullOrWhiteSpace(otp))
                        {
                            await DisplayAlert("Thông báo", "Bạn đã hủy xác thực Email.", "OK");
                            return;
                        }

                        if (otp.Equals("resend", StringComparison.OrdinalIgnoreCase))
                        {
                            var resend = await _httpClient.PostAsJsonAsync(ApiConfig.ResendEmailOtp, new { Email = email });
                            if (!resend.IsSuccessStatusCode)
                            {
                                var msg = await resend.Content.ReadAsStringAsync();
                                await DisplayAlert("Lỗi", $"Gửi lại OTP thất bại:\n{msg}", "OK");
                            }
                            else
                            {
                                await DisplayAlert("Thành công", "Đã gửi lại OTP tới email!", "OK");
                            }
                            continue;
                        }

                        var verify = await _httpClient.PostAsJsonAsync(ApiConfig.VerifyEmailOtp,
                            new { Email = email, OTP = otp });

                        if (!verify.IsSuccessStatusCode)
                        {
                            var msg = await verify.Content.ReadAsStringAsync();
                            await DisplayAlert("Lỗi", $"OTP không hợp lệ:\n{msg}", "OK");
                        }
                        else
                        {
                            await DisplayAlert("Thành công", "Email đã được xác thực!", "OK");
                            verified = true;
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
                UpdateButton.Text = "Cập nhật nhân viên";
            }
        }
    }
}