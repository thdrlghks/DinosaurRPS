// using DefaultNamespace;
// using Enums;
//
// namespace Strategy
// {
//     public class FinalStrategy : IGameStrategy
//     {
//         private TournamentConfig config;
//     
//         public FinalStrategy(TournamentConfig config)
//         {
//             this.config = config;
//         }
//     
//         // public HandType GetPlayerHand(HandType opponentHand)
//         // {
//         //     var winRate = config.finalWinRate;
//         //     var randomValue = UnityEngine.Random.value;
//         //
//         //     if (randomValue < winRate * 0.6f) 
//         //     {
//         //         return GetWinningHand(opponentHand);
//         //     }
//         //     else if (randomValue < winRate) 
//         //     {
//         //         return opponentHand;
//         //     }
//         //     else 
//         //     {
//         //         return GetLosingHand(opponentHand);
//         //     }
//         // }
//     
//         public RockPaperScissorsResult DetermineResult(HandType playerHand, HandType opponentHand)
//         {
//             return GetGameResult(playerHand, opponentHand);
//         }
//     }
// }