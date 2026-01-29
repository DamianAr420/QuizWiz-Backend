using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizWiz_Backend.Data;
using QuizWiz_Backend.DTOs;

namespace QuizWiz_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StatsController(AppDbContext context) => _context = context;

        [HttpGet]
        public async Task<ActionResult<StatsDto>> GetGeneralStats()
        {
            var quizzesCount = await _context.Quizzes.CountAsync();
            var questionsCount = await _context.Questions.CountAsync();
            var usersCount = await _context.Users.CountAsync();

            return Ok(new StatsDto(quizzesCount, questionsCount, usersCount));
        }
    }
}