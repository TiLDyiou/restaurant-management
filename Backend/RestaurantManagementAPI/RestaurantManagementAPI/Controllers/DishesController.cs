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

        [HttpPost("upload-image")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0) 
                return BadRequest("File trống");

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(extension))
                return BadRequest("Chỉ cho phép định dạng ảnh (.jpg, .png, .gif)");

            if (file.Length > 5 * 1024 * 1024)
                return BadRequest("File quá lớn (tối đa 5MB)");

            var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var dishesFolder = Path.Combine(webRootPath, "uploads", "dishes");
            if (!Directory.Exists(dishesFolder)) 
                Directory.CreateDirectory(dishesFolder);

            var fileName = $"{Guid.NewGuid()}{extension}";
            var fullPath = Path.Combine(dishesFolder, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var relativeUrl = Path.Combine("uploads", "dishes", fileName).Replace("\\", "/");
            return Ok(Common.Wrappers.ServiceResult<string>.Ok(relativeUrl));
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