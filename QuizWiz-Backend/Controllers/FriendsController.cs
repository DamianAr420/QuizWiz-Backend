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

        public FriendsController(AppDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var userIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdValue, out int id) ? id : 0;
        }

        [HttpPost("request/{addresseeId}")]
        public async Task<IActionResult> SendRequest(int addresseeId)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();
            if (userId == addresseeId) return BadRequest("Nie możesz zaprosić samego siebie.");

            var existing = await _context.Friendships
                .FirstOrDefaultAsync(f =>
                    (f.RequesterId == userId && f.AddresseeId == addresseeId) ||
                    (f.RequesterId == addresseeId && f.AddresseeId == userId));

            if (existing != null) return BadRequest("Relacja już istnieje.");

            var friendship = new Friendship
            {
                RequesterId = userId,
                AddresseeId = addresseeId,
                Status = "Pending"
            };

            _context.Friendships.Add(friendship);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Zaproszenie wysłane." });
        }

        [HttpPut("accept/{requesterId}")]
        public async Task<IActionResult> AcceptRequest(int requesterId)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var request = await _context.Friendships
                .FirstOrDefaultAsync(f => f.RequesterId == requesterId && f.AddresseeId == userId && f.Status == "Pending");

            if (request == null) return NotFound("Zaproszenie nie istnieje.");

            request.Status = "Accepted";
            await _context.SaveChangesAsync();
            return Ok(new { message = "Zaakceptowano zaproszenie." });
        }

        [HttpGet]
        public async Task<IActionResult> GetFriends()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var friends = await _context.Friendships
                .Where(f => f.Status == "Accepted" && (f.RequesterId == userId || f.AddresseeId == userId))
                .Join(_context.Users,
                      f => (f.RequesterId == userId ? f.AddresseeId : f.RequesterId),
                      u => u.Id,
                      (f, u) => new
                      {
                          u.Id,
                          u.DisplayName,
                          isOnline = false
                      })
                .ToListAsync();

            return Ok(friends);
        }

        [HttpGet("chat/{friendId}")]
        public async Task<IActionResult> GetChatHistory(int friendId)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var messages = await _context.Messages
                .Where(m =>
                    (m.SenderId == userId && m.ReceiverId == friendId) ||
                    (m.SenderId == friendId && m.ReceiverId == userId))
                .OrderBy(m => m.SentAt)
                .Select(m => new {
                    m.Id,
                    m.SenderId,
                    m.ReceiverId,
                    m.Content,
                    m.SentAt,
                    m.IsRead
                })
                .ToListAsync();

            return Ok(messages);
        }
    }
}