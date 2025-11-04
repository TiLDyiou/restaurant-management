using RestaurantManagementGUI.Models;
using System.Net.Http.Json;

namespace RestaurantManagementGUI;

public partial class EditUserPage : ContentPage
{
    private readonly HttpClient _httpClient;
    private readonly string _maNV;

    public EditUserPage(UserModel user)
    {
        InitializeComponent();
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://localhost:7004/")
        };

        _maNV = user.MaNV;
        MaNVLabel.Text = $"Mã nhân viên: {user.MaNV}";
        HoTenEntry.Text = user.HoTen;
        ChucVuEntry.Text = user.ChucVu;
        SDTEntry.Text = user.SDT;
        QuyenPicker.SelectedItem = user.Quyen;
    }

    private async void OnUpdateUserClicked(object sender, EventArgs e)
    {
        var updateUser = new UpdateUserRequestModel
        {
            HoTen = HoTenEntry.Text,
            ChucVu = ChucVuEntry.Text,
            SDT = SDTEntry.Text,
            Quyen = QuyenPicker.SelectedItem?.ToString()
        };

        var response = await _httpClient.PutAsJsonAsync($"api/Auth/admin-update/{_maNV}", updateUser);

        if (response.IsSuccessStatusCode)
        {
            await DisplayAlert("Thành công", "C?p nh?t nhân viên thành công!", "OK");
            await Navigation.PopAsync();
        }
        else
        {
            var content = await response.Content.ReadAsStringAsync();
            await DisplayAlert("L?i", $"Không th? c?p nh?t nhân viên: {content}", "OK");
        }
    }
}
