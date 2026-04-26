using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizWiz_Backend.Data;
using QuizWiz_Backend.DTOs;
using System.Security.Claims;

namespace QuizWiz_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatsController(AppDbContext context) : ControllerBase
    {
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new UnauthorizedAccessException("Brak poprawnego identyfikatora użytkownika.");
            }
            return userId;
        }

        [HttpGet]
        public async Task<ActionResult<StatsDto>> GetGeneralStats()
        {
            var quizzesCount = await context.Quizzes.CountAsync();
            var questionsCount = await context.Questions.CountAsync();
            var usersCount = await context.Users.CountAsync();

            return Ok(new StatsDto(quizzesCount, questionsCount, usersCount));
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 5)
        {
            var userId = GetCurrentUserId();

            var query = context.QuizAttempts
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CompletedAt);

            var totalItems = await query.CountAsync();
            var history = await context.QuizAttempts
                .Include(a => a.Quiz)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CompletedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new {
                    a.Id,
                    a.Score,
                    a.TotalQuestions,
                    a.CompletedAt,
                    QuizTitle = a.Quiz!.Title
                })
                .ToListAsync();

            return Ok(new
            {
                items = history,
                totalItems,
                page,
                pageSize
            });
        }
    }
}