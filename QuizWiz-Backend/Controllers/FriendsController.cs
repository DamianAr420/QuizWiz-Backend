using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizWiz_Backend.Classes;
using QuizWiz_Backend.Data;
using System.Security.Claims;

namespace QuizWiz_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FriendsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FriendsController(AppDbContext context) => _context = context;

        private int GetCurrentUserId()
        {
            var userIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdValue, out int id) ? id : 0;
        }

        [HttpPost("request/{username}")]
        public async Task<IActionResult> SendRequest(string username)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized(new { code = "UNAUTHORIZED" });

            var addressee = await _context.Users.FirstOrDefaultAsync(u => u.DisplayName == username);
            if (addressee == null) return NotFound(new { code = "USER_NOT_FOUND" });

            if (userId == addressee.Id) return BadRequest(new { code = "CANNOT_INVITE_SELF" });

            var existing = await _context.Friendships
                .FirstOrDefaultAsync(f =>
                    (f.RequesterId == userId && f.AddresseeId == addressee.Id) ||
                    (f.RequesterId == addressee.Id && f.AddresseeId == userId));

            if (existing != null) return BadRequest(new { code = "FRIENDSHIP_ALREADY_EXISTS" });

            _context.Friendships.Add(new Friendship { RequesterId = userId, AddresseeId = addressee.Id, Status = "Pending" });
            await _context.SaveChangesAsync();

            return Ok(new { code = "SUCCESS_REQUEST_SENT" });
        }

        [HttpPut("accept/{requesterId}")]
        public async Task<IActionResult> AcceptRequest(int requesterId)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized(new { code = "UNAUTHORIZED" });

            var request = await _context.Friendships
                .FirstOrDefaultAsync(f => f.RequesterId == requesterId && f.AddresseeId == userId && f.Status == "Pending");

            if (request == null) return NotFound(new { code = "REQUEST_NOT_FOUND" });

            request.Status = "Accepted";
            await _context.SaveChangesAsync();
            return Ok(new { code = "SUCCESS_REQUEST_ACCEPTED" });
        }

        [HttpDelete("decline/{requesterId}")]
        public async Task<IActionResult> DeclineRequest(int requesterId)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized(new { code = "UNAUTHORIZED" });

            var request = await _context.Friendships
                .FirstOrDefaultAsync(f => f.RequesterId == requesterId && f.AddresseeId == userId && f.Status == "Pending");

            if (request == null) return NotFound(new { code = "REQUEST_NOT_FOUND" });

            _context.Friendships.Remove(request);
            await _context.SaveChangesAsync();
            return Ok(new { code = "SUCCESS_REQUEST_DECLINED" });
        }

        [HttpGet("requests")]
        public async Task<IActionResult> GetPendingRequests()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized(new { code = "UNAUTHORIZED" });

            var requests = await _context.Friendships
                .Where(f => f.Status == "Pending" && f.AddresseeId == userId)
                .Join(_context.Users, f => f.RequesterId, u => u.Id, (f, u) => new { senderId = u.Id, senderName = u.DisplayName })
                .ToListAsync();

            return Ok(requests);
        }

        [HttpGet]
        public async Task<IActionResult> GetFriends()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized(new { code = "UNAUTHORIZED" });

            var fiveMinutesAgo = DateTime.UtcNow.AddMinutes(-5);

            var friends = await _context.Friendships
                .Where(f => f.Status == "Accepted" && (f.RequesterId == userId || f.AddresseeId == userId))
                .Join(_context.Users,
                      f => (f.RequesterId == userId ? f.AddresseeId : f.RequesterId),
                      u => u.Id,
                      (f, u) => new {
                          u.Id,
                          u.DisplayName,
                          isOnline = u.LastActive > fiveMinutesAgo
                      })
                .ToListAsync();

            return Ok(friends);
        }

        [HttpGet("chat/{friendId}")]
        public async Task<IActionResult> GetChatHistory(int friendId)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized(new { code = "UNAUTHORIZED" });

            var messages = await _context.Messages
                .Where(m => (m.SenderId == userId && m.ReceiverId == friendId) || (m.SenderId == friendId && m.ReceiverId == userId))
                .OrderBy(m => m.SentAt)
                .Select(m => new { m.Id, m.SenderId, m.ReceiverId, m.Content, m.SentAt, m.IsRead })
                .ToListAsync();

            return Ok(messages);
        }
    }
}