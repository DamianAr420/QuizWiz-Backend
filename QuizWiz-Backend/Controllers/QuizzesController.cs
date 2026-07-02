using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizWiz_Backend.Classes;
using QuizWiz_Backend.Data;
using QuizWiz_Backend.DTOs;
using QuizWiz_Backend.Services;
using System.Security.Claims;

namespace QuizWiz_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuizzesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly RewardService _rewardService;

        public QuizzesController(
            AppDbContext context,
            RewardService rewardService)
        {
            _context = context;
            _rewardService = rewardService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<QuizListDto>>> GetQuizzes()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(userIdString, out int userId);
            var isAdmin = User.IsInRole("Admin");
            var today = DateTime.UtcNow.Date;

            return await _context.Quizzes
                .Where(q => q.IsVisible || isAdmin || (userIdString != null && q.AuthorId == userIdString))
                .Select(q => new QuizListDto(
                    q.Id,
                    q.Title,
                    q.Description,
                    q.Questions.Count,
                    q.TimeLimitSeconds,
                    q.IsOfficial,
                    q.IsVisible,
                    q.IsPlayable,
                    q.AuthorId,
                    q.IsVerified,
                    q.IsSubmitted,
                    userId != 0 && _context.QuizAttempts.Any(a => a.QuizId == q.Id && a.UserId == userId && a.CompletedAt >= today)
                ))
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Quiz>> GetQuiz(int id)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quiz == null) return NotFound();
            return quiz;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateQuiz([FromBody] CreateQuizDto dto)
        {
            var quiz = new Quiz
            {
                Title = dto.Title,
                Description = dto.Description,
                TimeLimitSeconds = dto.TimeLimitSeconds,
                MaxQuestions = dto.MaxQuestions,
                IsOfficial = User.IsInRole("Admin") && dto.IsOfficial,
                IsPlayable = dto.IsPlayable,
                AuthorId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                Questions = dto.Questions.Select(q => new Question
                {
                    Text = q.Text,
                    CorrectAnswer = q.CorrectAnswer,
                    Distractors = q.Distractors
                }).ToList()
            };
            _context.Quizzes.Add(quiz);
            await _context.SaveChangesAsync();
            return Ok(quiz);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateQuiz(int id, [FromBody] UpdateQuizDto dto)
        {
            var quiz = await _context.Quizzes.Include(q => q.Questions).FirstOrDefaultAsync(q => q.Id == id);
            if (quiz == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            if (quiz.AuthorId != userId && !isAdmin)
                return Forbid();

            quiz.Title = dto.Title;
            quiz.Description = dto.Description;
            quiz.TimeLimitSeconds = dto.TimeLimitSeconds;
            quiz.IsVisible = dto.IsVisible;
            quiz.IsPlayable = dto.IsPlayable;

            if (isAdmin)
            {
                quiz.IsOfficial = dto.IsOfficial;
                quiz.IsVerified = dto.IsVerified;
            }
            else
            {
                quiz.IsVerified = false;
                quiz.IsOfficial = false;
            }

            quiz.Questions.Clear();
            quiz.Questions.AddRange(dto.Questions.Select(q => new Question
            {
                Text = q.Text,
                CorrectAnswer = q.CorrectAnswer,
                Distractors = q.Distractors
            }));

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteQuiz(int id)
        {
            var quiz = await _context.Quizzes.FindAsync(id);
            if (quiz == null) return NotFound(new { code = "QUIZ_NOT_FOUND" });

            if (quiz.AuthorId != User.FindFirstValue(ClaimTypes.NameIdentifier) && !User.IsInRole("Admin"))
                return BadRequest(new { code = "FORBIDDEN" });

            _context.Quizzes.Remove(quiz);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("{id}/submit")]
        [Authorize]
        public async Task<IActionResult> SubmitResult(int id, [FromBody] SubmitResultDto dto)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            var quiz = await _context.Quizzes.FindAsync(id);
            if (user == null || quiz == null) return NotFound();

            var today = DateTime.UtcNow.Date;

            bool alreadyDoneToday = await _context.QuizAttempts
                .AnyAsync(a =>
                    a.UserId == userId &&
                    a.QuizId == id &&
                    a.CompletedAt >= today);

            var rewards = _rewardService.CalculateRewards(
                dto.Score,
                dto.TotalQuestions,
                quiz.IsOfficial,
                quiz.IsVerified,
                alreadyDoneToday);

            int gainedExp = rewards.Experience;
            int gainedPoints = rewards.Points;

            int levelBefore = user.Level;
            user.Experience += gainedExp;
            user.Points += gainedPoints;

            var attempt = new QuizAttempt
            {
                UserId = userId,
                QuizId = id,
                Score = dto.Score,
                TotalQuestions = dto.TotalQuestions,
                CompletedAt = DateTime.UtcNow
            };

            _context.QuizAttempts.Add(attempt);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                pointsGained = gainedPoints,
                xpGained = gainedExp,
                isLevelUp = user.Level > levelBefore,
                currentLevel = user.Level,
                totalPoints = user.Points,
                totalExperience = user.Experience
            });
        }

        [HttpPatch("{id}/verify")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> VerifyQuiz(int id, [FromBody] bool isVerified)
        {
            var quiz = await _context.Quizzes.FindAsync(id);
            if (quiz == null) return NotFound();

            quiz.IsVerified = isVerified;
            await _context.SaveChangesAsync();

            return Ok(new { id, isVerified, message = "Status weryfikacji zaktualizowany." });
        }

        [HttpPost("{id}/sendVerifyReq")]
        [Authorize]
        public async Task<IActionResult> SubmitQuiz(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var quiz = await _context.Quizzes.FindAsync(id);
            if (quiz == null) return NotFound(new { code = "QUIZ_NOT_FOUND" });

            if (quiz.AuthorId != userId) return BadRequest(new { code = "FORBIDDEN" });

            if (quiz.IsSubmitted) return BadRequest(new { code = "QUIZ_ALREADY_SUBMITTED" });

            quiz.IsSubmitted = true;
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}