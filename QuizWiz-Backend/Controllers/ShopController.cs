using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizWiz_Backend.Classes;
using QuizWiz_Backend.Data;
using System.Security.Claims;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ShopController : ControllerBase
{
    private readonly AppDbContext _context;

    public ShopController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ShopItem>>> GetShopItems()
    {
        return await _context.ShopItems
            .Where(s => (!s.StockQuantity.HasValue || s.StockQuantity > 0) && !string.IsNullOrEmpty(s.Title))
            .ToListAsync();
    }

    [HttpPost("purchase/{itemId}")]
    public async Task<IActionResult> PurchaseItem(int itemId)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        var item = await _context.ShopItems.FindAsync(itemId);

        if (user == null) return NotFound(new { code = "USER_NOT_FOUND" });
        if (item == null) return NotFound(new { code = "ITEM_NOT_FOUND" });

        if (user.Points < item.Price)
            return BadRequest(new { code = "INSUFFICIENT_FUNDS" });

        var alreadyOwned = await _context.UserInventories
            .AnyAsync(ui => ui.UserId == userId && ui.ShopItemId == itemId);
        if (alreadyOwned)
            return BadRequest(new { code = "ITEM_ALREADY_OWNED" });

        user.Points -= item.Price;

        _context.UserInventories.Add(new UserInventory
        {
            UserId = userId,
            ShopItemId = itemId
        });

        await _context.SaveChangesAsync();

        return Ok(new { message = "Zakup udany", points = user.Points });
    }

    [HttpGet("my-inventory")]
    public async Task<IActionResult> GetMyInventory()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            return Unauthorized();

        var inventory = await _context.UserInventories
            .Where(ui => ui.UserId == userId)
            .Include(ui => ui.ShopItem)
            .ToListAsync();

        return Ok(inventory);
    }
}