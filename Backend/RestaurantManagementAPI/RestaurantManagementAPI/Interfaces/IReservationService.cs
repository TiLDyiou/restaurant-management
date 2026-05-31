using RestaurantManagementAPI.Common.Wrappers;
using RestaurantManagementAPI.DTOs.BanDtos;
using RestaurantManagementAPI.Models.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RestaurantManagementAPI.Interfaces
{
    public interface IReservationService
    {
        Task<ServiceResult<DatBan>> CreateReservationAsync(CreateDatBanDto dto);
        Task<ServiceResult> CancelReservationAsync(string maDatBan);
        Task<ServiceResult<PaginatedResult<DatBanDto>>> GetAllReservationsAsync(int pageNumber = 1, int pageSize = 10);
    }
}