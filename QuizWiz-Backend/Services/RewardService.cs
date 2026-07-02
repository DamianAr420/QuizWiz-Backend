using QuizWiz_Backend.Classes;

namespace QuizWiz_Backend.Services
{
    public class RewardService
    {
        public RewardResult CalculateRewards(
            int correctAnswers,
            int totalQuestions,
            bool isOfficial,
            bool isVerified,
            bool alreadyCompletedToday,
            bool isWinner = false)
        {
            double typeMultiplier = (isOfficial || isVerified) ? 1.0 : 0.2;

            double repeatMultiplier = alreadyCompletedToday ? 0.4 : 1.0;
            bool canGetPerfectBonus = !alreadyCompletedToday;

            const int expPerCorrect = 25;
            const int pointsPerCorrect = 5;
            const int perfectExpBonus = 100;
            const int perfectPointsBonus = 25;

            int gainedExp = (int)(correctAnswers * expPerCorrect * typeMultiplier * repeatMultiplier);
            int gainedPoints = (int)(correctAnswers * pointsPerCorrect * typeMultiplier * repeatMultiplier);

            if (canGetPerfectBonus &&
                correctAnswers == totalQuestions &&
                totalQuestions >= 3)
            {
                gainedExp += (int)(perfectExpBonus * typeMultiplier);
                gainedPoints += (int)(perfectPointsBonus * typeMultiplier);
            }

            if (isWinner)
            {
                gainedExp += 100;
                gainedPoints += 20;
            }

            return new RewardResult
            {
                Experience = gainedExp,
                Points = gainedPoints
            };
        }
    }
}