using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantManagementAPI.DTOs;
using RestaurantManagementAPI.Services.Interfaces;

namespace RestaurantManagementAPI.Controllers
{
    [ApiController]
    [Route("api/dishes")]
    public class DishesController : ControllerBase
    {
        private readonly IDishService _dishService;

        public DishesController(IDishService dishService)
        {
            _dishService = dishService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetDishes()
        {
            var data = await _dishService.GetAllDishesAsync();
            return Ok(new { success = true, data });
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetDish(string id)
        {
            var dish = await _dishService.GetDishByIdAsync(id);
            if (dish == null) return NotFound(new { success = false, message = "Không tìm thấy món ăn" });
            return Ok(new { success = true, data = dish });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PostDish([FromBody] CreateMonAnDto createDto)
        {
            var result = await _dishService.CreateDishAsync(createDto);
            return Ok(new { success = true, message = result.Message, data = result.Data });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PutDish(string id, [FromBody] UpdateMonAnDto updateDto)
        {
            var result = await _dishService.UpdateDishAsync(id, updateDto);
            return result.Success
                ? Ok(new { success = true, message = result.Message })
                : NotFound(new { success = false, message = result.Message });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteDish(string id)
        {
            var result = await _dishService.SoftDeleteDishAsync(id);
            return result.Success
                ? Ok(new { success = true, message = result.Message })
                : NotFound(new { success = false, message = result.Message });
        }
    }
}