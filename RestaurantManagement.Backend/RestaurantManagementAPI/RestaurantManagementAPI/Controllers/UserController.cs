using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantManagementAPI.DTOs;
using RestaurantManagementAPI.Services.Interfaces;

namespace RestaurantManagementAPI.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var user = await _userService.GetUserProfileAsync(User.Identity?.Name!);
            if (user == null) return NotFound(new { success = false, message = "Không tìm thấy thông tin người dùng." });

            return Ok(new
            {
                success = true,
                message = "Lấy thông tin thành công",
                data = user
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(new
            {
                success = true,
                message = "Lấy danh sách thành công",
                data = users
            });
        }

        [Authorize]
        [HttpPut("{id?}")]
        public async Task<IActionResult> UpdateUser(string? id, [FromBody] UpdateUserDto dto)
        {
            var (success, message, data) = await _userService.UpdateUserAsync(User.Identity?.Name!, User.IsInRole("Admin"), id, dto);

            if (success)
            {
                return Ok(new
                {
                    success = true,
                    message = message,
                    data = data
                });
            }

            return BadRequest(new { success = false, message = message });
        }

        [Authorize]
        [HttpPost("email/verify")]
        public async Task<IActionResult> VerifyEmailOtp([FromBody] VerifyOtpDto dto)
        {
            var (success, message) = await _userService.VerifyEmailOtpAsync(dto.Email, dto.OTP);

            return success
                ? Ok(new { success = true, message = message })
                : BadRequest(new { success = false, message = message });
        }

        [Authorize]
        [HttpPost("email/resend-otp")]
        public async Task<IActionResult> ResendEmailOtp([FromBody] EmailDto dto)
        {
            var (success, message) = await _userService.ResendEmailOtpAsync(dto.Email);

            return success
                ? Ok(new { success = true, message = message })
                : BadRequest(new { success = false, message = message });
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}/status")]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var (success, message, data) = await _userService.ToggleUserStatusAsync(id);

            return success
                ? Ok(new { success = true, message = message, data = data })
                : NotFound(new { success = false, message = message });
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> HardDelete(string id)
        {
            var (success, message) = await _userService.HardDeleteUserAsync(id);

            return success
                ? Ok(new { success = true, message = message })
                : NotFound(new { success = false, message = message });
        }
    }
}