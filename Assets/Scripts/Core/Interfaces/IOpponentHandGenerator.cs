using Core.Enums;

namespace Core.Interfaces
{
    public interface IOpponentHandGenerator
    {
        HandType GenerateOpponentHand(HandType playerHand, TournamentStage stage, bool hasPlayerWonOnce);
    }
}