using System;
using Core.Enums;

namespace Core.Data
{
    [Serializable]
    public struct BattleStatistics
    {
        public int TotalMatches;
        public int TotalRounds;
        
        // 라운드 단위
        public int TotalWins;
        public int TotalLoses;
        public int TotalDraws;

        // 매치 단위
        public int MatchWins;
        public int MatchLoses;

        // 손 사용
        public int RockCount;
        public int PaperCount;
        public int ScissorsCount;

        // 계산 프로퍼티
        public float RoundWinRate => TotalRounds > 0 ? (float)TotalWins / TotalRounds * 100f : 0f;
        public float MatchWinRate => TotalMatches > 0 ? (float)MatchWins / TotalMatches * 100f : 0f;

        public float RockPercentage => TotalRounds > 0 ? (float)RockCount / TotalRounds * 100f : 0f;
        public float PaperPercentage => TotalRounds > 0 ? (float)PaperCount / TotalRounds * 100f : 0f;
        public float ScissorsPercentage => TotalRounds > 0 ? (float)ScissorsCount / TotalRounds * 100f : 0f;

        public HandType MostUsedHand
        {
            get
            {
                if (RockCount >= PaperCount && RockCount >= ScissorsCount) return HandType.Rock;
                if (PaperCount >= ScissorsCount) return HandType.Paper;
                return HandType.Scissors;
            }
        }

        public override string ToString()
        {
            return $"=== 전체 통계 ===\n" +
                   $"총 매치: {TotalMatches}회 ({MatchWins}승 {MatchLoses}패, 승률 {MatchWinRate:F1}%)\n" +
                   $"총 라운드: {TotalRounds}회 ({TotalWins}승 {TotalLoses}패 {TotalDraws}무)\n" +
                   $"라운드 승률: {RoundWinRate:F1}%\n" +
                   $"\n=== 손 사용 통계 ===\n" +
                   $"바위: {RockCount}회 ({RockPercentage:F1}%)\n" +
                   $"보: {PaperCount}회 ({PaperPercentage:F1}%)\n" +
                   $"가위: {ScissorsCount}회 ({ScissorsPercentage:F1}%)\n" +
                   $"가장 많이 낸 손: {MostUsedHand}";
        }
    }
}