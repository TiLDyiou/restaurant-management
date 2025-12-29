using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Maui.Storage;
using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Models;

namespace RestaurantManagementGUI
{
    public partial class EditMonAnPage : ContentPage
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly FoodModel _foodItem;

        public EditMonAnPage(HttpClient httpCLient, FoodModel foodItemToEdit)
        {
            InitializeComponent();
            _httpClient = httpCLient;
            _foodItem = foodItemToEdit;
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            EntryTenMA.Text = _foodItem.Name;
            EntryDonGia.Text = _foodItem.Price.ToString("0.##");
            EntryLoai.Text = _foodItem.Category;
            EntryHinhAnh.Text = _foodItem.ImageUrl;
            SwitchTrangThai.IsToggled = _foodItem.TrangThai;

            UpdateImagePreview();
        }

        private async void PickImage_Clicked(object sender, EventArgs e)
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
                    EntryHinhAnh.Text = result.FullPath;
                    UpdateImagePreview();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi", "Không thể chọn ảnh: " + ex.Message, "OK");
            }
        }

        private void EntryHinhAnh_Unfocused(object sender, FocusEventArgs e) => UpdateImagePreview();
        private void EntryHinhAnh_Completed(object sender, EventArgs e) => UpdateImagePreview();

        private void UpdateImagePreview()
        {
            if (!string.IsNullOrWhiteSpace(EntryHinhAnh.Text))
            {
                ImgPreview.Source = EntryHinhAnh.Text;
            }
        }

        private async void Save_Clicked(object sender, EventArgs e)
        {
            string tenMoi = EntryTenMA.Text?.Trim();
            if (string.IsNullOrWhiteSpace(tenMoi) || !decimal.TryParse(EntryDonGia.Text, out decimal giaMoi))
            {
                await DisplayAlert("Lỗi", "Tên món và Giá tiền không hợp lệ.", "OK");
                return;
            }

            var updateDto = new UpdateDishReq
            {
                TenMA = tenMoi,
                DonGia = giaMoi,
                Loai = EntryLoai.Text?.Trim(),
                HinhAnh = EntryHinhAnh.Text?.Trim(),
                TrangThai = SwitchTrangThai.IsToggled
            };

            try
            {
                var token = await SecureStorage.Default.GetAsync("auth_token");
                if (string.IsNullOrEmpty(token))
                {
                    await DisplayAlert("Lỗi", "Phiên đăng nhập hết hạn.", "OK");
                    return;
                }

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var url = ApiConfig.DishById(_foodItem.Id);

                var response = await _httpClient.PutAsJsonAsync(url, updateDto);
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(_jsonOptions);

                if (response.IsSuccessStatusCode && result != null && result.Success)
                {
                    await DisplayAlert("Thành công", result.Message ?? "Cập nhật món ăn thành công.", "OK");
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert("Thất bại", result?.Message ?? "Không thể cập nhật.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi Hệ thống", ex.Message, "OK");
            }
        }

        private async void Cancel_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}