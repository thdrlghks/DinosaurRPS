using Core.Enums;

namespace Core.Rules
{
    public static class TournamentRules
    {
        public static int GetRequiredWinsForStage(TournamentStage stage)
        {
            return stage switch
            {
                TournamentStage.Qualifiers => 1,   // 튜토리얼: 1승이면 종료 (닭 1방)
                TournamentStage.QuarterFinals => 2, // 8강: 2점제 (체력 2와 일치)
                TournamentStage.SemiFinals => 2,
                TournamentStage.Finals => 2,
                TournamentStage.GrandFinals => 1,
                _ => 2
            };
        }

        public static bool CanLoseInSemiFinals(bool hasPlayerWonOnce)
        {
            return hasPlayerWonOnce;
        }

        public static bool IsMatchComplete(int playerWins, int opponentWins, TournamentStage stage)
        {
            var requiredWins = GetRequiredWinsForStage(stage);
            return playerWins >= requiredWins || opponentWins >= requiredWins;
        }

        public static GameResult? DetermineWinner(int playerWins, int opponentWins, TournamentStage stage)
        {
            if (!IsMatchComplete(playerWins, opponentWins, stage))
                return null;

            var requiredWins = GetRequiredWinsForStage(stage);

            if (playerWins >= requiredWins)
                return GameResult.Win;
            else if (opponentWins >= requiredWins)
                return GameResult.Lose;

            return null;
        }
    }
}