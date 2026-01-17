using Core.Enums;

namespace DefaultNamespace
{
    public interface IGameJudge
    {
        GameResult DetermineResult(HandType playerHand, HandType opponentHand);
        HandType GetWinningHand(HandType hand);
        HandType GetLosingHand(HandType hand);
    }
}