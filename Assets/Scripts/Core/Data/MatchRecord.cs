using System;
using System.Collections.Generic;
using System.Linq;
using Core.Enums;

namespace Core.Data
{
    [Serializable]
    public class MatchRecord
    {
        public TournamentStage Stage { get; private set; }
        public List<RoundRecord> Rounds { get; private set; } = new();
        public GameResult? FinalResult { get; private set; }
        public DateTime StartTime { get; private set; }
        public DateTime? EndTime { get; private set; }

        // 계산 프로퍼티
        public int TotalRounds => Rounds.Count;
        public int PlayerWins => Rounds.Count(r => r.Result == GameResult.Win);
        public int OpponentWins => Rounds.Count(r => r.Result == GameResult.Lose);
        public int Draws => Rounds.Count(r => r.Result == GameResult.Draw);
        public TimeSpan Duration => (EndTime ?? DateTime.Now) - StartTime;
        public bool IsCompleted => FinalResult.HasValue;

        public MatchRecord(TournamentStage stage)
        {
            Stage = stage;
            StartTime = DateTime.Now;
        }

        public void AddRound(HandType playerHand, HandType opponentHand, GameResult result)
        {
            var record = new RoundRecord(playerHand, opponentHand, result, Rounds.Count + 1);
            Rounds.Add(record);
        }

        public void Complete(GameResult finalResult)
        {
            FinalResult = finalResult;
            EndTime = DateTime.Now;
        }

        // 손 사용 횟수
        public int GetPlayerHandCount(HandType hand)
        {
            return Rounds.Count(r => r.PlayerHand == hand);
        }

        public int GetOpponentHandCount(HandType hand)
        {
            return Rounds.Count(r => r.OpponentHand == hand);
        }

        // 통계 요약
        public MatchStatistics GetStatistics()
        {
            return new MatchStatistics
            {
                TotalRounds = TotalRounds,
                Wins = PlayerWins,
                Loses = OpponentWins,
                Draws = Draws,
                RockCount = GetPlayerHandCount(HandType.Rock),
                PaperCount = GetPlayerHandCount(HandType.Paper),
                ScissorsCount = GetPlayerHandCount(HandType.Scissors)
            };
        }
    }
}