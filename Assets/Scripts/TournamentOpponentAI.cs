using Core.Enums;
using Core.Interfaces;
using UnityEngine;

namespace Gameplay
{
    public class TournamentOpponentAI : MonoBehaviour, IOpponentHandGenerator
    {
        public HandType GenerateOpponentHand(HandType playerHand, TournamentStage stage, bool hasPlayerWonOnce)
        {
            if (stage == TournamentStage.Qualifiers)
            {
                return GetLosingHand(playerHand);
            }
            
            if (stage == TournamentStage.QuarterFinals)
            {
                return GetNonWinningHand(playerHand);
            }
            
            if (stage == TournamentStage.SemiFinals && !hasPlayerWonOnce)
            {
                return GetNonWinningHand(playerHand);
            }
            
            return GetRandomHand();
        }
        
        private HandType GetLosingHand(HandType playerHand)
        {
            return playerHand switch
            {
                HandType.Rock => HandType.Scissors,      
                HandType.Paper => HandType.Rock,        
                HandType.Scissors => HandType.Paper,     
                _ => HandType.Rock
            };
        }
        
        private HandType GetNonWinningHand(HandType playerHand)
        {
            int choice = Random.Range(0, 3);

            if (choice == 0)
            {
                return playerHand; // 비김 (33%)
            }
            else
            {
                return GetLosingHand(playerHand); // 플레이어 승 (66%)
            }
        }
        
        private HandType GetRandomHand()
        {
            return (HandType)Random.Range(0, 3);
        }
    }
}