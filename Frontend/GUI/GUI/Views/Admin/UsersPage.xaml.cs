using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Models;
using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Maui.Storage;

namespace RestaurantManagementGUI
{
    public partial class UsersPage : ContentPage
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        private ObservableCollection<UserModel> _users = new();
        private List<UserModel> _allUsers = new();

        public UsersPage()
        {
            InitializeComponent();

#if DEBUG
            _httpClient = new HttpClient(GetInsecureHandler());
#else
            _httpClient = new HttpClient();
#endif
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadUsersAsync();

            MessagingCenter.Subscribe<Services.TCPSocketClient, string>(this, "UpdateStatus", (sender, message) =>
            {
                // Format: STATUS|NV001|TRUE
                var parts = message.Split('|');
                if (parts.Length == 3)
                {
                    string targetNV = parts[1];
                    bool isOnline = parts[2] == "TRUE";

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        var user = _users.FirstOrDefault(u => u.MaNV == targetNV);
                        if (user != null)
                        {
                            user.Online = isOnline;
                            int index = _users.IndexOf(user);
                            if (index >= 0)
                            {
                                _users[index] = user;
                            }
                        }
                    });
                }
            });
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            MessagingCenter.Unsubscribe<Services.TCPSocketClient, string>(this, "UpdateStatus");
        }


        private async Task LoadUsersAsync()
        {
            try
            {
                var token = await SecureStorage.Default.GetAsync("auth_token");
                if (string.IsNullOrEmpty(token)) return;

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // GỌI API GET ALL USERS
                // Backend trả về: { success: true, data: [User1, User2...] }
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<UserModel>>>(ApiConfig.GetAllUsers, _jsonOptions);

                if (response != null && response.Success)
                {
                    _allUsers = response.Data ?? new List<UserModel>();
                    _users = new ObservableCollection<UserModel>(_allUsers);
                    UsersCollectionView.ItemsSource = _users;
                }
                else
                {
                    await DisplayAlert("Lỗi", response?.Message ?? "Không thể tải danh sách.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi kết nối", ex.Message, "OK");
            }
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            // Lấy từ khóa, xóa khoảng trắng thừa, chuyển thường và BỎ DẤU ngay lập tức
            string originKeyword = e.NewTextValue?.Trim() ?? "";
            string keyword = RemoveSign4VietnameseString(originKeyword).ToLower();

            if (string.IsNullOrEmpty(keyword))
            {
                // Nếu ô tìm kiếm rỗng, hiển thị lại toàn bộ danh sách gốc
                _users = new ObservableCollection<UserModel>(_allUsers);
            }
            else
            {
                // Lọc danh sách
                var filtered = _allUsers.Where(u =>
                {
                    // Chuẩn hóa tên và mã của nhân viên về dạng KHÔNG DẤU + CHỮ THƯỜNG
                    string tenKhongDau = RemoveSign4VietnameseString(u.HoTen ?? "").ToLower();
                    string maKhongDau = (u.MaNV ?? "").ToLower();

                    // So sánh: Chỉ cần chứa từ khóa là được
                    return tenKhongDau.Contains(keyword) || maKhongDau.Contains(keyword);
                }).ToList();

                _users = new ObservableCollection<UserModel>(filtered);
            }

            // Cập nhật lên giao diện
            UsersCollectionView.ItemsSource = _users;
        }

        private static string RemoveSign4VietnameseString(string str)
        {
            if (string.IsNullOrEmpty(str)) return str;
            string[] VietnameseSigns = new string[]
            {
                "aAeEoOuUiIdDyY", "áàạảãâấầậẩẫăắằặẳẵ", "ÁÀẠẢÃÂẤẦẬẨẪĂẮẰẶẲẴ", "éèẹẻẽêếềệểễ", "ÉÈẸẺẼÊẾỀỆỂỄ",
                "óòọỏõôốồộổỗơớờợởỡ", "ÓÒỌỎÕÔỐỒỘỔỖƠỚỜỢỞỠ", "úùụủũưứừựửữ", "ÚÙỤỦŨƯỨỪỰỬỮ", "íìịỉĩ", "ÍÌỊỈĨ",
                "đ", "Đ", "ýỳỵỷỹ", "ÝỲỴỶỸ"
            };
            for (int i = 1; i < VietnameseSigns.Length; i++)
            {
                for (int j = 0; j < VietnameseSigns[i].Length; j++)
                    str = str.Replace(VietnameseSigns[i][j], VietnameseSigns[0][i - 1]);
            }
            return str;
        }

        private async void OnAddUserClicked(object sender, EventArgs e) => await Navigation.PushAsync(new AddUserPage());

        private async void OnEditClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.BindingContext is UserModel user)
                await Navigation.PushAsync(new EditUserPage(user));
        }
        private async void OnToggleStatusClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.BindingContext is UserModel user)
            {
                bool isWorking = user.TrangThai == "Đang làm";
                bool confirm = await DisplayAlert("Xác nhận",
                    $"Bạn có muốn {(isWorking ? "cho nghỉ việc" : "khôi phục")} nhân viên {user.HoTen}?", "Có", "Không");

                if (!confirm) return;

                try
                {
                    // Gọi API PUT: api/users/{id}/status
                    var url = ApiConfig.ToggleUserStatus(user.MaNV);
                    var response = await _httpClient.PutAsync(url, null); // Body null vì ID đã ở trên URL
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(_jsonOptions);

                    if (result != null && result.Success)
                    {
                        // Cập nhật UI ngay lập tức (Optimistic UI)
                        user.TrangThai = isWorking ? "Đã nghỉ" : "Đang làm";
                        var index = _users.IndexOf(user);
                        if (index >= 0)
                        {
                            _users[index] = user;
                        }
                    }
                    else
                    {
                        await DisplayAlert("Lỗi", result?.Message ?? "Thất bại", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Lỗi", ex.Message, "OK");
                }
            }
        }
        private async void OnHardDeleteClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.BindingContext is UserModel user)
            {
                bool confirm = await DisplayAlert("Cảnh báo", $"Xóa vĩnh viễn {user.HoTen}? Hành động này không thể hoàn tác!", "Xóa", "Hủy");
                if (!confirm) return;

                try
                {
                    // Gọi API DELETE: api/users/{id}
                    var url = ApiConfig.HardDeleteUser(user.MaNV);
                    var response = await _httpClient.DeleteAsync(url);
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(_jsonOptions);

                    if (result != null && result.Success)
                    {
                        _users.Remove(user);
                        _allUsers.Remove(user);
                        await DisplayAlert("Thành công", "Đã xóa nhân viên.", "OK");
                    }
                    else
                    {
                        await DisplayAlert("Lỗi", result?.Message ?? "Không thể xóa.", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Lỗi", ex.Message, "OK");
                }
            }
        }

        private HttpClientHandler GetInsecureHandler()
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, errors) => true;
            return handler;
        }
    }
}