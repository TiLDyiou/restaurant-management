using RestaurantManagementAPI.Common.Wrappers;
using RestaurantManagementAPI.DTOs.BanDtos;
using RestaurantManagementAPI.Models.Entities;

namespace RestaurantManagementAPI.Interfaces
{
    public interface IReservationService
    {
        Task<ServiceResult<DatBan>> CreateReservationAsync(CreateDatBanDto dto);
    }
}