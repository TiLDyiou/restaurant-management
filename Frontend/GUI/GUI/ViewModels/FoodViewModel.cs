using System.Collections.ObjectModel;
using System.Windows.Input;
using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Models;
using RestaurantManagementGUI.Services;
using RestaurantManagementGUI.Views.Admin; // Để dùng EditMonAnPage

namespace RestaurantManagementGUI.ViewModels
{
    public class FoodViewModel : BaseViewModel
    {
        private readonly ApiService _apiService;
        private readonly IServiceProvider _serviceProvider;

        // Danh sách hiển thị
        public ObservableCollection<FoodModel> FoodItems { get; } = new();

        // Các trường nhập liệu (Binding TwoWay)
        private string _newTenMA;
        public string NewTenMA
        {
            get => _newTenMA;
            set { _newTenMA = value; OnPropertyChanged(); }
        }

        private string _newDonGia; // Để string cho dễ bind entry, sẽ parse sang decimal sau
        public string NewDonGia
        {
            get => _newDonGia;
            set { _newDonGia = value; OnPropertyChanged(); }
        }

        private string _newLoai;
        public string NewLoai
        {
            get => _newLoai;
            set { _newLoai = value; OnPropertyChanged(); }
        }

        private string _newHinhAnh;
        public string NewHinhAnh
        {
            get => _newHinhAnh;
            set { _newHinhAnh = value; OnPropertyChanged(); }
        }

        // Commands
        public ICommand LoadDishesCommand { get; }
        public ICommand AddDishCommand { get; }
        public ICommand DeleteDishCommand { get; }
        public ICommand EditDishCommand { get; }
        public ICommand PickImageCommand { get; }

        public FoodViewModel(ApiService apiService, IServiceProvider serviceProvider)
        {
            _apiService = apiService;
            _serviceProvider = serviceProvider;

            LoadDishesCommand = new Command(async () => await LoadDishesAsync());
            AddDishCommand = new Command(async () => await AddDishAsync());
            DeleteDishCommand = new Command<FoodModel>(async (item) => await DeleteDishAsync(item));
            EditDishCommand = new Command<FoodModel>(async (item) => await EditDishAsync(item));
            PickImageCommand = new Command(async () => await PickImageAsync());
        }

        public async Task LoadDishesAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            var response = await _apiService.GetAsync<List<FoodModel>>(ApiConfig.Dishes);

            if (response.Success && response.Data != null)
            {
                FoodItems.Clear();
                foreach (var item in response.Data)
                {
                    FoodItems.Add(item);
                }
            }
            // Không cần báo lỗi nếu tải thất bại để tránh spam popup, có thể log console

            IsBusy = false;
        }

        private async Task AddDishAsync()
        {
            if (string.IsNullOrWhiteSpace(NewTenMA) || string.IsNullOrWhiteSpace(NewLoai) || !decimal.TryParse(NewDonGia, out decimal price))
            {
                await Application.Current.MainPage.DisplayAlert("Lỗi", "Vui lòng nhập đầy đủ Tên, Giá và Loại hợp lệ.", "OK");
                return;
            }

            IsBusy = true;

            var newDish = new CreateMonAnDto
            {
                TenMA = NewTenMA,
                DonGia = price,
                Loai = NewLoai,
                HinhAnh = string.IsNullOrWhiteSpace(NewHinhAnh) ? "default.jpg" : NewHinhAnh
            };

            var response = await _apiService.PostAsync<FoodModel>(ApiConfig.Dishes, newDish);

            if (response.Success)
            {
                await Application.Current.MainPage.DisplayAlert("Thành công", "Đã thêm món ăn mới.", "OK");

                // Clear form
                NewTenMA = "";
                NewDonGia = "";
                NewLoai = "";
                NewHinhAnh = "";

                // Reload list
                await LoadDishesAsync();
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Lỗi", response.Message, "OK");
            }

            IsBusy = false;
        }

        private async Task DeleteDishAsync(FoodModel item)
        {
            if (item == null) return;

            bool confirm = await Application.Current.MainPage.DisplayAlert("Xác nhận", $"Bạn có chắc muốn xóa (nghỉ bán) món {item.Name}?", "Đồng ý", "Hủy");
            if (!confirm) return;

            IsBusy = true;

            var response = await _apiService.DeleteAsync<object>(ApiConfig.DishById(item.Id));

            if (response.Success)
            {
                await LoadDishesAsync(); // Load lại để cập nhật trạng thái
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Lỗi", response.Message, "OK");
            }

            IsBusy = false;
        }

        private async Task EditDishAsync(FoodModel item)
        {
            if (item == null) return;
            // Điều hướng sang trang Edit, truyền item đi
            await Application.Current.MainPage.Navigation.PushAsync(new EditMonAnPage(item));
        }

        private async Task PickImageAsync()
        {
            try
            {
                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Chọn ảnh món ăn",
                    FileTypes = FilePickerFileType.Images
                });

                if (result != null)
                {
                    NewHinhAnh = result.FullPath;
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Lỗi", "Không thể chọn ảnh: " + ex.Message, "OK");
            }
        }
    }
}