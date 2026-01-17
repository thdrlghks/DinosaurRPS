namespace DefaultNamespace
{
    using Core.Enums;
    using Core.Interfaces;
    using UnityEngine;

    namespace Gameplay
    {
        public class GameJudge : MonoBehaviour, IGameJudge
        {
            public GameResult DetermineResult(HandType playerHand, HandType opponentHand)
            {
                if (playerHand == opponentHand)
                {
                    Debug.Log($"[GameJudge] Draw! Both chose {playerHand}");
                    return GameResult.Draw;
                }

                bool playerWins = IsWinningHand(playerHand, opponentHand);

                if (playerWins)
                {
                    Debug.Log($"[GameJudge] Player wins! {playerHand} beats {opponentHand}");
                    return GameResult.Win;
                }
                else
                {
                    Debug.Log($"[GameJudge] Player loses! {opponentHand} beats {playerHand}");
                    return GameResult.Lose;
                }
            }


            private bool IsWinningHand(HandType firstHand, HandType secondHand)
            {
                return (firstHand == HandType.Rock && secondHand == HandType.Scissors) || 
                       (firstHand == HandType.Paper && secondHand == HandType.Rock) || 
                       (firstHand == HandType.Scissors && secondHand == HandType.Paper); 
            }


            public HandType GetWinningHand(HandType hand)
            {
                return hand switch
                {
                    HandType.Rock => HandType.Paper,
                    HandType.Paper => HandType.Scissors,
                    HandType.Scissors => HandType.Rock,
                    _ => HandType.Rock
                };
            }


            public HandType GetLosingHand(HandType hand)
            {
                return hand switch
                {
                    HandType.Rock => HandType.Scissors,
                    HandType.Paper => HandType.Rock,
                    HandType.Scissors => HandType.Paper,
                    _ => HandType.Scissors
                };
            }


            public string GetRuleDescription(HandType playerHand, HandType opponentHand)
            {
                if (playerHand == opponentHand)
                    return $"{playerHand} vs {opponentHand} = Draw";

                bool playerWins = IsWinningHand(playerHand, opponentHand);

                if (playerWins)
                {
                    return playerHand switch
                    {
                        HandType.Rock => "Rock crushes Scissors!",
                        HandType.Paper => "Paper covers Rock!",
                        HandType.Scissors => "Scissors cuts Paper!",
                        _ => "Player wins!"
                    };
                }
                else
                {
                    return opponentHand switch
                    {
                        HandType.Rock => "Rock crushes Scissors...",
                        HandType.Paper => "Paper covers Rock...",
                        HandType.Scissors => "Scissors cuts Paper...",
                        _ => "Player loses..."
                    };
                }
            }


            public void LogGameResult(HandType playerHand, HandType opponentHand, GameResult result)
            {
                string resultColor = result switch
                {
                    GameResult.Win => "green",
                    GameResult.Lose => "red",
                    GameResult.Draw => "yellow",
                    _ => "white"
                };

                Debug.Log(
                    $"<color={resultColor}>[Game Result] Player: {playerHand} vs Opponent: {opponentHand} = {result}</color>");
                Debug.Log($"<color={resultColor}>{GetRuleDescription(playerHand, opponentHand)}</color>");
            }
        }
    }
}