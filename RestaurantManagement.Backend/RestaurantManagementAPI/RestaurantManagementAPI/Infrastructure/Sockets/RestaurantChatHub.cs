using Microsoft.AspNetCore.SignalR;
using RestaurantManagementAPI.Models.Entities;
using RestaurantManagementAPI.Models.DTOs;
using RestaurantManagementAPI.Interfaces;

namespace RestaurantManagementAPI.Infrastructure.Sockets
{
    public class RestaurantChatHub : Hub
    {
        private readonly IChatService _chatService;
        public RestaurantChatHub(IChatService chatService) => _chatService = chatService;
        public async Task JoinConversation(string conversationId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
        }
        public async Task LeaveConversation(string conversationId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId);
        }

        public async Task SendMessage(Message msg)
        {
            msg.Timestamp = DateTime.Now;
            msg.IsRead = false;
            var saveResult = await _chatService.SaveMessage(msg);

            if (saveResult.Success)
            {
                await Clients.OthersInGroup(msg.ConversationId).SendAsync("ReceiveMessage", msg);
                await Clients.Caller.SendAsync("MessageSentConfirmed", msg);
            }
        }

        public async Task MarkAsRead(string conversationId, string currentUserId)
        {
            await _chatService.MarkAsRead(conversationId, currentUserId);
            await Clients.OthersInGroup(conversationId).SendAsync("UserReadMessages", conversationId, currentUserId);
        }
    }
}