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

        string finalImageUrl = string.IsNullOrWhiteSpace(NewHinhAnh.Text) ? "" : NewHinhAnh.Text.Trim();
        if (finalImageUrl.StartsWith("data:image/"))
        {
            AddButton.Text = "Đang xử lý ảnh...";
            var uploadedUrl = await UploadBase64ImageAsync(finalImageUrl);
            if (!string.IsNullOrEmpty(uploadedUrl))
            {
                finalImageUrl = uploadedUrl;
            }
            else
            {
                await DisplayAlert("Lỗi", "Không thể lưu ảnh đã dán. Vui lòng thử lại hoặc tải ảnh về máy để chọn.", "OK");
                AddButton.IsEnabled = true;
                AddButton.Text = "THÊM MÓN MỚI";
                return;
            }
        }

        var newDish = new CreateMonAnDto
        {
            TenMA = NewTenMA.Text.Trim(),
            DonGia = donGia,
            Loai = NewLoai.Text.Trim(),
            HinhAnh = finalImageUrl
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

    private async void ToggleDishStatus_Clicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is FoodModel food)
        {
            string action = food.TrangThai ? "Ngừng bán" : "Bán lại";
            bool confirm = await DisplayAlert("Xác nhận", $"{action} món '{food.Name}'?", "Đồng ý", "Hủy");
            if (!confirm) return;

            try
            {
                var token = await SecureStorage.Default.GetAsync("auth_token");
                if (!string.IsNullOrEmpty(token))
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var url = ApiConfig.DishById(food.Id);
                var updateDto = new { TrangThai = !food.TrangThai };
                var response = await _httpClient.PutAsJsonAsync(url, updateDto);
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(_jsonOptions);

                if (response.IsSuccessStatusCode && result != null && result.Success)
                {
                    await DisplayAlert("Thành công", $"Đã {action.ToLower()} món ăn.", "OK");
                    await LoadDishesAsync();
                }
                else
                {
                    await DisplayAlert("Lỗi", result?.Message ?? $"Không thể {action.ToLower()}.", "OK");
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
            if (result != null) 
            {
                NewHinhAnh.Text = "Đang tải ảnh lên...";
                var url = await UploadImageAsync(result.FullPath);
                if (!string.IsNullOrEmpty(url))
                {
                    NewHinhAnh.Text = url;
                }
                else
                {
                    NewHinhAnh.Text = "";
                    await DisplayAlert("Lỗi", "Tải ảnh lên thất bại.", "OK");
                }
            }
        }
        catch { }
    }

    private async Task<string?> UploadImageAsync(string localPath)
    {
        try
        {
            var token = await SecureStorage.Default.GetAsync("auth_token");
            if (!string.IsNullOrEmpty(token))
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var stream = File.OpenRead(localPath);
            using var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(stream);
            
            string extension = Path.GetExtension(localPath).ToLower();
            string contentType = extension switch { ".png" => "image/png", ".gif" => "image/gif", _ => "image/jpeg" };
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Add(streamContent, "file", Path.GetFileName(localPath));

            var response = await _httpClient.PostAsync(ApiConfig.UploadDishImage, content);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<string>>(_jsonOptions);
                if (result != null && result.Success && !string.IsNullOrEmpty(result.Data))
                {
                    return ApiConfig.DomainUrl + result.Data;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Upload image error: {ex.Message}");
        }
        return null;
    }

    private async Task<string?> UploadBase64ImageAsync(string dataUri)
    {
        try
        {
            var commaIndex = dataUri.IndexOf(',');
            if (commaIndex < 0) return null;
            
            var header = dataUri.Substring(0, commaIndex);
            var base64 = dataUri.Substring(commaIndex + 1);
            
            string extension = ".jpg";
            string contentType = "image/jpeg";
            if (header.Contains("image/png")) { extension = ".png"; contentType = "image/png"; }
            else if (header.Contains("image/gif")) { extension = ".gif"; contentType = "image/gif"; }
            
            byte[] imageBytes = Convert.FromBase64String(base64);
            using var stream = new MemoryStream(imageBytes);
            using var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Add(streamContent, "file", $"pasted_image_{Guid.NewGuid()}{extension}");

            var token = await SecureStorage.Default.GetAsync("auth_token");
            if (!string.IsNullOrEmpty(token))
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.PostAsync(ApiConfig.UploadDishImage, content);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<string>>(_jsonOptions);
                if (result != null && result.Success && !string.IsNullOrEmpty(result.Data))
                {
                    return ApiConfig.DomainUrl + result.Data;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Upload base64 image error: {ex.Message}");
        }
        return null;
    }
}