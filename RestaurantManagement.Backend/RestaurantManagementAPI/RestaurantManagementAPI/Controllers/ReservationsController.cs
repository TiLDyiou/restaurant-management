using Microsoft.AspNetCore.Mvc;
using RestaurantManagementAPI.DTOs.BanDtos;
using RestaurantManagementAPI.Services.Interfaces;

namespace RestaurantManagementAPI.Controllers
{
    [Route("api/reservations")]
    [ApiController]
    public class ReservationsController : ControllerBase
    {
        private readonly IReservationService _reservationService;

        public ReservationsController(IReservationService reservationService)
        {
            _reservationService = reservationService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateDatBanDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _reservationService.CreateReservationAsync(dto);
            return result.Success
                ? Ok(new { success = true, message = result.Message, data = result.Data })
                : BadRequest(new { success = false, message = result.Message });
        }
    }
}