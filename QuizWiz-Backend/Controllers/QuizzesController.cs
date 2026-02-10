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
                    q.Id, q.Title, q.Description, q.Questions.Count,
                    q.TimeLimitSeconds, q.IsOfficial, q.IsVisible, q.IsPlayable, q.AuthorId))
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
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
                return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            var quiz = await _context.Quizzes.FindAsync(id);
            if (quiz == null) return NotFound();

            int expPerCorrect = 25;
            int pointsPerCorrect = 5;
            int perfectScoreBonus = 100;

            int gainedExp = dto.Score * expPerCorrect;
            int gainedPoints = dto.Score * pointsPerCorrect;

            if (dto.Score == dto.TotalQuestions && dto.TotalQuestions > 0)
            {
                gainedExp += perfectScoreBonus;
                gainedPoints += 25;
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
                newTotalPoints = user.Points,
                newExperience = user.Experience,
                currentLevel = user.Level,
                isLevelUp = user.Level > levelBefore
            });
        }
    }
}