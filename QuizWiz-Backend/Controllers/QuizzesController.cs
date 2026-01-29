using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using QuizWiz_Backend.Classes;
using QuizWiz_Backend.Data;
using QuizWiz_Backend.DTOs;

namespace QuizWiz_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuizzesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public QuizzesController(AppDbContext context) => _context = context;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<QuizListDto>>> GetQuizzes([FromQuery] bool? official)
        {
            var query = _context.Quizzes.AsQueryable();

            if (official.HasValue)
            {
                query = query.Where(q => q.IsOfficial == official.Value);
            }

            return await query
                .Select(q => new QuizListDto(
                    q.Id,
                    q.Title,
                    q.Description,
                    q.Questions.Count,
                    q.TimeLimitSeconds,
                    q.IsOfficial))
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
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateQuiz([FromBody] CreateQuizDto dto)
        {
            var quiz = new Quiz
            {
                Title = dto.Title,
                Description = dto.Description,
                TimeLimitSeconds = dto.TimeLimitSeconds,
                MaxQuestions = dto.MaxQuestions,
                IsOfficial = dto.IsOfficial,
                Questions = dto.Questions.Select(q => new Question
                {
                    Text = q.Text,
                    CorrectAnswer = q.CorrectAnswer,
                    Distractors = q.Distractors
                }).ToList()
            };

            _context.Quizzes.Add(quiz);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetQuiz), new { id = quiz.Id }, quiz);
        }
    }
}