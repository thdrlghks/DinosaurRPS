using System;
using System.Collections.Generic;
using System.Linq;
using Core.Enums;
using UnityEngine;

namespace Core.Data
{
    public class BattleHistoryManager : MonoBehaviour
    {
        public static BattleHistoryManager Instance { get; private set; }

        [SerializeField] private List<MatchRecord> _matchHistory = new();
        private MatchRecord _currentMatch;

        public IReadOnlyList<MatchRecord> MatchHistory => _matchHistory;
        public MatchRecord CurrentMatch => _currentMatch;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            LoadFromPlayerPrefs();
        }

        #region Match Lifecycle

        public void StartNewMatch(TournamentStage stage)
        {
            _currentMatch = new MatchRecord(stage);
            Debug.Log($"[BattleHistory] 새 매치 시작: {stage}");
        }

        public void RecordRound(HandType playerHand, HandType opponentHand, GameResult result)
        {
            if (_currentMatch == null)
            {
                Debug.LogWarning("[BattleHistory] 매치가 시작되지 않음!");
                return;
            }

            _currentMatch.AddRound(playerHand, opponentHand, result);
            
            Debug.Log($"[BattleHistory] 라운드 기록: {playerHand} vs {opponentHand} = {result}");
        }

        public void CompleteMatch(GameResult finalResult)
        {
            if (_currentMatch == null) return;

            _currentMatch.Complete(finalResult);
            _matchHistory.Add(_currentMatch);

            Debug.Log($"[BattleHistory] 매치 완료: {_currentMatch.Stage} - {finalResult}");
            Debug.Log(_currentMatch.GetStatistics().ToString());

            SaveToPlayerPrefs();
            _currentMatch = null;
        }

        #endregion

        #region Statistics

        public MatchStatistics GetCurrentMatchStats()
        {
            return _currentMatch?.GetStatistics() ?? default;
        }

        public BattleStatistics GetTotalStats()
        {
            var stats = new BattleStatistics
            {
                TotalMatches = _matchHistory.Count
            };

            foreach (var match in _matchHistory)
            {
                var matchStats = match.GetStatistics();
                stats.TotalRounds += matchStats.TotalRounds;
                stats.TotalWins += matchStats.Wins;
                stats.TotalLoses += matchStats.Loses;
                stats.TotalDraws += matchStats.Draws;
                stats.RockCount += matchStats.RockCount;
                stats.PaperCount += matchStats.PaperCount;
                stats.ScissorsCount += matchStats.ScissorsCount;
            }

            // 매치 승패 (라운드 X)
            stats.MatchWins = _matchHistory.Count(m => m.FinalResult == GameResult.Win);
            stats.MatchLoses = _matchHistory.Count(m => m.FinalResult == GameResult.Lose);

            return stats;
        }

        public List<MatchRecord> GetMatchesByStage(TournamentStage stage)
        {
            return _matchHistory.Where(m => m.Stage == stage).ToList();
        }

        #endregion

        #region Persistence

        private const string SAVE_KEY = "BattleHistory";

        public void SaveToPlayerPrefs()
        {
            var data = new BattleHistorySaveData
            {
                Matches = _matchHistory.Select(m => MatchRecordToSaveData(m)).ToList()
            };
            
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();
        }

        public void LoadFromPlayerPrefs()
        {
            if (!PlayerPrefs.HasKey(SAVE_KEY)) return;

            string json = PlayerPrefs.GetString(SAVE_KEY);
            var data = JsonUtility.FromJson<BattleHistorySaveData>(json);

            _matchHistory.Clear();
            foreach (var matchData in data.Matches)
            {
                _matchHistory.Add(SaveDataToMatchRecord(matchData));
            }

            Debug.Log($"[BattleHistory] 기록 로드: {_matchHistory.Count}개 매치");
        }

        public void ClearHistory()
        {
            _matchHistory.Clear();
            PlayerPrefs.DeleteKey(SAVE_KEY);
            Debug.Log("[BattleHistory] 기록 초기화");
        }

        #endregion

        #region Save Data Conversion

        private MatchRecordSaveData MatchRecordToSaveData(MatchRecord record)
        {
            return new MatchRecordSaveData
            {
                Stage = (int)record.Stage,
                FinalResult = record.FinalResult.HasValue ? (int)record.FinalResult.Value : -1,
                StartTime = record.StartTime.ToBinary(),
                EndTime = record.EndTime?.ToBinary() ?? 0,
                Rounds = record.Rounds.Select(r => new RoundRecordSaveData
                {
                    PlayerHand = (int)r.PlayerHand,
                    OpponentHand = (int)r.OpponentHand,
                    Result = (int)r.Result,
                    RoundNumber = r.RoundNumber
                }).ToList()
            };
        }

        private MatchRecord SaveDataToMatchRecord(MatchRecordSaveData data)
        {
            var record = new MatchRecord((TournamentStage)data.Stage);
            
            foreach (var roundData in data.Rounds)
            {
                record.AddRound(
                    (HandType)roundData.PlayerHand,
                    (HandType)roundData.OpponentHand,
                    (GameResult)roundData.Result
                );
            }

            if (data.FinalResult >= 0)
            {
                record.Complete((GameResult)data.FinalResult);
            }

            return record;
        }

        #endregion
    }

    #region Save Data Classes

    [Serializable]
    public class BattleHistorySaveData
    {
        public List<MatchRecordSaveData> Matches = new();
    }

    [Serializable]
    public class MatchRecordSaveData
    {
        public int Stage;
        public int FinalResult;
        public long StartTime;
        public long EndTime;
        public List<RoundRecordSaveData> Rounds = new();
    }

    [Serializable]
    public class RoundRecordSaveData
    {
        public int PlayerHand;
        public int OpponentHand;
        public int Result;
        public int RoundNumber;
    }

    #endregion
}