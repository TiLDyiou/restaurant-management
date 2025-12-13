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

public partial class EditMonAnPage : ContentPage
{
    private readonly HttpClient _httpClient;
    private FoodModel _foodItem;

    public EditMonAnPage(FoodModel foodItemToEdit)
    {
        InitializeComponent();

        _foodItem = foodItemToEdit;

#if DEBUG
        _httpClient = new HttpClient(GetInsecureHandler());
#else
        _httpClient = new HttpClient();
#endif

        EntryTenMA.Text = _foodItem.Name;
        EntryDonGia.Text = _foodItem.Price.ToString();
        EntryLoai.Text = _foodItem.Category;
        EntryHinhAnh.Text = _foodItem.ImageUrl;
        SwitchTrangThai.IsToggled = _foodItem.TrangThai;

        if (!string.IsNullOrEmpty(_foodItem.ImageUrl))
        {
            ImgPreview.Source = _foodItem.ImageUrl;
        }
    }

    // XỬ LÝ CHỌN FILE ẢNH
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
                // Điền đường dẫn vào ô Entry
                EntryHinhAnh.Text = result.FullPath;

                // Cập nhật ngay ảnh hiển thị bên dưới
                ImgPreview.Source = result.FullPath;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", "Không thể chọn ảnh: " + ex.Message, "OK");
        }
    }

    // Xử lý khi người dùng dán link thủ công rồi bấm ra ngoài (Unfocused) hoặc Enter (Completed)
    private void EntryHinhAnh_Unfocused(object sender, FocusEventArgs e)
    {
        UpdateImagePreview();
    }

    private void EntryHinhAnh_Completed(object sender, EventArgs e)
    {
        UpdateImagePreview();
    }

    private void UpdateImagePreview()
    {
        if (!string.IsNullOrWhiteSpace(EntryHinhAnh.Text))
        {
            ImgPreview.Source = EntryHinhAnh.Text;
        }
    }

    // NÚT LƯU THAY ĐỔI
    private async void Save_Clicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(EntryTenMA.Text) ||
            !decimal.TryParse(EntryDonGia.Text, out decimal newPrice))
        {
            await DisplayAlert("Lỗi", "Tên và Giá không được rỗng.", "OK");
            return;
        }

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

            var token = await SecureStorage.Default.GetAsync("auth_token");
            if (string.IsNullOrEmpty(token))
            {
                await DisplayAlert("Lỗi", "Phiên đăng nhập hết hạn.", "OK");
                return;
            }
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.PutAsync(ApiConfig.UpdateDish(_foodItem.Id), content);

            if (response.IsSuccessStatusCode)
            {
                await DisplayAlert("Thành công", "Đã cập nhật món ăn.", "OK");
                await Navigation.PopAsync();
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

    private async void Cancel_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

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