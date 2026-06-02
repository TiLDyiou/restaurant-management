using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RestaurantManagementAPI.DTOs;
using RestaurantManagementAPI.Interfaces;
using System.Security.Claims;

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

        [HttpPost("login")]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _authService.LoginAsync(dto, ip);

            return result.Success
                ? Ok(new { success = true, message = result.Message, data = result.Data })
                : Unauthorized(new { success = false, message = result.Message });
        }

        [HttpPost("refresh")]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _authService.RefreshTokenAsync(dto.RefreshToken, ip);

            return result.Success
                ? Ok(new { success = true, message = result.Message, data = result.Data })
                : Unauthorized(new { success = false, message = result.Message });
        }

        [HttpPost("revoke")]
        [Authorize]
        public async Task<IActionResult> RevokeToken([FromBody] RefreshTokenDto dto)
        {
            var result = await _authService.RevokeRefreshTokenAsync(dto.RefreshToken);
            return result.Success
                ? Ok(new { success = true, message = result.Message })
                : BadRequest(new { success = false, message = result.Message });
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var maNV = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(maNV))
                return Unauthorized(new { message = "Không xác định được người dùng." });

            var result = await _authService.LogoutAsync(maNV);
            return result.Success
                ? Ok(new { message = result.Message })
                : BadRequest(new { message = result.Message });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _authService.RegisterAsync(dto);
            return result.Success
                ? Created("", new { success = true, message = result.Message, data = new { email = dto.Email, maNV = result.Data } })
                : BadRequest(new { success = false, message = result.Message });
        }

        [HttpPost("otp/register")]
        [EnableRateLimiting("otp")]
        public async Task<IActionResult> SendRegisterOtp([FromBody] EmailDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _authService.SendRegisterOtpAsync(dto.Email);
            return result.Success
                ? Ok(new { success = true, message = result.Message })
                : BadRequest(new { success = false, message = result.Message });
        }

        [HttpPost("verify/register")]
        public async Task<IActionResult> VerifyRegisterOtp([FromBody] VerifyOtpDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _authService.VerifyRegisterOtpAsync(dto.Email, dto.OTP);
            return result.Success
                ? Ok(new { success = true, message = result.Message })
                : BadRequest(new { success = false, message = result.Message });
        }

        [HttpPost("forgot-password")]
        [EnableRateLimiting("otp")]
        public async Task<IActionResult> ForgotPassword([FromBody] EmailDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _authService.ForgotPasswordAsync(dto.Email);
            return result.Success
                ? Ok(new { success = true, message = result.Message })
                : BadRequest(new { success = false, message = result.Message });
        }

        [HttpPost("verify/reset-password")]
        public async Task<IActionResult> VerifyForgotOtp([FromBody] VerifyOtpDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _authService.VerifyForgotOtpAsync(dto.Email, dto.OTP);
            return result.Success
                ? Ok(new { success = true, message = result.Message })
                : BadRequest(new { success = false, message = result.Message });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _authService.ResetPasswordAsync(dto);
            return result.Success
                ? Ok(new { success = true, message = result.Message })
                : BadRequest(new { success = false, message = result.Message });
        }
    }
}
