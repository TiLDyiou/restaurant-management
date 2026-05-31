using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantManagementAPI.DTOs;
using RestaurantManagementAPI.Interfaces;

namespace RestaurantManagementAPI.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        public UserController(IUserService userService) { _userService = userService; }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var result = await _userService.GetUserProfileAsync(User.Identity?.Name!);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _userService.GetAllUsersAsync();
            return Ok(result);
        }

        [Authorize]
        [HttpPut("{id?}")]
        public async Task<IActionResult> UpdateUser(string? id, [FromBody] UpdateUserDto dto)
        {
            var result = await _userService.UpdateUserAsync(User.Identity?.Name!, User.IsInRole("Admin"), id, dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [Authorize]
        [HttpPost("email/verify")]
        public async Task<IActionResult> VerifyEmailOtp([FromBody] VerifyOtpDto dto)
        {
            var result = await _userService.VerifyEmailOtpAsync(dto.Email, dto.OTP);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [Authorize]
        [HttpPost("email/resend-otp")]
        public async Task<IActionResult> ResendEmailOtp([FromBody] EmailDto dto)
        {
            var result = await _userService.ResendEmailOtpAsync(dto.Email);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}/status")]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var result = await _userService.ToggleUserStatusAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> HardDelete(string id)
        {
            var result = await _userService.HardDeleteUserAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }
    }
}