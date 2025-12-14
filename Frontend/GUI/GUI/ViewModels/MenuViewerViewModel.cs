using CommunityToolkit.Mvvm.ComponentModel;
using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Models;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;

namespace RestaurantManagementGUI.ViewModels
{
    public partial class MenuViewerViewModel : ObservableObject
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        [ObservableProperty]
        private ObservableCollection<DishGroup> groupedDishes = new();

        [ObservableProperty]
        private bool isLoading;

        public MenuViewerViewModel()
        {
            var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (m, c, ch, e) => true };
            _httpClient = new HttpClient(handler) { BaseAddress = new Uri(ApiConfig.BaseUrl) };
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        public async Task LoadMenuAsync()
        {
            if (IsLoading) return;
            IsLoading = true;

            try
            {
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<FoodModel>>>(ApiConfig.Dishes, _jsonOptions);

                if (response != null && response.Success && response.Data != null)
                {
                    var dishes = response.Data;
                    var groups = dishes
                        .GroupBy(d =>
                        {
                            if (string.IsNullOrWhiteSpace(d.Category)) 
                                return "Khác";
                            string cleanCat = d.Category.Trim().ToLower();
                            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(cleanCat);
                        })
                        .Select(g => new DishGroup(g.Key, g.ToList()))
                        .OrderBy(g => g.Category)
                        .ToList();

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        GroupedDishes.Clear();
                        foreach (var g in groups) GroupedDishes.Add(g);
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Menu Load Error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}