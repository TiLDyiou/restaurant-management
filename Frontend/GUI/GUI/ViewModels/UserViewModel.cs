using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Models;
using RestaurantManagementGUI.Services;
using RestaurantManagementGUI.Views; // Để điều hướng Add/Edit

namespace RestaurantManagementGUI.ViewModels
{
    public partial class UserViewModel : BaseViewModel
    {
        private readonly ApiService _apiService;
        private List<UserModel> _allUsers = new();

        public ObservableCollection<UserModel> Users { get; } = new();

        [ObservableProperty]
        private string _searchText;

        partial void OnSearchTextChanged(string value) => FilterUsers();

        public UserViewModel(ApiService apiService)
        {
            _apiService = apiService;
        }

        [RelayCommand]
        public async Task LoadUsersAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            var response = await _apiService.GetAsync<List<UserModel>>(ApiConfig.Users);
            if (response.Success && response.Data != null)
            {
                _allUsers = response.Data;
                FilterUsers();
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Lỗi", response.Message, "OK");
            }

            IsBusy = false;
        }

        private void FilterUsers()
        {
            var query = _allUsers.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var lower = SearchText.ToLower();
                query = query.Where(u =>
                    (u.HoTen?.ToLower().Contains(lower) ?? false) ||
                    (u.MaNV?.ToLower().Contains(lower) ?? false));
            }

            Users.Clear();
            foreach (var u in query) Users.Add(u);
        }

        [RelayCommand]
        public async Task NavigateAddUser()
        {
            await Application.Current.MainPage.Navigation.PushAsync(new AddUserPage());
        }

        [RelayCommand]
        public async Task NavigateEditUser(UserModel user)
        {
            if (user == null) return;
            // Truyền user sang trang Edit
            await Application.Current.MainPage.Navigation.PushAsync(new EditUserPage(user));
        }

        [RelayCommand]
        public async Task ToggleStatus(UserModel user)
        {
            if (user == null) return;

            bool isActive = user.TrangThai == "Đang làm";
            string action = isActive ? "nghỉ việc" : "quay lại làm việc";

            bool confirm = await Application.Current.MainPage.DisplayAlert("Xác nhận",
                $"Bạn có muốn cho {user.HoTen} {action}?", "Có", "Không");

            if (!confirm) return;

            // Gọi API đổi trạng thái (Soft Delete / Restore)
            // Backend có API PUT /api/users/{id}/status ? Nếu chưa có, dùng UpdateUser
            // Theo code cũ bạn dùng PUT ApiConfig.UserById(maNV) -> UpdateUser chung

            // Giả sử dùng API UpdateUser chung:
            // Hoặc API riêng ToggleStatus: ApiConfig.ToggleUserStatus(user.MaNV)
            var url = ApiConfig.ToggleUserStatus(user.MaNV);

            var response = await _apiService.PutAsync<UserModel>(url, null); // Body null nếu backend chỉ cần ID

            if (response.Success)
            {
                await LoadUsersAsync(); // Reload lại list
                await Application.Current.MainPage.DisplayAlert("Thành công", "Đã cập nhật trạng thái.", "OK");
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Lỗi", response.Message, "OK");
            }
        }

        [RelayCommand]
        public async Task HardDeleteUser(UserModel user)
        {
            if (user == null) return;

            bool confirm = await Application.Current.MainPage.DisplayAlert("CẢNH BÁO",
                $"Xóa vĩnh viễn nhân viên {user.HoTen}? Hành động này không thể hoàn tác!", "Xóa ngay", "Hủy");

            if (!confirm) return;

            var response = await _apiService.DeleteAsync<object>(ApiConfig.UserById(user.MaNV));

            if (response.Success)
            {
                Users.Remove(user);
                _allUsers.Remove(user);
                await Application.Current.MainPage.DisplayAlert("Thành công", "Đã xóa vĩnh viễn.", "OK");
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Lỗi", response.Message, "OK");
            }
        }
    }
}