using Microsoft.AspNetCore.Mvc;
using RestaurantManagementAPI.DTOs.BanDtos;
using RestaurantManagementAPI.Interfaces;

namespace RestaurantManagementAPI.Controllers
{
    [Route("api/reservations")]
    [ApiController]
    public class ReservationsController : ControllerBase
    {
        private readonly IReservationService _service;
        public ReservationsController(IReservationService service) { _service = service; }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateDatBanDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _service.CreateReservationAsync(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}