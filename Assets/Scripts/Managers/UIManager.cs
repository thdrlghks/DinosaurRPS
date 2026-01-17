using Core.Enums;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace Managers
{
    public class UIManager : MonoBehaviour
    {
        [Header("Health Bars")]
        [SerializeField] private Image _playerHealthBar;
        [SerializeField] private Image _opponentHealthBar;
        
        [Header("Hand Display")]
        [SerializeField] private Image _playerHandImage;     
        [SerializeField] private Image _opponentHandImage;    
        
        [Header("Hand Sprites")]
        [SerializeField] private Sprite _rockSprite;
        [SerializeField] private Sprite _paperSprite;
        [SerializeField] private Sprite _scissorsSprite;
        
        [Header("Enemy Sprites")]
        [SerializeField] private Sprite _enemyRockSprite;
        [SerializeField] private Sprite _enemyPaperSprite;
        [SerializeField] private Sprite _enemyScissorsSprite;
        
        [Header("Result Text")]
        [SerializeField] private TextMeshProUGUI _resultText; 
        
        [Header("Animation Settings")]
        [SerializeField] private float _healthBarAnimationSpeed = 2f;
        [SerializeField] private float _handSlideInDuration = 0.5f;
        [SerializeField] private float _handDisplayDuration = 2f;
        [SerializeField] private float _handSlideOutDuration = 0.3f;
        [SerializeField] private float _slideDistance = 300f;
        
        private int _maxPlayerHealth;
        private int _maxOpponentHealth;
        private int _currentPlayerHealth;
        private int _currentOpponentHealth;
        
        private float _targetPlayerFill;
        private float _targetOpponentFill;
        
        private Vector3 _playerHandOriginalPosition;
        private Vector3 _opponentHandOriginalPosition;
        
        private Coroutine _handDisplayCoroutine;

        private void Awake()
        {
            if (_playerHandImage != null)
            {
                _playerHandOriginalPosition = _playerHandImage.rectTransform.anchoredPosition;
                _playerHandImage.gameObject.SetActive(false);
            }
            
            if (_opponentHandImage != null)
            {
                _opponentHandOriginalPosition = _opponentHandImage.rectTransform.anchoredPosition;
                _opponentHandImage.gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            if (_resultText != null)
            {
                _resultText.text = "";
            }
        }

        private void Update()
        {
            AnimateHealthBars();
        }

        private void AnimateHealthBars()
        {
            if (_playerHealthBar != null)
            {
                _playerHealthBar.fillAmount = Mathf.Lerp(
                    _playerHealthBar.fillAmount, 
                    _targetPlayerFill, 
                    Time.deltaTime * _healthBarAnimationSpeed
                );
            }
            
            if (_opponentHealthBar != null)
            {
                _opponentHealthBar.fillAmount = Mathf.Lerp(
                    _opponentHealthBar.fillAmount, 
                    _targetOpponentFill, 
                    Time.deltaTime * _healthBarAnimationSpeed
                );
            }
        }

        public void InitializeHealthBars(TournamentStage tournamentStage)
        {
            switch (tournamentStage)
            {
                case TournamentStage.Qualifiers:
                    SetMaxHealth(1, 1);
                    break;
                case TournamentStage.QuarterFinals:
                    SetMaxHealth(999, 1);
                    break;
                case TournamentStage.SemiFinals:
                    SetMaxHealth(2, 2);
                    break;
                case TournamentStage.Finals:
                    SetMaxHealth(2, 2);
                    break;
                case TournamentStage.GrandFinals:
                    SetMaxHealth(1, 1);
                    break;
            }
            ResetHealthBars();
        }

        public void SetMaxHealth(int playerMax, int opponentMax)
        {
            _maxPlayerHealth = playerMax;
            _maxOpponentHealth = opponentMax;
            _currentPlayerHealth = playerMax;
            _currentOpponentHealth = opponentMax;
        }

        private void ResetHealthBars()
        {
            _currentPlayerHealth = _maxPlayerHealth;
            _currentOpponentHealth = _maxOpponentHealth;
            
            _targetPlayerFill = 1f;
            _targetOpponentFill = 1f;
            
            if (_playerHealthBar != null)
                _playerHealthBar.fillAmount = 1f;
            if (_opponentHealthBar != null)
                _opponentHealthBar.fillAmount = 1f;
        }

        public void UpdateHealthBars(int playerHealth, int opponentHealth)
        {
            _currentPlayerHealth = playerHealth;
            _currentOpponentHealth = opponentHealth;
            
            _targetPlayerFill = _maxPlayerHealth > 0 ? (float)playerHealth / _maxPlayerHealth : 0f;
            _targetOpponentFill = _maxOpponentHealth > 0 ? (float)opponentHealth / _maxOpponentHealth : 0f;
            
            if (_maxPlayerHealth >= 999)
            {
                _targetPlayerFill = 1f;
            }
        }


        public void ShowBattleResult(HandType playerHand, HandType opponentHand, GameResult result)
        {
            if (_handDisplayCoroutine != null)
            {
                StopCoroutine(_handDisplayCoroutine);
            }
            
            _handDisplayCoroutine = StartCoroutine(ShowBattleResultCoroutine(playerHand, opponentHand, result));
        }

        private IEnumerator ShowBattleResultCoroutine(HandType playerHand, HandType opponentHand, GameResult result)
        {
            SetHandSprites(playerHand, opponentHand);
            
            var playerStartPos = _playerHandOriginalPosition - new Vector3(_slideDistance, 0, 0);
            var opponentStartPos = _opponentHandOriginalPosition + new Vector3(_slideDistance, 0, 0);
            
            if (_playerHandImage != null)
            {
                _playerHandImage.rectTransform.anchoredPosition = playerStartPos;
                _playerHandImage.gameObject.SetActive(true);
            }
            
            if (_opponentHandImage != null)
            {
                _opponentHandImage.rectTransform.anchoredPosition = opponentStartPos;
                _opponentHandImage.gameObject.SetActive(true);
            }
            
            var elapsedTime = 0f;
            while (elapsedTime < _handSlideInDuration)
            {
                elapsedTime += Time.deltaTime;
                var t = elapsedTime / _handSlideInDuration;
                t = 1f - Mathf.Pow(1f - t, 3f); 
                
                if (_playerHandImage != null)
                {
                    _playerHandImage.rectTransform.anchoredPosition = 
                        Vector3.Lerp(playerStartPos, _playerHandOriginalPosition, t);
                }
                
                if (_opponentHandImage != null)
                {
                    _opponentHandImage.rectTransform.anchoredPosition = 
                        Vector3.Lerp(opponentStartPos, _opponentHandOriginalPosition, t);
                }
                
                yield return null;
            }
            
            if (_playerHandImage != null)
                _playerHandImage.rectTransform.anchoredPosition = _playerHandOriginalPosition;
            if (_opponentHandImage != null)
                _opponentHandImage.rectTransform.anchoredPosition = _opponentHandOriginalPosition;
            
            ShowResultText(result);
            
            yield return new WaitForSeconds(_handDisplayDuration);
            
            var playerEndPos = _playerHandOriginalPosition + new Vector3(_slideDistance, 0, 0);
            var opponentEndPos = _opponentHandOriginalPosition - new Vector3(_slideDistance, 0, 0);
            
            elapsedTime = 0f;
            while (elapsedTime < _handSlideOutDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / _handSlideOutDuration;
                t = Mathf.Pow(t, 3f); 
                
                if (_playerHandImage != null)
                {
                    _playerHandImage.rectTransform.anchoredPosition = 
                        Vector3.Lerp(_playerHandOriginalPosition, playerEndPos, t);
                    
                    Color color = _playerHandImage.color;
                    color.a = 1f - t;
                    _playerHandImage.color = color;
                }
                
                if (_opponentHandImage != null)
                {
                    _opponentHandImage.rectTransform.anchoredPosition = 
                        Vector3.Lerp(_opponentHandOriginalPosition, opponentEndPos, t);
                    
                    Color color = _opponentHandImage.color;
                    color.a = 1f - t;
                    _opponentHandImage.color = color;
                }
                
                if (_resultText != null)
                {
                    Color textColor = _resultText.color;
                    textColor.a = 1f - t;
                    _resultText.color = textColor;
                }
                
                yield return null;
            }
            
            HideHandsAndResult();
            ResetPositionsAndAlpha();
        }

        private void SetHandSprites(HandType playerHand, HandType opponentHand)
        {
            if (_playerHandImage != null)
            {
                _playerHandImage.sprite = GetPlayerHandSprite(playerHand);
            }
            
            if (_opponentHandImage != null)
            {
                _opponentHandImage.sprite = GetEnemyHandSprite(opponentHand);
            }
        }

        private Sprite GetPlayerHandSprite(HandType handType)
        {
            return handType switch
            {
                HandType.Rock => _rockSprite,
                HandType.Paper => _paperSprite,
                HandType.Scissors => _scissorsSprite,
                _ => _rockSprite
            };
        }
        
        private Sprite GetEnemyHandSprite(HandType handType)
        {
            return handType switch
            {
                HandType.Rock => _enemyRockSprite,
                HandType.Paper => _enemyPaperSprite,
                HandType.Scissors => _enemyScissorsSprite,
                _ => _enemyRockSprite
            };
        }

        private void ShowResultText(GameResult result)
        {
            if (_resultText != null)
            {
                _resultText.text = result switch
                {
                    GameResult.Win => "WIN!",
                    GameResult.Lose => "LOSE!",
                    GameResult.Draw => "DRAW!",
                    _ => ""
                };
                
                _resultText.color = result switch
                {
                    GameResult.Win => Color.green,
                    GameResult.Lose => Color.red,
                    GameResult.Draw => Color.yellow,
                    _ => Color.white
                };
                
                var color = _resultText.color;
                color.a = 1f;
                _resultText.color = color;
            }
        }

        private void HideHandsAndResult()
        {
            if (_playerHandImage != null)
                _playerHandImage.gameObject.SetActive(false);
            
            if (_opponentHandImage != null)
                _opponentHandImage.gameObject.SetActive(false);
            
            if (_resultText != null)
                _resultText.text = "";
        }

        private void ResetPositionsAndAlpha()
        {
            if (_playerHandImage != null)
            {
                _playerHandImage.rectTransform.anchoredPosition = _playerHandOriginalPosition;
                Color color = _playerHandImage.color;
                color.a = 1f;
                _playerHandImage.color = color;
            }
            
            if (_opponentHandImage != null)
            {
                _opponentHandImage.rectTransform.anchoredPosition = _opponentHandOriginalPosition;
                Color color = _opponentHandImage.color;
                color.a = 1f;
                _opponentHandImage.color = color;
            }
            
            if (_resultText != null)
            {
                Color color = _resultText.color;
                color.a = 1f;
                _resultText.color = color;
            }
        }

        public void HideResultUI()
        {
            HideHandsAndResult();
        }


        public void ShowResultUI(GameResult result)
        {
            ShowResultText(result);
        }
    }
}