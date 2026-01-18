using System;
using Core.Enums;

namespace Core.Data
{
    [Serializable]
    public class RoundRecord
    {
        public HandType PlayerHand { get; private set; }
        public HandType OpponentHand { get; private set; }
        public GameResult Result { get; private set; }
        public int RoundNumber { get; private set; }
        public DateTime Timestamp { get; private set; }

        public RoundRecord(HandType playerHand, HandType opponentHand, 
            GameResult result, int roundNumber)
        {
            PlayerHand = playerHand;
            OpponentHand = opponentHand;
            Result = result;
            RoundNumber = roundNumber;
            Timestamp = DateTime.Now;
        }

        public override string ToString()
        {
            return $"R{RoundNumber}: {PlayerHand} vs {OpponentHand} = {Result}";
        }
    }
}