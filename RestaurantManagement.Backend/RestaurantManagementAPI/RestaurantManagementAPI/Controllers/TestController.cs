using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurentManagementAPI.Data;

namespace RestaurentManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly QLNHDbContext _context;

        public TestController(QLNHDbContext context)
        {
            _context = context;
        }

        [HttpGet("check-db")]
        public IActionResult CheckDb()
        {
            try
            {
                var nvCount = _context.NHANVIEN.Count();
                var tkCount = _context.TAIKHOAN.Count();
                return Ok(new { success = true, nhanvien = nvCount, taikhoan = tkCount });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("admin-only")]
        public IActionResult AdminOnly()
        {
            return Ok("Bạn là Admin — truy cập thành công.");
        }

        [Authorize(Roles = "Admin,NhanVien")]
        [HttpGet("staff")]
        public IActionResult Staff()
        {
            return Ok("Bạn là nhân viên hoặc admin — truy cập thành công.");
        }
    }
}
