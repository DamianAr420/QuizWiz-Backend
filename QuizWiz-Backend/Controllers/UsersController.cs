using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizWiz_Backend.Data;
using QuizWiz_Backend.DTOs;
using System.Security.Claims;

namespace QuizWiz_Backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IImageService _imageService;
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context, IImageService imageService)
        {
            _imageService = imageService;
            _context = context;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = GetCurrentUserId();

            var user = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => new UserDto(
                    u.Id,
                    u.DisplayName,
                    u.Email,
                    u.Role,
                    u.CloudinaryPublicId,
                    u.SelectedFrame,
                    u.SelectedBackground,
                    u.CreatedAt,
                    u.Points,
                    u.Experience,
                    u.Level
                ))
                .FirstOrDefaultAsync();

            if (user == null) return NotFound();

            return Ok(user);
        }

        [HttpPut("update-profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserDto dto)
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);

            if (user == null) return NotFound(new { code = "USER_NOT_FOUND" });

            user.DisplayName = dto.DisplayName;
            user.SelectedFrame = dto.SelectedFrame;
            user.SelectedBackground = dto.SelectedBackground;

            await _context.SaveChangesAsync();

            return Ok(new UserDto(
                user.Id,
                user.DisplayName,
                user.Email,
                user.Role,
                user.CloudinaryPublicId,
                user.SelectedFrame,
                user.SelectedBackground,
                user.CreatedAt,
                user.Points,
                user.Experience,
                user.Level
            ));
        }

        [HttpDelete("delete-account")]
        public async Task<IActionResult> DeleteAccount()
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);

            if (user == null) return NotFound();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetUserStats()
        {
            var userId = GetCurrentUserId();

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

        [HttpPost("upload-avatar")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);

            if (user == null) return NotFound(new { code = "USER_NOT_FOUND" });

            if (file == null || file.Length == 0) return BadRequest(new { code = "INVALID_FILE" });

            if (!string.IsNullOrEmpty(user.CloudinaryPublicId))
            {
                await _imageService.DeleteImageAsync(user.CloudinaryPublicId);
            }

            var uploadResult = await _imageService.UploadImageAsync(file);
            if (uploadResult.Error != null) return BadRequest(new { code = "UPLOAD_FAILED" });

            user.CloudinaryPublicId = uploadResult.PublicId;
            await _context.SaveChangesAsync();

            return Ok(new { publicId = user.CloudinaryPublicId });
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
            var firstDate = distinctDates.First();

            if (firstDate < today.AddDays(-1)) return 0;

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

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new UnauthorizedAccessException("Brak poprawnego identyfikatora użytkownika.");
            }
            return userId;
        }
    }
}