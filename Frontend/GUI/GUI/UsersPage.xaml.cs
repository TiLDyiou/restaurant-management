using RestaurantManagementGUI.Models;
using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace RestaurantManagementGUI;

public partial class UsersPage : ContentPage
{
    private readonly HttpClient _httpClient;
    private ObservableCollection<UserModel> _users = new();

    public UsersPage()
    {
        InitializeComponent();

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://localhost:7004/") 
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadUsersAsync();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
    private async Task LoadUsersAsync()
    {
        try
        {

            var token = await SecureStorage.Default.GetAsync("auth_token");
            if (string.IsNullOrEmpty(token))
            {
                await DisplayAlert("Lỗi", "Chưa đăng nhập hoặc token hết hạn!", "OK");
                return;
            }

            if (token != null)
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync("api/Auth/users");

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<List<UserModel>>();
                _users = new ObservableCollection<UserModel>(data ?? new());
                UsersCollectionView.ItemsSource = _users;
            }
            else
            {
                var msg = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Lỗi", $"Không thể tải danh sách nhân viên: {response.StatusCode}\n{msg}", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", ex.Message, "OK");
        }
    }

    private async void OnAddUserClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new AddUserPage());
    }

    private async void OnEditClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is UserModel user)
        {
            await Navigation.PushAsync(new EditUserPage(user));
        }
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is UserModel user)
        {
            bool confirm = await DisplayAlert("Xác nhận", $"Bạn có chắc muốn xóa {user.HoTen}?", "Có", "Không");
            if (!confirm) return;

            var token = Preferences.Get("AccessToken", null);
            if (token != null)
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.DeleteAsync($"api/Auth/{user.MaNV}");
            if (response.IsSuccessStatusCode)
            {
                _users.Remove(user);
                await DisplayAlert("Thành công", "Xóa nhân viên thành công", "OK");
            }
            else
            {
                var msg = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Lỗi", $"Không thể xóa nhân viên: {msg}", "OK");
            }
        }
    }
}
