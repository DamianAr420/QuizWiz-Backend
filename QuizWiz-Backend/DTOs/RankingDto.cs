namespace QuizWiz_Backend.DTOs;

public record RankingExperienceDto(int Id, string DisplayName, int Experience, int Level, string? CloudinaryPublicId);
public record RankingPointsDto(int Id, string DisplayName, int Points, string? CloudinaryPublicId);
public record RankingCorrectAnswersDto(int Id, string DisplayName, int CorrectAnswers, string? CloudinaryPublicId);
public record RankingAccuracyDto(int Id, string DisplayName, double Accuracy, string? CloudinaryPublicId);
public record RankingCompletedDto(int Id, string DisplayName, int Completed, string? CloudinaryPublicId);