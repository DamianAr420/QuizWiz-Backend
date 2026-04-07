using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuizWiz_Backend.Classes;
using QuizWiz_Backend.Data;
using QuizWiz_Backend.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace QuizWiz_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(AppDbContext context, IConfiguration config) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var email = dto.Email.Trim();
        var displayName = dto.DisplayName.Trim();

        if (await context.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower()))
            return BadRequest(new { message = "Ten adres email jest już zajęty." });

        if (await context.Users.AnyAsync(u => u.DisplayName.ToLower() == displayName.ToLower()))
            return BadRequest(new { message = "Ta nazwa użytkownika jest już zajęta." });

        var user = new User
        {
            DisplayName = displayName,
            Email = email.ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = "User",
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var token = CreateToken(user);
        return Ok(new AuthResponseDto(MapToUserDto(user), token));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var identifier = dto.Identifier.Trim();

        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == identifier.ToLower()
                                   || u.DisplayName.ToLower() == identifier.ToLower());

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized(new { message = "Nieprawidłowe dane logowania." });

        var token = CreateToken(user);
        return Ok(new AuthResponseDto(MapToUserDto(user), token));
    }

    private static UserDto MapToUserDto(User user)
    {
        return new(
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
        );
    }

    private string CreateToken(User user)
    {
        var claims = new List<Claim> {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Role, user.Role)
        };

        var tokenKey = config["AppSettings:Token"]
            ?? throw new InvalidOperationException("JWT Token Key is missing!");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = creds
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }
}