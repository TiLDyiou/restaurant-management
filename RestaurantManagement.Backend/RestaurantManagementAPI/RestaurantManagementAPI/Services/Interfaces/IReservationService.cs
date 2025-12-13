using RestaurantManagementAPI.DTOs.BanDtos;
using RestaurantManagementAPI.Models.Entities;

namespace RestaurantManagementAPI.Services.Interfaces
{
    public interface IReservationService
    {
        Task<(bool Success, string Message, DatBan? Data)> CreateReservationAsync(CreateDatBanDto dto);
    }
}