using Core.Enums;

namespace Gameplay
{
    public interface IGameJudge
    {
        GameResult DetermineResult(HandType playerHand, HandType opponentHand);
        HandType GetWinningHand(HandType hand);
        HandType GetLosingHand(HandType hand);
        string GetRuleDescription(HandType playerHand, HandType opponentHand);
        void LogGameResult(HandType playerHand, HandType opponentHand, GameResult result);
    }
}