// File: EditMonAnPage.xaml.cs
using RestaurantManagementGUI.Models;
using RestaurantManagementGUI.Helpers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Maui.Controls;
using System.Net.Http.Headers;
using Microsoft.Maui.Storage;
using System.Text.Json.Serialization;

namespace RestaurantManagementGUI;

// DTO để khớp với API /api/update_dish
public class UpdateMonAnDto_Full
{
    [JsonPropertyName("tenMA")]
    public string? TenMA { get; set; }
    [JsonPropertyName("donGia")]
    public decimal? DonGia { get; set; }
    [JsonPropertyName("loai")]
    public string? Loai { get; set; }
    [JsonPropertyName("hinhAnh")]
    public string? HinhAnh { get; set; }
    [JsonPropertyName("trangThai")]
    public bool? TrangThai { get; set; }
}


public partial class EditMonAnPage : ContentPage
{
    private readonly HttpClient _httpClient;
    private FoodModel _foodItem; // Biến để lưu món ăn được gửi qua

    public EditMonAnPage(FoodModel foodItemToEdit)
    {
        InitializeComponent();

        _foodItem = foodItemToEdit;

        // Khởi tạo HttpClient (giống LoginPage)
#if DEBUG
        _httpClient = new HttpClient(GetInsecureHandler());
#else
        _httpClient = new HttpClient();
#endif

        // Đổ dữ liệu cũ vào các ô Entry
        EntryTenMA.Text = _foodItem.Name;
        EntryDonGia.Text = _foodItem.Price.ToString();
        EntryLoai.Text = _foodItem.Category;
        EntryHinhAnh.Text = _foodItem.ImageUrl;
        SwitchTrangThai.IsToggled = _foodItem.TrangThai;
    }

    // NÚT LƯU THAY ĐỔI
    private async void Save_Clicked(object sender, EventArgs e)
    {
        // 1. Kiểm tra dữ liệu
        if (string.IsNullOrWhiteSpace(EntryTenMA.Text) ||
            !decimal.TryParse(EntryDonGia.Text, out decimal newPrice))
        {
            await DisplayAlert("Lỗi", "Tên và Giá không được rỗng.", "OK");
            return;
        }

        // 2. Tạo DTO
        var updateDto = new UpdateMonAnDto_Full
        {
            TenMA = EntryTenMA.Text,
            DonGia = newPrice,
            Loai = EntryLoai.Text,
            HinhAnh = EntryHinhAnh.Text,
            TrangThai = SwitchTrangThai.IsToggled
        };

        try
        {
            var content = new StringContent(JsonSerializer.Serialize(updateDto), Encoding.UTF8, "application/json");

            // 3. Thêm Token
            var token = await SecureStorage.Default.GetAsync("auth_token");
            if (string.IsNullOrEmpty(token))
            {
                await DisplayAlert("Lỗi", "Phiên đăng nhập hết hạn.", "OK");
                return;
            }
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // 4. Gọi API PUT /api/update_dish/{maMA}
            var response = await _httpClient.PutAsync(ApiConfig.UpdateDish(_foodItem.Id), content);

            if (response.IsSuccessStatusCode)
            {
                await DisplayAlert("Thành công", "Đã cập nhật món ăn.", "OK");
                await Navigation.PopAsync(); // Quay lại trang Quản lý
            }
            else
            {
                await DisplayAlert("Lỗi", "Không thể cập nhật món ăn.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi Hệ thống", ex.Message, "OK");
        }
    }

    // NÚT HỦY
    private async void Cancel_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync(); // Quay lại
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