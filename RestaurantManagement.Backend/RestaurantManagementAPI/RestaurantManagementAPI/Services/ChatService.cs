using Microsoft.EntityFrameworkCore;
using RestaurantManagementAPI.Common.Wrappers;
using RestaurantManagementAPI.Data;
using RestaurantManagementAPI.Models.DTOs;
using RestaurantManagementAPI.Models.Entities;
using RestaurantManagementAPI.Interfaces;

namespace RestaurantManagementAPI.Services
{
    public class ChatService : IChatService
    {
        private readonly QLNHDbContext _context;
        private const string PublicGroupId = "PUBLIC_ROOM";

        public ChatService(QLNHDbContext context) => _context = context;

        public async Task<ServiceResult<List<Message>>> GetHistory(string conversationId, int limit = 50)
        {
            var logs = await _context.MESSAGES
                .Where(m => m.ConversationId == conversationId)
                .OrderByDescending(m => m.Timestamp)
                .Take(limit)
                .OrderBy(m => m.Timestamp)
                .ToListAsync();
            return ServiceResult<List<Message>>.Ok(logs);
        }

        public async Task<ServiceResult> SaveMessage(Message message)
        {
            message.Timestamp = DateTime.Now;
            _context.MESSAGES.Add(message);
            await _context.SaveChangesAsync();
            return ServiceResult.Ok();
        }

        public async Task<ServiceResult> MarkAsRead(string conversationId, string currentUserId)
        {
            var unreadMessages = await _context.MESSAGES
                .Where(m => m.ConversationId == conversationId
                            && m.MaNV_Receiver == currentUserId
                            && !m.IsRead)
                .ToListAsync();

            if (unreadMessages.Any())
            {
                unreadMessages.ForEach(m => m.IsRead = true);
                await _context.SaveChangesAsync();
            }

            return ServiceResult.Ok();
        }

        public async Task<ServiceResult<List<InboxItemDto>>> GetInboxList(string currentUserId)
        {
            var inboxList = new List<InboxItemDto>();
            var lastPublicMsg = await _context.MESSAGES
                .Where(m => m.ConversationId == PublicGroupId)
                .OrderByDescending(m => m.Timestamp)
                .FirstOrDefaultAsync();

            inboxList.Add(new InboxItemDto
            {
                ConversationId = PublicGroupId,
                DisplayName = "NHÓM CHUNG TOÀN NHÀ HÀNG",
                LastMessage = lastPublicMsg?.Content ?? "Chưa có tin nhắn nào",
                LastTimestamp = lastPublicMsg?.Timestamp ?? DateTime.Now,
                IsGroup = true,
                UnreadCount = 0
            });

            var otherEmployees = await _context.NHANVIEN
                .Where(n => n.MaNV != currentUserId)
                .ToListAsync();

            foreach (var emp in otherEmployees)
            {
                string conversationId = string.Compare(currentUserId, emp.MaNV) < 0
                    ? $"{currentUserId}_{emp.MaNV}"
                    : $"{emp.MaNV}_{currentUserId}";

                var lastMsg = await _context.MESSAGES
                    .Where(m => m.ConversationId == conversationId)
                    .OrderByDescending(m => m.Timestamp)
                    .FirstOrDefaultAsync();
                var unreadCount = await _context.MESSAGES
                    .CountAsync(m => m.ConversationId == conversationId
                                     && m.MaNV_Receiver == currentUserId
                                     && !m.IsRead);

                inboxList.Add(new InboxItemDto
                {
                    ConversationId = conversationId,
                    DisplayName = emp.HoTen,
                    PartnerId = emp.MaNV,
                    LastMessage = lastMsg?.Content ?? "Bắt đầu cuộc trò chuyện ngay",
                    LastTimestamp = lastMsg?.Timestamp ?? DateTime.MinValue,
                    UnreadCount = unreadCount,
                    IsGroup = false
                });
            }

            var sortedList = inboxList.OrderByDescending(x => x.LastTimestamp).ToList();
            return ServiceResult<List<InboxItemDto>>.Ok(sortedList);
        }
    }
}