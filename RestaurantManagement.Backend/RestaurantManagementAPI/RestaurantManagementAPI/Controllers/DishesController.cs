using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantManagementAPI.DTOs;
using RestaurantManagementAPI.Interfaces;

namespace RestaurantManagementAPI.Controllers
{
    [ApiController]
    [Route("api/dishes")]
    public class DishesController : ControllerBase
    {
        private readonly IDishService _dishService;
        public DishesController(IDishService dishService) { _dishService = dishService; }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetDishes()
        {
            var result = await _dishService.GetAllDishesAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetDish(string id)
        {
            var result = await _dishService.GetDishByIdAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PostDish([FromBody] CreateMonAnDto dto)
        {
            var result = await _dishService.CreateDishAsync(dto);
            if (!result.Success)
                return BadRequest(result);
            return CreatedAtAction(
                nameof(GetDish), 
                new { id = result.Data }, 
            result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PutDish(string id, [FromBody] UpdateMonAnDto dto)
        {
            var result = await _dishService.UpdateDishAsync(id, dto);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteDish(string id)
        {
            var result = await _dishService.SoftDeleteDishAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }
    }
}