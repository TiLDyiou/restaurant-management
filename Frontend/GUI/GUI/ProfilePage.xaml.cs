using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace RestaurantManagementGUI;

public partial class ProfilePage : ContentPage
{
    private readonly HttpClient _client;

    public ProfilePage()
    {
        InitializeComponent();

        _client = new HttpClient();
        LoadProfile();
    }

    private async void LoadProfile()
    {
        try
        {
            var token = await SecureStorage.Default.GetAsync("jwt_token");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync("https://localhost:7004/api/Auth/me");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var profile = JsonSerializer.Deserialize<ProfileDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                UsernameEntry.Text = profile.tenDangNhap;
                FullNameEntry.Text = profile.hoTen;
                RoleEntry.Text = profile.chucVu;
                PhoneEntry.Text = profile.sdt;
            }
            else
            {
                await DisplayAlert("L?i", "Không t?i ???c thông tin cá nhân", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("L?i", ex.Message, "OK");
        }
    }

    private async void OnUpdateProfileClicked(object sender, EventArgs e)
    {
        try
        {
            var token = await SecureStorage.Default.GetAsync("jwt_token");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var dto = new
            {
                SDT = PhoneEntry.Text,
                CurrentPassword = CurrentPasswordEntry.Text,
                NewPassword = NewPasswordEntry.Text
            };

            var content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
            var response = await _client.PutAsync("https://localhost:7004/api/Auth/update-profile", content);

            if (response.IsSuccessStatusCode)
            {
                await DisplayAlert("Thành công", "C?p nh?t thông tin thành công", "OK");
                CurrentPasswordEntry.Text = "";
                NewPasswordEntry.Text = "";
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                await DisplayAlert("L?i", error, "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("L?i", ex.Message, "OK");
        }
    }
}

public class ProfileDto
{
    public string tenDangNhap { get; set; }
    public string quyen { get; set; }
    public string maNV { get; set; }
    public string hoTen { get; set; }
    public string chucVu { get; set; }
    public string sdt { get; set; }
}
