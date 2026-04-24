using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using QuizWiz_Backend.Classes;
using QuizWiz_Backend.Data;
using System.Security.Claims;

namespace QuizWiz_Backend.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly AppDbContext _context;

        public ChatHub(AppDbContext context)
        {
            _context = context;
        }

        public async Task SendMessage(int receiverId, string content)
        {
            var senderIdString = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(senderIdString) || !int.TryParse(senderIdString, out int senderId))
            {
                await Clients.Caller.SendAsync("Error", "UNAUTHORIZED");
                return;
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                await Clients.Caller.SendAsync("Error", "EMPTY_MESSAGE");
                return;
            }

            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = content,
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            try
            {
                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                await Clients.Users(receiverId.ToString(), senderId.ToString()).SendAsync("ReceiveMessage", new
                {
                    id = message.Id,
                    senderId = message.SenderId,
                    receiverId = message.ReceiverId,
                    content = message.Content,
                    sentAt = message.SentAt,
                    isRead = false
                });
            }
            catch
            {
                await Clients.Caller.SendAsync("Error", "MESSAGE_SAVE_FAILED");
            }
        }

        public async Task MarkAsRead(int friendId)
        {
            var userIdString = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int currentUserId)) return;

            var unreadMessages = await _context.Messages
                .Where(m => m.SenderId == friendId && m.ReceiverId == currentUserId && !m.IsRead)
                .ToListAsync();

            if (unreadMessages.Any())
            {
                foreach (var msg in unreadMessages)
                {
                    msg.IsRead = true;
                }
                await _context.SaveChangesAsync();

                await Clients.User(friendId.ToString()).SendAsync("MessagesRead", currentUserId);
            }
        }
    }
}