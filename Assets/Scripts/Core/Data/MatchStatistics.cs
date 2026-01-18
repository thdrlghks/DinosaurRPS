using System;
using System.Collections.Generic;
using Core.Enums;

namespace Core.Data
{
    [Serializable]
    public struct MatchStatistics
    {
        public int TotalRounds;
        public int Wins;
        public int Loses;
        public int Draws;

        public int RockCount;
        public int PaperCount;
        public int ScissorsCount;

        // 승률
        public float WinRate => TotalRounds > 0 ? (float)Wins / TotalRounds * 100f : 0f;
        public float LoseRate => TotalRounds > 0 ? (float)Loses / TotalRounds * 100f : 0f;
        public float DrawRate => TotalRounds > 0 ? (float)Draws / TotalRounds * 100f : 0f;

        // 손 사용 비율
        public float RockPercentage => TotalRounds > 0 ? (float)RockCount / TotalRounds * 100f : 0f;
        public float PaperPercentage => TotalRounds > 0 ? (float)PaperCount / TotalRounds * 100f : 0f;
        public float ScissorsPercentage => TotalRounds > 0 ? (float)ScissorsCount / TotalRounds * 100f : 0f;

        // 가장 많이/적게 낸 손
        public HandType MostUsedHand
        {
            get
            {
                if (RockCount >= PaperCount && RockCount >= ScissorsCount) return HandType.Rock;
                if (PaperCount >= ScissorsCount) return HandType.Paper;
                return HandType.Scissors;
            }
        }

        public HandType LeastUsedHand
        {
            get
            {
                if (RockCount <= PaperCount && RockCount <= ScissorsCount) return HandType.Rock;
                if (PaperCount <= ScissorsCount) return HandType.Paper;
                return HandType.Scissors;
            }
        }

        public Dictionary<HandType, int> HandCounts => new()
        {
            { HandType.Rock, RockCount },
            { HandType.Paper, PaperCount },
            { HandType.Scissors, ScissorsCount }
        };

        public override string ToString()
        {
            return $"전적: {Wins}승 {Loses}패 {Draws}무 (승률 {WinRate:F1}%)\n" +
                   $"손 사용: 바위 {RockCount}회({RockPercentage:F1}%), " +
                   $"보 {PaperCount}회({PaperPercentage:F1}%), " +
                   $"가위 {ScissorsCount}회({ScissorsPercentage:F1}%)\n" +
                   $"가장 많이 낸 손: {MostUsedHand}";
        }
    }
}