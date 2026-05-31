using RestaurantManagementAPI.Common.Wrappers;
using RestaurantManagementAPI.Models.DTOs;
using RestaurantManagementAPI.Models.Entities;

namespace RestaurantManagementAPI.Interfaces
{
    public interface IChatService
    {
        Task<ServiceResult<List<Message>>> GetHistory(string conversationId, int limit = 50);
        Task<ServiceResult> SaveMessage(Message message);

        Task<ServiceResult> MarkAsRead(string conversationId, string currentUserId);

        Task<ServiceResult<List<InboxItemDto>>> GetInboxList(string currentUserId);
    }
}
