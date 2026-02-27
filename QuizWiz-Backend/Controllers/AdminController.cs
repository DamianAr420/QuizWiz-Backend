using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizWiz_Backend.Classes;
using QuizWiz_Backend.Data;
using QuizWiz_Backend.DTOs;

namespace QuizWiz_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("users")]
        public async Task<ActionResult<IEnumerable<User>>> GetAllUsers()
        {
            return await _context.Users.ToListAsync();
        }

        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UserAdminUpdateDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("Użytkownik nie istnieje.");

            user.DisplayName = dto.DisplayName;
            user.Email = dto.Email;
            user.Role = dto.Role;
            user.Points = dto.Points;
            user.Experience = dto.Experience;

            await _context.SaveChangesAsync();
            return Ok(user);
        }

        [HttpPost("shop")]
        public async Task<ActionResult<ShopItem>> CreateShopItem([FromBody] ShopItemCreateDto dto)
        {
            var newItem = new ShopItem
            {
                Title = dto.Title,
                Description = dto.Description,
                Price = dto.Price,
                Type = (ItemType)dto.Type,
                Rarity = (ItemRarity)dto.Rarity,
                ImageUrl = dto.ImageUrl,
                StockQuantity = dto.StockQuantity,
                RequiredLevel = dto.RequiredLevel
            };

            _context.ShopItems.Add(newItem);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(CreateShopItem), new { id = newItem.Id }, newItem);
        }

        [HttpPut("shop/{id}")]
        public async Task<IActionResult> UpdateShopItem(int id, [FromBody] ShopItemCreateDto dto)
        {
            var item = await _context.ShopItems.FindAsync(id);
            if (item == null) return NotFound("Przedmiot nie istnieje.");

            item.Title = dto.Title;
            item.Description = dto.Description;
            item.Price = dto.Price;
            item.Type = (ItemType)dto.Type;
            item.Rarity = (ItemRarity)dto.Rarity;
            item.ImageUrl = dto.ImageUrl;
            item.StockQuantity = dto.StockQuantity;
            item.RequiredLevel = dto.RequiredLevel;

            await _context.SaveChangesAsync();
            return Ok(item);
        }

        [HttpDelete("shop/{id}")]
        public async Task<IActionResult> DeleteShopItem(int id)
        {
            var item = await _context.ShopItems.FindAsync(id);
            if (item == null) return NotFound();

            _context.ShopItems.Remove(item);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("quizzes/pending")]
        public async Task<ActionResult<IEnumerable<Quiz>>> GetPendingQuizzes()
        {
            return await _context.Quizzes
                .Where(q => q.IsSubmitted && !q.IsVerified)
                .OrderByDescending(q => q.CreatedAt)
                .Include(q => q.Questions)
                .ToListAsync();
        }

        [HttpPut("quizzes/{id}/verify")]
        public async Task<IActionResult> VerifyQuiz(int id)
        {
            var quiz = await _context.Quizzes.FindAsync(id);
            if (quiz == null) return NotFound("Quiz nie istnieje.");

            quiz.IsVerified = true;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Quiz został pomyślnie zweryfikowany." });
        }

        [HttpDelete("quizzes/{id}/reject")]
        public async Task<IActionResult> RejectQuiz(int id)
        {
            var quiz = await _context.Quizzes.FindAsync(id);
            if (quiz == null) return NotFound("Quiz nie istnieje.");

            quiz.IsSubmitted = false;
            quiz.IsVerified = false;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Quiz został odrzucony i przywrócony do edycji dla autora." });
        }
    }
}