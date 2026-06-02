using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementAPI.Common.Wrappers;
using RestaurantManagementAPI.Data;
using RestaurantManagementAPI.Models.Entities;

[Authorize]
[Route("api/notifications")]
[ApiController]
public class NotificationsController : ControllerBase
{
    private readonly QLNHDbContext _context;
    public NotificationsController(QLNHDbContext context) { _context = context; }

    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] string? loai = null, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var query = _context.THONGBAO.AsQueryable();
        if (!string.IsNullOrEmpty(loai)) 
            query = query.Where(x => x.Loai == loai);

        var totalCount = await query.CountAsync();
        var list = await query
            .OrderByDescending(x => x.ThoiGian)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var paginated = PaginatedResult<ThongBao>.Create(list, totalCount, pageNumber, pageSize);
        return Ok(ServiceResult<PaginatedResult<ThongBao>>.Ok(paginated));
    }

    [HttpDelete]
    public async Task<IActionResult> ClearNotifications([FromQuery] string? loai = null)
    {
        var query = _context.THONGBAO.AsQueryable();
        if (!string.IsNullOrEmpty(loai)) 
            query = query.Where(x => x.Loai == loai);
        var list = await query.ToListAsync();
        if (list.Any())
        {
            _context.THONGBAO.RemoveRange(list);
            await _context.SaveChangesAsync();
        }
        return Ok(ServiceResult.Ok());
    }
}