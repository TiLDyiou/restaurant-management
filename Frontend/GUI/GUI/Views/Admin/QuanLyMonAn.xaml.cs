using RestaurantManagementGUI.Models;
using RestaurantManagementGUI.Helpers;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Maui.Storage;
using System.Net.Http.Headers;

namespace RestaurantManagementGUI;

public partial class QuanLyMonAnPage : ContentPage
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    public ObservableCollection<FoodModel> FoodItems { get; set; } = new();

    public QuanLyMonAnPage(HttpClient httpClient)
    {
        InitializeComponent();

        _httpClient = httpClient;

        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        DishesCollectionView.ItemsSource = FoodItems;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDishesAsync();
    }

    private async Task LoadDishesAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<FoodModel>>>(ApiConfig.Dishes, _jsonOptions);

            if (response != null && response.Success)
            {
                FoodItems.Clear();
                if (response.Data != null)
                {
                    foreach (var item in response.Data)
                    {
                        FoodItems.Add(item);
                    }
                }
            }
            else
            {
                await DisplayAlert("Lỗi", response?.Message ?? "Không thể tải danh sách món.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi kết nối", ex.Message, "OK");
        }
    }

    private async void AddDish_Clicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NewTenMA.Text) ||
            string.IsNullOrWhiteSpace(NewLoai.Text) ||
            !decimal.TryParse(NewDonGia.Text, out decimal donGia))
        {
            await DisplayAlert("Lỗi", "Vui lòng nhập đầy đủ thông tin.", "OK");
            return;
        }

        AddButton.IsEnabled = false;
        AddButton.Text = "Đang thêm...";

        var newDish = new CreateMonAnDto
        {
            TenMA = NewTenMA.Text.Trim(),
            DonGia = donGia,
            Loai = NewLoai.Text.Trim(),
            HinhAnh = string.IsNullOrWhiteSpace(NewHinhAnh.Text) ? "" : NewHinhAnh.Text.Trim()
        };

        try
        {
            var token = await SecureStorage.Default.GetAsync("auth_token");
            if (!string.IsNullOrEmpty(token))
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.PostAsJsonAsync(ApiConfig.Dishes, newDish);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<FoodModel>>(_jsonOptions);

            if (response.IsSuccessStatusCode && result != null && result.Success)
            {
                await DisplayAlert("Thành công", "Đã thêm món mới.", "OK");

                NewTenMA.Text = NewDonGia.Text = NewLoai.Text = NewHinhAnh.Text = "";
                await LoadDishesAsync();
            }
            else
            {
                await DisplayAlert("Thất bại", result?.Message ?? "Lỗi server", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", ex.Message, "OK");
        }
        finally
        {
            AddButton.IsEnabled = true;
            AddButton.Text = "THÊM MÓN MỚI";
        }
    }

    private async void DeleteDish_Clicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is FoodModel food)
        {
            bool confirm = await DisplayAlert("Xác nhận", $"Xóa món '{food.Name}'?", "Xóa", "Hủy");
            if (!confirm) return;

            try
            {
                var token = await SecureStorage.Default.GetAsync("auth_token");
                if (!string.IsNullOrEmpty(token))
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var url = ApiConfig.DishById(food.Id);
                var response = await _httpClient.DeleteAsync(url);
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(_jsonOptions);

                if (response.IsSuccessStatusCode && result != null && result.Success)
                {
                    await DisplayAlert("Thành công", "Đã xóa món ăn.", "OK");
                    await LoadDishesAsync();
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

    private async void EditDish_Clicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is FoodModel food)
        {
            await Navigation.PushAsync(new EditMonAnPage(_httpClient, food));
        }
    }

    private async void PickImage_Clicked(object sender, EventArgs e)
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions { FileTypes = FilePickerFileType.Images });
            if (result != null) NewHinhAnh.Text = result.FullPath;
        }
        catch { }
    }
}