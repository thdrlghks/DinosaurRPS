using System;
using System.Collections.Generic;
using Core.Enums;
using Core.Rules;
using UnityEngine;

namespace Core.Data
{
    [System.Serializable]
    public class MatchData
    {
        [SerializeField] private TournamentStage _tournamentStage;
        [SerializeField] private int _playerWins;
        [SerializeField] private int _opponentWins;
        [SerializeField] private int _draws;
        [SerializeField] private bool _hasPlayerWonOnce;
        [SerializeField] private List<GameResult> _resultHistory = new();
       
        public TournamentStage TournamentStage => _tournamentStage;
        public int PlayerWins => _playerWins;
        public int OpponentWins => _opponentWins;
        public int Draws => _draws;
        public bool HasPlayerWonOnce => _hasPlayerWonOnce;
        public IReadOnlyList<GameResult> ResultHistory => _resultHistory;
        public int TotalRounds => _playerWins + _opponentWins + _draws;

        #region stage & reset

        public void SetStage(TournamentStage newStage, bool resetState = true)
        {
            _tournamentStage = newStage;
            if (resetState) Reset();
        }

        public void Reset()
        {
            _playerWins = 0;
            _opponentWins = 0;
            _draws = 0;
            _hasPlayerWonOnce = false;
            _resultHistory.Clear();
        }

        #endregion

        public bool IsMatchOver()
        {
            return TournamentRules.IsMatchComplete(_playerWins, _opponentWins, _tournamentStage);
        }

        public void RecordResult(GameResult result)
        {
            switch (result)
            {
                case GameResult.Win:
                    _playerWins++;
                    if (_tournamentStage == TournamentStage.SemiFinals && !_hasPlayerWonOnce)
                        _hasPlayerWonOnce = true;
                    break;
                case GameResult.Lose:
                    _opponentWins++;
                    break;
                case GameResult.Draw:
                    _draws++;
                    break;
            }

            _resultHistory.Add(result);
        }

        public GameResult? GetWinner()
        {
            return TournamentRules.DetermineWinner(_playerWins, _opponentWins, _tournamentStage);
        }

        public string GetMatchSummary()
        {
            var winner = GetWinner();
            string winnerText = winner switch
            {
                GameResult.Win => "플레이어 승리",
                GameResult.Lose => "상대 승리",
                null => "진행중",
                _ => "알 수 없음"
            };

            return $"[{_tournamentStage}] {_playerWins}:{_opponentWins} ({_draws}무) - {winnerText}";
        }

        public int GetPlayerHealth(TournamentStage stage)
        {
            int maxHealth = GetMaxHealthForStage(stage, true);
            return Mathf.Max(0, maxHealth - _opponentWins);
        }

        public int GetOpponentHealth(TournamentStage stage)
        {
            int maxHealth = GetMaxHealthForStage(stage, false);
            return Mathf.Max(0, maxHealth - _playerWins);
        }

        private int GetMaxHealthForStage(TournamentStage stage, bool isPlayer)
        {
            return stage switch
            {
                TournamentStage.Qualifiers => 1,
                TournamentStage.QuarterFinals => isPlayer ? 999 : 1,
                TournamentStage.SemiFinals => 2,
                TournamentStage.Finals => 2,
                TournamentStage.GrandFinals => 1,
                _ => 2
            };
        }
    }
}