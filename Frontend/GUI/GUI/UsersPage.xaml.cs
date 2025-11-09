using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Models;
using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Maui.Storage;

namespace RestaurantManagementGUI
{
    public partial class UsersPage : ContentPage
    {
        private readonly HttpClient _httpClient;
        private ObservableCollection<UserModel> _users = new();
        private List<UserModel> _allUsers = new(); // danh sách gốc để tìm kiếm

        public UsersPage()
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

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadUsersAsync();
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

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.GetAsync(ApiConfig.Users);
                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadFromJsonAsync<List<UserModel>>();
                    _allUsers = data ?? new();
                    _users = new ObservableCollection<UserModel>(_allUsers);
                    UsersCollectionView.ItemsSource = _users;
                }
                else
                {
                    await DisplayAlert("Lỗi", "Không thể tải danh sách người dùng.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi", $"Không thể kết nối: {ex.Message}", "OK");
            }
        }

        // Tìm kiếm theo mã hoặc tên
        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            string keyword = e.NewTextValue?.Trim().ToLower() ?? string.Empty;

            if (string.IsNullOrEmpty(keyword))
            {
                _users = new ObservableCollection<UserModel>(_allUsers);
            }
            else
            {
                var filtered = _allUsers
                    .Where(u =>
                        (!string.IsNullOrEmpty(u.HoTen) && u.HoTen.ToLower().Contains(keyword)) ||
                        (!string.IsNullOrEmpty(u.MaNV) && u.MaNV.ToLower().Contains(keyword)))
                    .ToList();

                _users = new ObservableCollection<UserModel>(filtered);
            }

            UsersCollectionView.ItemsSource = _users;
        }

        private async void OnAddUserClicked(object sender, EventArgs e)
            => await Navigation.PushAsync(new AddUserPage());

        private async void OnEditClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.BindingContext is UserModel user)
                await Navigation.PushAsync(new EditUserPage(user));
        }

        private async void OnToggleStatusClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.BindingContext is UserModel user)
            {
                bool confirm = await DisplayAlert("Xác nhận",
                    $"Bạn có chắc muốn cho {user.HoTen} {(user.TrangThai == "Đang làm" ? "nghỉ việc" : "quay lại làm việc")}?",
                    "Có", "Không");
                if (!confirm) return;

                var token = await SecureStorage.Default.GetAsync("auth_token");
                if (!string.IsNullOrEmpty(token))
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.PutAsync(ApiConfig.SoftDeleteUser(user.MaNV), null);
                if (response.IsSuccessStatusCode)
                {
                    var updated = await response.Content.ReadFromJsonAsync<UserModel>();
                    if (updated != null)
                    {
                        user.TrangThai = updated.TrangThai;
                        UsersCollectionView.ItemsSource = null;
                        UsersCollectionView.ItemsSource = _users;
                    }
                }
                else
                {
                    await DisplayAlert("Lỗi", "Cập nhật trạng thái thất bại!", "OK");
                }
            }
        }

        private async void OnHardDeleteClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.BindingContext is UserModel user)
            {
                bool confirm = await DisplayAlert("Xác nhận", $"Xóa vĩnh viễn {user.HoTen}?", "Có", "Không");
                if (!confirm) return;

                var token = await SecureStorage.Default.GetAsync("auth_token");
                if (!string.IsNullOrEmpty(token))
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.DeleteAsync(ApiConfig.HardDeleteUser(user.MaNV));
                if (response.IsSuccessStatusCode)
                {
                    _users.Remove(user);
                    _allUsers.Remove(user);
                    await DisplayAlert("Thành công", "Nhân viên đã bị xóa vĩnh viễn", "OK");
                }
                else
                {
                    await DisplayAlert("Lỗi", "Không thể xóa người dùng.", "OK");
                }
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
            => await Navigation.PopAsync();
    }
}
