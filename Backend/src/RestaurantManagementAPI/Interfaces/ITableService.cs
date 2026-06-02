using RestaurantManagementAPI.Common.Wrappers;
using RestaurantManagementAPI.DTOs;
using RestaurantManagementAPI.Models.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RestaurantManagementAPI.Interfaces
{
    public interface ITableService
    {
        Task<ServiceResult<PaginatedResult<Ban>>> GetAllBanAsync(int pageNumber = 1, int pageSize = 10);
        Task<ServiceResult<Ban>> GetBanByIdAsync(string maBan);
        Task<ServiceResult<Ban>> UpdateStatusAsync(string maBan, string trangThai, string? maNV = null);
        Task<ServiceResult<Ban>> CreateBanAsync(CreateBanDto dto, string? maNV = null);
        Task<ServiceResult<Ban>> UpdateBanAsync(string maBan, UpdateBanDto dto, string? maNV = null);
        Task<ServiceResult<Ban>> DeleteBanAsync(string maBan, string? maNV = null);
        Task<ServiceResult<Ban>> MergeTablesAsync(MergeTablesDto dto, string? maNV = null);
        Task<ServiceResult<Ban>> SplitTablesAsync(string maBanChinh, string? maNV = null);
        Task<ServiceResult<Ban>> TransferOrderAsync(TransferOrderDto dto, string? maNV = null);
        Task<ServiceResult<List<LichSuBanDto>>> GetTableHistoryAsync(string maBan);
    }
}