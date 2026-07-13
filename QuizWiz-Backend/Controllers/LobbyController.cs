using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizWiz_Backend.Classes;
using QuizWiz_Backend.Data;
using QuizWiz_Backend.DTOs;
using System.Security.Claims;

namespace QuizWiz_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LobbyController(AppDbContext context) : ControllerBase
    {
        private readonly AppDbContext _context = context;

        [HttpPost("create")]
        public async Task<IActionResult> CreateLobby([FromBody] CreateLobbyDto dto)
        {
            if (dto.QuestionCount < 1 || dto.QuestionCount > 50)
                return BadRequest("Nieprawidłowa liczba pytań.");

            var userId = GetCurrentUserId();

            var lobby = new Lobby
            {
                Name = dto.Name,
                HostId = userId,
                MaxPlayers = dto.MaxPlayers,
                QuestionCount = dto.QuestionCount,
                IsPrivate = dto.IsPrivate,
                QuizId = dto.QuizId,
                Status = "Waiting"
            };

            _context.Lobbies.Add(lobby);

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return Unauthorized();

            var hostPlayer = new LobbyPlayer
            {
                LobbyId = lobby.Id,
                UserId = userId,
                DisplayName = user.DisplayName,
                IsReady = true,
                CloudinaryPublicId = user.CloudinaryPublicId ?? string.Empty,
                SelectedFrame = user.SelectedFrame ?? string.Empty,
                SelectedBackground = user.SelectedBackground ?? string.Empty
            };

            _context.LobbyPlayers.Add(hostPlayer);
            await _context.SaveChangesAsync();

            return Ok(await GetLobbyInternal(lobby.Id));
        }

        [HttpPost("join/{id}")]
        public async Task<IActionResult> JoinLobby(Guid id)
        {
            var userId = GetCurrentUserId();

            var lobby = await _context.Lobbies
                .Include(l => l.Players)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lobby == null)
                return NotFound(new { code = "LOBBY_NOT_FOUND" });

            if (lobby.Players.Any(p => p.UserId == userId))
                return Ok(await GetLobbyInternal(id));

            if (lobby.Status != "Waiting")
                return BadRequest(new { code = "GAME_ALREADY_STARTED" });

            if (lobby.Players.Count >= lobby.MaxPlayers)
                return BadRequest(new { code = "LOBBY_FULL" });

            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return Unauthorized();

            var newPlayer = new LobbyPlayer
            {
                LobbyId = lobby.Id,
                UserId = userId,
                DisplayName = user.DisplayName,
                IsReady = false,
                CloudinaryPublicId = user.CloudinaryPublicId ?? string.Empty,
                SelectedFrame = user.SelectedFrame ?? string.Empty,
                SelectedBackground = user.SelectedBackground ?? string.Empty
            };

            _context.LobbyPlayers.Add(newPlayer);
            await _context.SaveChangesAsync();

            return Ok(await GetLobbyInternal(id));
        }

        [HttpGet("public")]
        public async Task<ActionResult<List<LobbySummaryDto>>> GetPublicLobbies()
        {
            var userId = GetCurrentUserId();
            var lobbies = await _context.Lobbies
                .Where(l => !l.IsPrivate && l.Status == "Waiting")
                .Select(l => new LobbySummaryDto(
                    l.Id,
                    l.Name,
                    l.Players.Count,
                    l.MaxPlayers,
                    l.QuestionCount,
                    _context.Users
                        .Where(u => u.Id == l.HostId)
                        .Select(u => u.DisplayName)
                        .FirstOrDefault() ?? "Unknown",
                    l.Players.Any(p => p.UserId == userId)
                ))
                .ToListAsync();

            return Ok(lobbies);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetLobby(Guid id)
        {
            var lobby = await GetLobbyInternal(id);
            if (lobby == null) return NotFound();
            return Ok(lobby);
        }

        private async Task<LobbyDto?> GetLobbyInternal(Guid id)
        {
            return await _context.Lobbies
                .Where(l => l.Id == id)
                .Select(l => new LobbyDto(
                    l.Id,
                    l.Name,
                    l.HostId,
                    l.QuizId,
                    l.MaxPlayers,
                    l.QuestionCount,
                    l.IsPrivate,
                    l.Status,
                    l.Players.Join(_context.Users,
                        lp => lp.UserId,
                        u => u.Id,
                        (lp, u) => new LobbyPlayerDto(
                            u.Id,
                            u.DisplayName,
                            lp.IsReady,
                            lp.CloudinaryPublicId,
                            u.SelectedFrame,
                            u.SelectedBackground,
                            lp.Score,
                            lp.Progress
                        )).ToList()
                ))
                .FirstOrDefaultAsync();
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdClaim, out int id) ? id : 0;
        }
    }
}