using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizWiz_Backend.Classes;
using QuizWiz_Backend.Data;
using System.Security.Claims;

namespace QuizWiz_Backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int id))
            {
                return Unauthorized(new { message = "Nieprawidłowy token." });
            }

            var user = await _context.Users
                .Select(u => new { u.Id, u.DisplayName, u.Email, u.CreatedAt })
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return NotFound();

            return Ok(user);
        }

        [HttpPut("update-profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _context.Users.FindAsync(userId);

            if (user == null) return NotFound();

            user.DisplayName = dto.DisplayName;

            await _context.SaveChangesAsync();

            return Ok(new { user.Id, user.DisplayName, user.Email });
        }

        [HttpDelete("delete-account")]
        public async Task<IActionResult> DeleteAccount()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _context.Users.FindAsync(userId);

            if (user == null) return NotFound();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetUserStats()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out int userId)) return Unauthorized();

            var statsQuery = await _context.QuizAttempts
                .Where(a => a.UserId == userId)
                .GroupBy(a => a.UserId)
                .Select(g => new
                {
                    QuizzesPlayed = g.Count(),
                    TotalQuestionsAnswered = g.Sum(a => a.TotalQuestions),
                    CorrectAnswers = g.Sum(a => a.Score)
                })
                .FirstOrDefaultAsync();

            var dates = await _context.QuizAttempts
                .Where(a => a.UserId == userId)
                .Select(a => a.CompletedAt)
                .OrderByDescending(d => d)
                .ToListAsync();

            if (statsQuery == null)
            {
                return Ok(new { QuizzesPlayed = 0, TotalQuestionsAnswered = 0, CorrectAnswers = 0, BestStreak = 0 });
            }

            return Ok(new
            {
                statsQuery.QuizzesPlayed,
                statsQuery.TotalQuestionsAnswered,
                statsQuery.CorrectAnswers,
                BestStreak = CalculateStreakFromDates(dates)
            });
        }

        private int CalculateStreakFromDates(List<DateTime> dates)
        {
            if (dates == null || !dates.Any()) return 0;

            var distinctDates = dates
                .Select(d => d.Date)
                .Distinct()
                .OrderByDescending(d => d)
                .ToList();

            var today = DateTime.UtcNow.Date;
            var streak = 0;
            var currentDayToCheck = distinctDates.First();

            if (currentDayToCheck < today.AddDays(-1)) return 0;

            foreach (var date in distinctDates)
            {
                if (date == today.AddDays(-streak) || date == today.AddDays(-streak - 1))
                {
                    streak++;
                }
                else
                {
                    break;
                }
            }

            return streak;
        }
    }

    public record UpdateUserDto(string DisplayName);
}