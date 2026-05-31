using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantManagementAPI.DTOs.BanDtos;
using RestaurantManagementAPI.Interfaces;
using System.Threading.Tasks;

namespace RestaurantManagementAPI.Controllers
{
    [Authorize]
    [Route("api/reservations")]
    [ApiController]
    public class ReservationsController : ControllerBase
    {
        private readonly IReservationService _service;

        public ReservationsController(IReservationService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetReservations([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetAllReservationsAsync(pageNumber, pageSize);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateDatBanDto dto)
        {
            if (!ModelState.IsValid) 
                return BadRequest(ModelState);

            var result = await _service.CreateReservationAsync(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> Cancel(string id)
        {
            var result = await _service.CancelReservationAsync(id);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}