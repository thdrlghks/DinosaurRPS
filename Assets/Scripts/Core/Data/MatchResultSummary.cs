using System;
using System.Collections.Generic;
using Core.Enums;

namespace Core.Data
{
    /// <summary>A snapshot of the rounds actually recorded for one match.</summary>
    public sealed class MatchResultSummary
    {
        public sealed class HandPerformance
        {
            public HandType Hand { get; }
            public int Attempts { get; }
            public int Wins { get; }

            /// <summary>Percentage (0–100), including draws in attempts. Check Attempts before displaying.</summary>
            public float WinRate => Attempts > 0 ? 100f * Wins / Attempts : 0f;

            internal HandPerformance(HandType hand, int attempts, int wins)
            {
                Hand = hand;
                Attempts = attempts;
                Wins = wins;
            }
        }

        public int TotalRounds { get; }
        public int Wins { get; }
        public int Loses { get; }
        public int Draws { get; }
        public int MaxConsecutiveWins { get; }
        public float WinRate => TotalRounds > 0 ? 100f * Wins / TotalRounds : 0f;
        public IReadOnlyList<HandPerformance> Hands { get; }
        public IReadOnlyList<HandType> MostUsedHands { get; }

        /// <summary>Null for no rounds or a tie; MostUsedHands preserves every tied hand.</summary>
        public HandType? MostUsedHand => MostUsedHands.Count == 1 ? MostUsedHands[0] : (HandType?)null;
        public HandType? BestHand { get; }
        public string MostUsedHandText { get; }
        public string BestHandText => BestHand.HasValue ? GetHandName(BestHand.Value) : "기록 없음";
        public string PersonalityText { get; }

        public MatchResultSummary(MatchRecord record)
        {
            var handTypes = (HandType[])Enum.GetValues(typeof(HandType));
            var attempts = new Dictionary<HandType, int>();
            var wins = new Dictionary<HandType, int>();
            foreach (var hand in handTypes)
            {
                attempts.Add(hand, 0);
                wins.Add(hand, 0);
            }

            int consecutiveWins = 0;
            if (record != null)
            {
                foreach (var round in record.Rounds)
                {
                    TotalRounds++;
                    attempts[round.PlayerHand]++;
                    switch (round.Result)
                    {
                        case GameResult.Win:
                            Wins++;
                            wins[round.PlayerHand]++;
                            consecutiveWins++;
                            MaxConsecutiveWins = Math.Max(MaxConsecutiveWins, consecutiveWins);
                            break;
                        case GameResult.Lose:
                            Loses++;
                            consecutiveWins = 0;
                            break;
                        case GameResult.Draw:
                            Draws++;
                            consecutiveWins = 0;
                            break;
                        default:
                            throw new ArgumentException("A round contains an unknown result.", nameof(record));
                    }
                }
            }

            var performances = new List<HandPerformance>(handTypes.Length);
            var mostUsed = new List<HandType>();
            int mostAttempts = 0;
            HandPerformance best = null;
            foreach (var hand in handTypes)
            {
                var performance = new HandPerformance(hand, attempts[hand], wins[hand]);
                performances.Add(performance);
                if (performance.Attempts == 0) continue;

                if (performance.Attempts > mostAttempts)
                {
                    mostAttempts = performance.Attempts;
                    mostUsed.Clear();
                }
                if (performance.Attempts == mostAttempts) mostUsed.Add(hand);
                if (best == null || IsBetter(performance, best)) best = performance;
            }

            Hands = performances.AsReadOnly();
            MostUsedHands = mostUsed.AsReadOnly();
            BestHand = best?.Hand;
            var handNames = mostUsed.ConvertAll(GetHandName);
            MostUsedHandText = mostUsed.Count == 0 ? "기록 없음"
                : string.Join(" · ", handNames) + (mostUsed.Count > 1 ? " (동률)" : "");
            PersonalityText = TotalRounds == 0 ? "아직 경기 기록이 없어요."
                : mostUsed.Count > 1 ? "여러 손을 고르게 쓰는 균형형 승부사"
                : mostUsed[0] switch
                {
                    HandType.Rock => "바위를 믿는 뚝심 있는 승부사",
                    HandType.Paper => "보자기로 흐름을 감싸는 유연한 승부사",
                    HandType.Scissors => "가위로 기회를 노리는 날카로운 승부사",
                    _ => "나만의 손을 고르는 승부사"
                };
        }

        public HandPerformance GetHand(HandType hand)
        {
            foreach (var performance in Hands)
                if (performance.Hand == hand) return performance;
            throw new ArgumentOutOfRangeException(nameof(hand), hand, "Unknown hand.");
        }

        public static string GetHandName(HandType hand) => hand switch
        {
            HandType.Rock => "바위",
            HandType.Paper => "보자기",
            HandType.Scissors => "가위",
            _ => hand.ToString()
        };

        private static bool IsBetter(HandPerformance candidate, HandPerformance current)
        {
            // Compare exact fractions so equal rates cannot diverge through float rounding.
            long candidateRate = (long)candidate.Wins * current.Attempts;
            long currentRate = (long)current.Wins * candidate.Attempts;
            if (candidateRate != currentRate) return candidateRate > currentRate;
            if (candidate.Wins != current.Wins) return candidate.Wins > current.Wins;
            if (candidate.Attempts != current.Attempts) return candidate.Attempts > current.Attempts;
            return (int)candidate.Hand < (int)current.Hand;
        }
    }
}
