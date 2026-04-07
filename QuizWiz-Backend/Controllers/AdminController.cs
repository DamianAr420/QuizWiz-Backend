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
    public class AdminController(AppDbContext context) : ControllerBase
    {
        [HttpGet("users")]
        public async Task<ActionResult<PagedResultDto<User>>> GetAllUsers(
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var pattern = $"%{search}%";
                query = query.Where(u =>
                    EF.Functions.Like(u.DisplayName, pattern) ||
                    EF.Functions.Like(u.Email, pattern));
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var items = await query
                .OrderBy(u => u.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new PagedResultDto<User>
            {
                Items = items,
                TotalCount = totalCount,
                TotalPages = totalPages,
                CurrentPage = page
            });
        }

        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UserAdminUpdateDto dto)
        {
            var user = await context.Users.FindAsync(id);
            if (user == null) return NotFound("Użytkownik nie istnieje.");

            user.DisplayName = dto.DisplayName;
            user.Email = dto.Email;
            user.Role = dto.Role;
            user.Points = dto.Points;
            user.Experience = dto.Experience;

            await context.SaveChangesAsync();
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

            context.ShopItems.Add(newItem);
            await context.SaveChangesAsync();

            return CreatedAtAction(nameof(CreateShopItem), new { id = newItem.Id }, newItem);
        }

        [HttpPut("shop/{id}")]
        public async Task<IActionResult> UpdateShopItem(int id, [FromBody] ShopItemCreateDto dto)
        {
            var item = await context.ShopItems.FindAsync(id);
            if (item == null) return NotFound("Przedmiot nie istnieje.");

            item.Title = dto.Title;
            item.Description = dto.Description;
            item.Price = dto.Price;
            item.Type = (ItemType)dto.Type;
            item.Rarity = (ItemRarity)dto.Rarity;
            item.ImageUrl = dto.ImageUrl;
            item.StockQuantity = dto.StockQuantity;
            item.RequiredLevel = dto.RequiredLevel;

            await context.SaveChangesAsync();
            return Ok(item);
        }

        [HttpDelete("shop/{id}")]
        public async Task<IActionResult> DeleteShopItem(int id)
        {
            var item = await context.ShopItems.FindAsync(id);
            if (item == null) return NotFound();

            context.ShopItems.Remove(item);
            await context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("quizzes/pending")]
        public async Task<ActionResult<IEnumerable<Quiz>>> GetPendingQuizzes()
        {
            return await context.Quizzes
                .Where(q => q.IsSubmitted && !q.IsVerified)
                .OrderByDescending(q => q.CreatedAt)
                .Include(q => q.Questions)
                .ToListAsync();
        }

        [HttpPut("quizzes/{id}/verify")]
        public async Task<IActionResult> VerifyQuiz(int id)
        {
            var quiz = await context.Quizzes.FindAsync(id);
            if (quiz == null) return NotFound("Quiz nie istnieje.");

            quiz.IsVerified = true;

            await context.SaveChangesAsync();
            return Ok(new { message = "Quiz został pomyślnie zweryfikowany." });
        }

        [HttpDelete("quizzes/{id}/reject")]
        public async Task<IActionResult> RejectQuiz(int id)
        {
            var quiz = await context.Quizzes.FindAsync(id);
            if (quiz == null) return NotFound("Quiz nie istnieje.");

            quiz.IsSubmitted = false;
            quiz.IsVerified = false;

            await context.SaveChangesAsync();

            return Ok(new { message = "Quiz został odrzucony i przywrócony do edycji dla autora." });
        }
    }
}