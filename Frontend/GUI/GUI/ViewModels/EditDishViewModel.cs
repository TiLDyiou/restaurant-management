using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Models;
using RestaurantManagementGUI.Services;

namespace RestaurantManagementGUI.ViewModels
{
    public partial class EditDishViewModel : BaseViewModel
    {
        private readonly ApiService _apiService;
        private readonly string _dishId;

        [ObservableProperty] private string _tenMA;
        [ObservableProperty] private string _donGia; // String để bind Entry
        [ObservableProperty] private string _loai;
        [ObservableProperty] private string _hinhAnh;
        [ObservableProperty] private bool _trangThai;

        public EditDishViewModel(ApiService apiService, FoodModel dish)
        {
            _apiService = apiService;
            _dishId = dish.Id;

            // Fill data
            TenMA = dish.Name;
            DonGia = dish.Price.ToString();
            Loai = dish.Category;
            HinhAnh = dish.ImageUrl;
            TrangThai = dish.TrangThai;
        }

        [RelayCommand]
        public async Task PickImage()
        {
            try
            {
                var result = await FilePicker.Default.PickAsync(new PickOptions { FileTypes = FilePickerFileType.Images });
                if (result != null) HinhAnh = result.FullPath;
            }
            catch { }
        }

        [RelayCommand]
        public async Task SaveChanges()
        {
            if (string.IsNullOrWhiteSpace(TenMA) || !decimal.TryParse(DonGia, out decimal price))
            {
                await Application.Current.MainPage.DisplayAlert("Lỗi", "Thông tin không hợp lệ", "OK");
                return;
            }

            IsBusy = true;

            var dto = new UpdateMonAnDto_Full
            {
                TenMA = TenMA,
                DonGia = price,
                Loai = Loai,
                HinhAnh = HinhAnh,
                TrangThai = TrangThai
            };

            var response = await _apiService.PutAsync<object>(ApiConfig.DishById(_dishId), dto);

            if (response.Success)
            {
                await Application.Current.MainPage.DisplayAlert("Thành công", "Đã cập nhật món ăn.", "OK");
                await Application.Current.MainPage.Navigation.PopAsync();
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Lỗi", response.Message, "OK");
            }

            IsBusy = false;
        }

        [RelayCommand]
        public async Task Cancel() => await Application.Current.MainPage.Navigation.PopAsync();
    }
}