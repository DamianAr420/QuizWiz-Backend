using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizWiz_Backend.Classes;
using QuizWiz_Backend.Data;
using QuizWiz_Backend.DTOs;

namespace QuizWiz_Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RankingController(AppDbContext db) : ControllerBase
{
    [HttpGet("{type}")]
    public async Task<IActionResult> GetRanking(string type)
    {
        return type.ToLowerInvariant() switch
        {
            "experience" => Ok(await GetTopByExperience()),
            "points" => Ok(await GetTopByPoints()),
            "correctanswers" => Ok(await GetTopByCorrectAnswers()),
            "accuracy" => Ok(await GetTopByAccuracy()),
            "quizzescompleted" => Ok(await GetTopByQuizzesCompleted()),
            _ => BadRequest("Nieznany ranking.")
        };
    }

    private async Task<List<RankingExperienceDto>> GetTopByExperience() =>
        await db.Users
            .OrderByDescending(u => u.Experience)
            .Select(u => new RankingExperienceDto(u.Id, u.DisplayName, u.Experience, u.Level, u.CloudinaryPublicId))
            .Take(20)
            .ToListAsync();

    private async Task<List<RankingPointsDto>> GetTopByPoints() =>
        await db.Users
            .OrderByDescending(u => u.Points)
            .Select(u => new RankingPointsDto(u.Id, u.DisplayName, u.Points, u.CloudinaryPublicId))
            .Take(20)
            .ToListAsync();

    private async Task<List<RankingCorrectAnswersDto>> GetTopByCorrectAnswers() =>
        await db.QuizAttempts
            .GroupBy(a => a.UserId)
            .Select(g => new { UserId = g.Key, CorrectAnswers = g.Sum(x => x.Score) })
            .OrderByDescending(x => x.CorrectAnswers)
            .Join(db.Users, x => x.UserId, u => u.Id, (x, u) => new RankingCorrectAnswersDto(u.Id, u.DisplayName, x.CorrectAnswers, u.CloudinaryPublicId))
            .Take(20)
            .ToListAsync();

    private async Task<List<RankingAccuracyDto>> GetTopByAccuracy() =>
        await db.QuizAttempts
            .GroupBy(a => a.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                Accuracy = g.Sum(x => x.TotalQuestions) > 0
                    ? (g.Sum(x => x.Score) * 1.0) / g.Sum(x => x.TotalQuestions)
                    : 0
            })
            .OrderByDescending(x => x.Accuracy)
            .Join(db.Users, x => x.UserId, u => u.Id, (x, u) => new RankingAccuracyDto(
                u.Id,
                u.DisplayName,
                Math.Round(x.Accuracy * 100, 2),
                u.CloudinaryPublicId
            ))
            .Take(20)
            .ToListAsync();

    private async Task<List<RankingCompletedDto>> GetTopByQuizzesCompleted() =>
        await db.QuizAttempts
            .GroupBy(a => a.UserId)
            .Select(g => new { UserId = g.Key, Completed = g.Count() })
            .OrderByDescending(x => x.Completed)
            .Join(db.Users, x => x.UserId, u => u.Id, (x, u) => new RankingCompletedDto(u.Id, u.DisplayName, x.Completed, u.CloudinaryPublicId))
            .Take(20)
            .ToListAsync();
}
