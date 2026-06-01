using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using RestaurantManagementGUI.Helpers;
using RestaurantManagementGUI.Models;

namespace RestaurantManagementGUI.Services
{
    public class TableService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public TableService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        public async Task<List<Ban>?> GetAllTablesAsync()
        {
            try
            {
                var url = $"{ApiConfig.Tables}?pageSize=100";
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<PaginatedResult<Ban>>>(url, _jsonOptions);
                return (response != null && response.Success && response.Data != null) ? response.Data.Items : new List<Ban>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TableService] GetTables Error: {ex.Message}");
                return new List<Ban>();
            }
        }

        public async Task<bool> UpdateStatusAsync(string maBan, string trangThai)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync(ApiConfig.UpdateTableStatus(maBan), trangThai);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse>(_jsonOptions);
                    return result != null && result.Success;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TableService] UpdateStatus Error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> MergeTablesAsync(string maBanChinh, string maBanPhu)
        {
            try
            {
                var payload = new MergeTablesDto { MaBanChinh = maBanChinh, MaBanPhu = maBanPhu };
                var response = await _httpClient.PostAsJsonAsync(ApiConfig.MergeTables, payload);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(_jsonOptions);
                    return result != null && result.Success;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TableService] MergeTables Error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SplitTablesAsync(string maBan)
        {
            try
            {
                var response = await _httpClient.PostAsync(ApiConfig.SplitTables(maBan), null);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(_jsonOptions);
                    return result != null && result.Success;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TableService] SplitTables Error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> TransferOrderAsync(string maBanNguon, string maBanDich)
        {
            try
            {
                var payload = new TransferOrderDto { MaBanNguon = maBanNguon, MaBanDich = maBanDich };
                var response = await _httpClient.PostAsJsonAsync(ApiConfig.TransferOrder, payload);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(_jsonOptions);
                    return result != null && result.Success;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TableService] TransferOrder Error: {ex.Message}");
                return false;
            }
        }

        public async Task<List<LichSuBanDto>> GetTableHistoryAsync(string maBan)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<LichSuBanDto>>>(ApiConfig.TableHistory(maBan), _jsonOptions);
                return (response != null && response.Success) ? (response.Data ?? new List<LichSuBanDto>()) : new List<LichSuBanDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TableService] GetTableHistory Error: {ex.Message}");
                return new List<LichSuBanDto>();
            }
        }

        public async Task<bool> CreateTableAsync(CreateBanDto dto)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(ApiConfig.Tables, dto);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(_jsonOptions);
                    return result != null && result.Success;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TableService] CreateTable Error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteTableAsync(string maBan)
        {
            try
            {
                var url = $"{ApiConfig.Tables}/{maBan}";
                var response = await _httpClient.DeleteAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(_jsonOptions);
                    return result != null && result.Success;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TableService] DeleteTable Error: {ex.Message}");
                return false;
            }
        }
    }
}