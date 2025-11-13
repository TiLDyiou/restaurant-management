// File: QuanLyMonAnPage.xaml.cs
using RestaurantManagementGUI.Models;
using RestaurantManagementGUI.Helpers;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Maui.Controls;
using System.Net.Http.Headers;
using Microsoft.Maui.Storage;
using System.Text.Json.Serialization;

namespace RestaurantManagementGUI;

// DTO để khớp với API /api/add_dish
public class CreateMonAnDto
{
    [JsonPropertyName("tenMA")]
    public string TenMA { get; set; }
    [JsonPropertyName("donGia")]
    public decimal DonGia { get; set; }
    [JsonPropertyName("loai")]
    public string Loai { get; set; }
    [JsonPropertyName("hinhAnh")]
    public string HinhAnh { get; set; }
}

// DTO để khớp với API /api/update_dish
public class UpdateMonAnDto
{
    [JsonPropertyName("donGia")]
    public decimal? DonGia { get; set; }
}


public partial class QuanLyMonAnPage : ContentPage
{
    private readonly HttpClient _httpClient;

    public ObservableCollection<FoodModel> FoodItems { get; set; } = new();

    public QuanLyMonAnPage()
    {
        InitializeComponent();

#if DEBUG
        _httpClient = new HttpClient(GetInsecureHandler());
#else
        _httpClient = new HttpClient();
#endif

        DishesCollectionView.ItemsSource = FoodItems;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDishesAsync();
    }

    // HÀM TẢI API (GET /api/dishes - [AllowAnonymous])
    private async Task LoadDishesAsync()
    {
        try
        {
            // API này không cần token (AllowAnonymous)
            var items = await _httpClient.GetFromJsonAsync<List<FoodModel>>(ApiConfig.GetFoodMenu);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                FoodItems.Clear();
                if (items != null)
                {
                    foreach (var item in items)
                    {
                        FoodItems.Add(item);
                    }
                }
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi Tải Menu", $"Không thể tải API: {ex.Message}", "OK");
        }
    }

    // HÀM THÊM MÓN MỚI (POST /api/add_dish - [Authorize])
    private async void AddDish_Clicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NewTenMA.Text) ||
            string.IsNullOrWhiteSpace(NewLoai.Text) ||
            !decimal.TryParse(NewDonGia.Text, out decimal donGia))
        {
            await DisplayAlert("Lỗi", "Vui lòng nhập đầy đủ Tên, Giá và Loại hợp lệ.", "OK");
            return;
        }

        var newDishDto = new CreateMonAnDto
        {
            TenMA = NewTenMA.Text,
            DonGia = donGia,
            Loai = NewLoai.Text,
            HinhAnh = string.IsNullOrWhiteSpace(NewHinhAnh.Text) ? "default.jpg" : NewHinhAnh.Text
        };

        try
        {
            var content = new StringContent(JsonSerializer.Serialize(newDishDto), Encoding.UTF8, "application/json");

            // --- THÊM TOKEN VÀO ĐÂY ---
            var token = await SecureStorage.Default.GetAsync("auth_token");
            if (string.IsNullOrEmpty(token))
            {
                await DisplayAlert("Lỗi", "Phiên đăng nhập hết hạn.", "OK");
                return;
            }
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            // --------------------------

            var response = await _httpClient.PostAsync(ApiConfig.AddDish, content);

            if (response.IsSuccessStatusCode)
            {
                await DisplayAlert("Thành công", "Đã thêm món ăn mới.", "OK");
                await LoadDishesAsync();

                NewTenMA.Text = string.Empty;
                NewDonGia.Text = string.Empty;
                NewLoai.Text = string.Empty;
                NewHinhAnh.Text = string.Empty;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Lỗi API",
                    $"Không thể thêm món:\nStatusCode: {response.StatusCode}\nNội dung: {error}", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi Hệ thống", ex.Message, "OK");
        }
    }

    // HÀM XÓA MÓN (DELETE /api/softdelete_dish - [Authorize])
    private async void DeleteDish_Clicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        var foodItem = button?.BindingContext as FoodModel;

        if (foodItem == null) return;
        bool confirm = await DisplayAlert("Xác nhận Xóa",
            $"Bạn có chắc muốn xóa (nghỉ bán) món {foodItem.Name}?", "Đồng ý", "Hủy");
        if (!confirm) return;

        try
        {
            // --- THÊM TOKEN VÀO ĐÂY ---
            var token = await SecureStorage.Default.GetAsync("auth_token");
            if (string.IsNullOrEmpty(token))
            {
                await DisplayAlert("Lỗi", "Phiên đăng nhập hết hạn.", "OK");
                return;
            }
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            // --------------------------

            var response = await _httpClient.DeleteAsync(ApiConfig.SoftDeleteDish(foodItem.Id));
            if (response.IsSuccessStatusCode)
            {
                await DisplayAlert("Thành công", "Đã xóa món ăn.", "OK");
                await LoadDishesAsync();
            }
            else
            {
                await DisplayAlert("Lỗi", "Không thể xóa món ăn.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi Hệ thống", ex.Message, "OK");
        }
    }

    // HÀM SỬA (PUT /api/update_dish - [Authorize])
    private async void EditDish_Clicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        var foodItem = button?.BindingContext as FoodModel;
        if (foodItem == null) return;

        // Điều hướng đến trang Edit mới, gửi kèm món ăn
        await Navigation.PushAsync(new EditMonAnPage(foodItem));
    }

    // (Hàm GetInsecureHandler giữ nguyên từ LoginPage)
    private HttpClientHandler GetInsecureHandler()
    {
        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, errors) =>
        {
            if (sender is HttpRequestMessage request)
            {
                return request.RequestUri.IsLoopback ||
                       (DeviceInfo.Platform == DevicePlatform.Android && request.RequestUri.Host == "10.0.2.2");
            }
            return errors == System.Net.Security.SslPolicyErrors.None;
        };
        return handler;
    }
}