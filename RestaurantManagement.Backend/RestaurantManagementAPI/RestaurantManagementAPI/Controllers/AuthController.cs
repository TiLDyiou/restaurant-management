using Microsoft.AspNetCore.Mvc;
using RestaurantManagementAPI.DTOs;
using RestaurantManagementAPI.Services.Interfaces;

namespace RestaurantManagementAPI.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var result = await _authService.RegisterAsync(dto);

            if (result.Success)
            {
                return Ok(new
                {
                    success = true,
                    message = result.Message,
                    data = new
                    {
                        email = dto.Email, 
                        maNV = result.MaNV
                    }
                });
            }

            return BadRequest(new { success = false, message = result.Message });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var result = await _authService.LoginAsync(dto);

            if (result.Success)
            {
                return Ok(new
                {
                    success = true,
                    message = result.Message,
                    data = result.Data
                });
            }

            return Unauthorized(new { success = false, message = result.Message });
        }

        [HttpPost("otp/register")]
        public async Task<IActionResult> SendRegisterOtp([FromBody] EmailDto dto)
        {
            var result = await _authService.SendRegisterOtpAsync(dto.Email);

            return result.Success
                ? Ok(new { success = true, message = result.Message })
                : BadRequest(new { success = false, message = result.Message });
        }

        [HttpPost("verify/register")]
        public async Task<IActionResult> VerifyRegisterOtp([FromBody] VerifyOtpDto dto)
        {
            var result = await _authService.VerifyRegisterOtpAsync(dto.Email, dto.OTP);

            return result.Success
                ? Ok(new { success = true, message = result.Message })
                : BadRequest(new { success = false, message = result.Message });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] EmailDto dto)
        {
            var result = await _authService.ForgotPasswordAsync(dto.Email);

            return result.Success
                ? Ok(new { success = true, message = result.Message })
                : BadRequest(new { success = false, message = result.Message });
        }

        [HttpPost("verify/reset-password")]
        public async Task<IActionResult> VerifyForgotOtp([FromBody] VerifyOtpDto dto)
        {
            var result = await _authService.VerifyForgotOtpAsync(dto.Email, dto.OTP);

            return result.Success
                ? Ok(new { success = true, message = result.Message })
                : BadRequest(new { success = false, message = result.Message });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var result = await _authService.ResetPasswordAsync(dto);

            return result.Success
                ? Ok(new { success = true, message = result.Message })
                : BadRequest(new { success = false, message = result.Message });
        }
    }
}