using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizWiz_Backend.Classes;
using QuizWiz_Backend.Data;
using QuizWiz_Backend.DTOs;
using System.Security.Claims;

namespace QuizWiz_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuizzesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public QuizzesController(AppDbContext context) => _context = context;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<QuizListDto>>> GetQuizzes()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            return await _context.Quizzes
                .Where(q =>
                    q.IsVisible ||
                    isAdmin ||
                    (userId != null && q.AuthorId == userId)
                )
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
                    q.IsVerified
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

            if (quiz.AuthorId != User.FindFirstValue(ClaimTypes.NameIdentifier) && !User.IsInRole("Admin"))
                return Forbid();

            quiz.Title = dto.Title;
            quiz.Description = dto.Description;
            quiz.TimeLimitSeconds = dto.TimeLimitSeconds;
            quiz.IsVisible = dto.IsVisible;
            quiz.IsPlayable = dto.IsPlayable;

            if (User.IsInRole("Admin"))
            {
                quiz.IsOfficial = dto.IsOfficial;
                quiz.IsVerified = dto.IsVerified;
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
            if (quiz == null) return NotFound();

            if (quiz.AuthorId != User.FindFirstValue(ClaimTypes.NameIdentifier) && !User.IsInRole("Admin"))
                return Forbid();

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

            double typeMultiplier = (quiz.IsOfficial || quiz.IsVerified) ? 1.0 : 0.2;

            var today = DateTime.UtcNow.Date;
            bool alreadyDoneToday = await _context.QuizAttempts
                .AnyAsync(a => a.UserId == userId && a.QuizId == id && a.CompletedAt >= today);

            double repeatMultiplier = 1.0;
            bool canGetPerfectBonus = true;

            if (alreadyDoneToday)
            {
                repeatMultiplier = 0.4;
                canGetPerfectBonus = false;
            }

            int expPerCorrect = 25;
            int pointsPerCorrect = 5;
            int perfectScoreBonus = 100;

            int gainedExp = (int)(dto.Score * expPerCorrect * typeMultiplier * repeatMultiplier);
            int gainedPoints = (int)(dto.Score * pointsPerCorrect * typeMultiplier * repeatMultiplier);

            if (canGetPerfectBonus && dto.Score == dto.TotalQuestions && dto.TotalQuestions >= 3)
            {
                gainedExp += (int)(perfectScoreBonus * typeMultiplier);
                gainedPoints += (int)(25 * typeMultiplier);
            }

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
    }
}