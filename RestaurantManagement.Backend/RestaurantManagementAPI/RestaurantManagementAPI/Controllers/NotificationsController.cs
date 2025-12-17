using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementAPI.Common.Wrappers;
using RestaurantManagementAPI.Data;

[Route("api/notifications")]
[ApiController]
public class NotificationsController : ControllerBase
{
    private readonly QLNHDbContext _context;
    public NotificationsController(QLNHDbContext context) { _context = context; }

    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] string? loai = null)
    {
        var query = _context.THONGBAO.AsQueryable();
        if (!string.IsNullOrEmpty(loai)) query = query.Where(x => x.Loai == loai);
        var list = await query.OrderByDescending(x => x.ThoiGian).Take(30).ToListAsync();
        return Ok(ServiceResult<object>.Ok(list));
    }

    [HttpDelete]
    public async Task<IActionResult> ClearNotifications([FromQuery] string? loai = null)
    {
        var query = _context.THONGBAO.AsQueryable();
        if (!string.IsNullOrEmpty(loai)) query = query.Where(x => x.Loai == loai);
        var list = await query.ToListAsync();
        if (list.Any())
        {
            _context.THONGBAO.RemoveRange(list);
            await _context.SaveChangesAsync();
        }
        return Ok(ServiceResult.Ok());
    }
}