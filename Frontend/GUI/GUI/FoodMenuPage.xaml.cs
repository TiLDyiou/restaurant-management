using RestaurantManagementGUI.Models;
using RestaurantManagementGUI.Helpers;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Maui.Controls;
using System.Net.Http;
using System.Linq;
using System;

namespace RestaurantManagementGUI.Views
{
    public partial class FoodMenuPage : ContentPage
    {
        private readonly HttpClient _httpClient;
        public ObservableCollection<DishGroup> GroupedDishes { get; set; } = new();

        public FoodMenuPage()
        {
            InitializeComponent();

            try
            {
#if DEBUG
                _httpClient = new HttpClient(GetInsecureHandler());
#else
                _httpClient = new HttpClient();
#endif

                // ?? ??M B?O B?N ?Ã S?A C?NG VÀ GIAO TH?C (http/https) CHÍNH XÁC
                _httpClient.BaseAddress = new Uri("https://localhost:7004/"); // Ho?c http://localhost:5276/

                BindingContext = this;
            }
            catch (Exception ex)
            {
                DisplayAlert("L?i Kh?i T?o (Constructor)",
                             $"Không th? kh?i t?o HttpClient. Hãy ki?m tra BaseAddress (URL/PORT).\nL?i: {ex.Message}",
                             "?óng");
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (_httpClient != null)
            {
                await LoadDishesAsync();
            }
        }

        private async Task LoadDishesAsync()
        {
            try
            {
                var dishes = await _httpClient.GetFromJsonAsync<List<Dish>>("/api/dishes/get-dishes");

                if (dishes != null && dishes.Any())
                {
                    // ?? ?Ã S?A L?I TRÙNG DANH M?C (Case-Insensitive Grouping)
                    var groupedData = dishes
                        // 1. G?p nhóm d?a trên TÊN ?Ã VI?T HOA (và c?t kho?ng tr?ng)
                        .GroupBy(d => d.Loai.Trim().ToUpperInvariant())
                        .Select(group => new DishGroup(
                            // 2. L?y tên g?c (ví d?: "Món Chính") làm tiêu ?? c?t
                            group.First().Loai.Trim(),
                            group.ToList()
                        ))
                        .OrderBy(g => g.Category);

                    // 3. C?p nh?t UI
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        GroupedDishes.Clear();
                        foreach (var group in groupedData)
                        {
                            GroupedDishes.Add(group);
                        }
                    });
                }
                else
                {
                    await DisplayAlert("Thông tin", "API không tr? v? món ?n nào.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("L?i T?i Menu (LoadDishesAsync)", $"Không th? t?i API: {ex.Message}", "OK");
            }
        }

        // (Hàm GetInsecureHandler gi? nguyên)
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
}