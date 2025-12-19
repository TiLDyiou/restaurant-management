using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagementAPI.Common.Wrappers;
using RestaurantManagementAPI.Data;
using RestaurantManagementAPI.Models.DTOs;
using RestaurantManagementAPI.Models.Entities;

[Route("api/[controller]")]
[ApiController]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly QLNHDbContext _context;

    public ChatController(IChatService chatService, QLNHDbContext context)
    {
        _chatService = chatService;
        _context = context;
    }

    [HttpGet("history/{conversationId}")]
    public async Task<IActionResult> GetHistory(string conversationId)
    {
        var result = await _chatService.GetHistory(conversationId);
        if (!result.Success) return BadRequest(result);

        var dtos = result.Data.Select(m => new ChatMessageDto
        {
            Id = m.Id,
            Content = m.Content,
            MaNV_Sender = m.MaNV_Sender,
            SenderName = m.SenderName,
            MaNV_Receiver = m.MaNV_Receiver,
            ConversationId = m.ConversationId,
            Timestamp = m.Timestamp,
            IsImage = m.IsImage,
            IsRead = m.IsRead
        }).ToList();

        return Ok(ServiceResult<List<ChatMessageDto>>.Ok(dtos));
    }

    [HttpGet("inbox-list/{currentUserId}")]
    public async Task<IActionResult> GetInboxList(string currentUserId)
    {
        var result = await _chatService.GetInboxList(currentUserId);
        return Ok(result);
    }

    [HttpPost("mark-read")]
    public async Task<IActionResult> MarkAsRead([FromQuery] string conversationId, [FromQuery] string userId)
    {
        var result = await _chatService.MarkAsRead(conversationId, userId);
        return Ok(result);
    }

    [HttpPost("upload-image")]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("File trống");
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        var extension = Path.GetExtension(file.FileName).ToLower();
        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest("Chỉ cho phép định dạng ảnh (.jpg, .png, .gif)");
        }
        var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var chatFolder = Path.Combine(webRootPath, "uploads", "chat");
        if (!Directory.Exists(chatFolder)) Directory.CreateDirectory(chatFolder);
        var fileName = $"{Guid.NewGuid()}{extension}";
        var fullPath = Path.Combine(chatFolder, fileName);

        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }
        var relativeUrl = Path.Combine("uploads", "chat", fileName).Replace("\\", "/");

        return Ok(ServiceResult<string>.Ok(relativeUrl));
    }
}