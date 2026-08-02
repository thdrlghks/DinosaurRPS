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

        [Header("Health Bar Damage Trail (빨간 잔상)")]
        [Tooltip("플레이어 체력바 뒤에 배치된 빨간색 Image (없으면 잔상 없음)")]
        [SerializeField] private Image _playerHealthTrail;
        [Tooltip("상대 체력바 뒤에 배치된 빨간색 Image (없으면 잔상 없음)")]
        [SerializeField] private Image _opponentHealthTrail;
        [SerializeField] private float _trailDelay = 0.5f;
        [SerializeField] private float _trailSpeed = 1f;

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

        [Header("Damage Feedback")]
        [SerializeField] private Color _damageFlashColor = new Color(1f, 0.45f, 0.45f, 1f);
        [SerializeField, Range(0.05f, 0.4f)] private float _damageFlashDuration = 0.18f;

        private int _maxPlayerHealth;
        private int _maxOpponentHealth;
        private int _currentPlayerHealth;
        private int _currentOpponentHealth;

        private float _targetPlayerFill;
        private float _targetOpponentFill;
        private float _trailPlayerFill;
        private float _trailOpponentFill;
        private float _trailPlayerTimer;
        private float _trailOpponentTimer;

        private Vector3 _playerHandOriginalPosition;
        private Vector3 _opponentHandOriginalPosition;
        private Color _playerHealthBarOriginalColor = Color.white;
        private Color _opponentHealthBarOriginalColor = Color.white;
        private Color _playerHealthTrailOriginalColor = Color.white;
        private Color _opponentHealthTrailOriginalColor = Color.white;

        private Coroutine _handDisplayCoroutine;
        private Coroutine _playerDamageFlashCoroutine;
        private Coroutine _opponentDamageFlashCoroutine;

        private void Awake()
        {
            if (_playerHealthBar != null)
            {
                _playerHealthBarOriginalColor = _playerHealthBar.color;
            }

            if (_opponentHealthBar != null)
            {
                _opponentHealthBarOriginalColor = _opponentHealthBar.color;
            }

            if (_playerHealthTrail != null)
            {
                _playerHealthTrailOriginalColor = _playerHealthTrail.color;
            }

            if (_opponentHealthTrail != null)
            {
                _opponentHealthTrailOriginalColor = _opponentHealthTrail.color;
            }

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
                // 폰트 외곽선 추가 (어떤 배경에서도 가독성 확보)
                _resultText.outlineWidth = 0.3f;
                _resultText.outlineColor = new Color32(0, 0, 0, 200);
            }
        }

        private void Update()
        {
            AnimateHealthBars();
            AnimateHealthTrails();
        }

        #region Health Bars

        private void AnimateHealthBars()
        {
            MoveFillTowards(_playerHealthBar, _targetPlayerFill, _healthBarAnimationSpeed);
            MoveFillTowards(_opponentHealthBar, _targetOpponentFill, _healthBarAnimationSpeed);
        }

        /// <summary>
        /// fillAmount는 대입할 때마다 해당 Image가 dirty로 표시되어 Canvas 배치/메시가 다시 계산된다.
        /// 목표값에 도달한 뒤에도 매 프레임 같은 값을 쓰면 그 비용이 계속 나가므로, 변할 때만 쓴다.
        /// </summary>
        private static void MoveFillTowards(Image image, float target, float speed)
        {
            if (image == null) return;

            float current = image.fillAmount;
            if (Mathf.Approximately(current, target)) return;

            image.fillAmount = Mathf.MoveTowards(current, target, Time.deltaTime * speed);
        }

        /// <summary>
        /// 체력바 빨간 잔상 애니메이션: 메인 바가 줄어든 뒤 딜레이 후 천천히 따라감
        /// </summary>
        private void AnimateHealthTrails()
        {
            // Player trail
            if (_playerHealthTrail != null && _trailPlayerFill > _targetPlayerFill)
            {
                _trailPlayerTimer += Time.deltaTime;
                if (_trailPlayerTimer >= _trailDelay)
                {
                    MoveFillTowards(_playerHealthTrail, _targetPlayerFill, _trailSpeed);
                    _trailPlayerFill = _playerHealthTrail.fillAmount;
                }
            }

            // Opponent trail
            if (_opponentHealthTrail != null && _trailOpponentFill > _targetOpponentFill)
            {
                _trailOpponentTimer += Time.deltaTime;
                if (_trailOpponentTimer >= _trailDelay)
                {
                    MoveFillTowards(_opponentHealthTrail, _targetOpponentFill, _trailSpeed);
                    _trailOpponentFill = _opponentHealthTrail.fillAmount;
                }
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
            _trailPlayerFill = 1f;
            _trailOpponentFill = 1f;

            if (_playerHealthBar != null)
                _playerHealthBar.fillAmount = 1f;
            if (_opponentHealthBar != null)
                _opponentHealthBar.fillAmount = 1f;
            if (_playerHealthTrail != null)
                _playerHealthTrail.fillAmount = 1f;
            if (_opponentHealthTrail != null)
                _opponentHealthTrail.fillAmount = 1f;
        }

        public void UpdateHealthBars(int playerHealth, int opponentHealth)
        {
            bool playerTookDamage = playerHealth < _currentPlayerHealth;
            bool opponentTookDamage = opponentHealth < _currentOpponentHealth;

            _currentPlayerHealth = playerHealth;
            _currentOpponentHealth = opponentHealth;

            float newPlayerFill = _maxPlayerHealth > 0 ? (float)playerHealth / _maxPlayerHealth : 0f;
            float newOpponentFill = _maxOpponentHealth > 0 ? (float)opponentHealth / _maxOpponentHealth : 0f;

            if (_maxPlayerHealth >= 999)
                newPlayerFill = 1f;

            // 잔상 타이머 리셋 (새 데미지 들어올 때)
            if (newPlayerFill < _targetPlayerFill)
            {
                _trailPlayerTimer = 0f;
                _trailPlayerFill = _targetPlayerFill;
            }
            if (newOpponentFill < _targetOpponentFill)
            {
                _trailOpponentTimer = 0f;
                _trailOpponentFill = _targetOpponentFill;
            }

            _targetPlayerFill = newPlayerFill;
            _targetOpponentFill = newOpponentFill;

            if ((playerTookDamage || opponentTookDamage) && SFXManager.Instance != null)
            {
                SFXManager.Instance.Play(SfxId.HealthDamage);
            }

            if (playerTookDamage)
            {
                TriggerHealthFlash(
                    ref _playerDamageFlashCoroutine,
                    _playerHealthBar,
                    _playerHealthTrail,
                    _playerHealthBarOriginalColor,
                    _playerHealthTrailOriginalColor);
            }

            if (opponentTookDamage)
            {
                TriggerHealthFlash(
                    ref _opponentDamageFlashCoroutine,
                    _opponentHealthBar,
                    _opponentHealthTrail,
                    _opponentHealthBarOriginalColor,
                    _opponentHealthTrailOriginalColor);
            }
        }

        private void TriggerHealthFlash(
            ref Coroutine flashCoroutine,
            Image primaryImage,
            Image secondaryImage,
            Color primaryOriginalColor,
            Color secondaryOriginalColor)
        {
            if (primaryImage == null && secondaryImage == null)
            {
                return;
            }

            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
            }

            flashCoroutine = StartCoroutine(FlashHealthBarCoroutine(
                primaryImage,
                secondaryImage,
                primaryOriginalColor,
                secondaryOriginalColor));
        }

        private IEnumerator FlashHealthBarCoroutine(
            Image primaryImage,
            Image secondaryImage,
            Color primaryOriginalColor,
            Color secondaryOriginalColor)
        {
            float elapsed = 0f;
            float halfDuration = _damageFlashDuration * 0.5f;

            while (elapsed < _damageFlashDuration)
            {
                elapsed += Time.deltaTime;
                float t = halfDuration <= 0f
                    ? 1f
                    : (elapsed <= halfDuration
                        ? elapsed / halfDuration
                        : 1f - ((elapsed - halfDuration) / halfDuration));
                t = Mathf.Clamp01(t);

                if (primaryImage != null)
                {
                    primaryImage.color = Color.Lerp(primaryOriginalColor, _damageFlashColor, t);
                }

                if (secondaryImage != null)
                {
                    secondaryImage.color = Color.Lerp(secondaryOriginalColor, _damageFlashColor, t);
                }

                yield return null;
            }

            if (primaryImage != null)
            {
                primaryImage.color = primaryOriginalColor;
            }

            if (secondaryImage != null)
            {
                secondaryImage.color = secondaryOriginalColor;
            }
        }

        #endregion

        #region Battle Result Display

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

            // Slide in
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

            // 승패 손 시각화: 이긴 쪽 밝게, 진 쪽 회색
            ApplyHandResultVisual(result);

            ShowResultText(result);

            yield return new WaitForSeconds(_handDisplayDuration);

            // Slide out
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

        /// <summary>
        /// 승리한 손은 밝게 + 살짝 확대, 패배한 손은 회색조
        /// </summary>
        private void ApplyHandResultVisual(GameResult result)
        {
            Color winColor = Color.white;
            Color loseColor = new Color(0.4f, 0.4f, 0.4f, 1f); // 회색조
            Vector3 winScale = new Vector3(1.15f, 1.15f, 1f);
            Vector3 normalScale = Vector3.one;

            switch (result)
            {
                case GameResult.Win:
                    if (_playerHandImage != null)
                    {
                        _playerHandImage.color = winColor;
                        _playerHandImage.rectTransform.localScale = winScale;
                    }
                    if (_opponentHandImage != null)
                    {
                        _opponentHandImage.color = loseColor;
                        _opponentHandImage.rectTransform.localScale = normalScale;
                    }
                    break;
                case GameResult.Lose:
                    if (_playerHandImage != null)
                    {
                        _playerHandImage.color = loseColor;
                        _playerHandImage.rectTransform.localScale = normalScale;
                    }
                    if (_opponentHandImage != null)
                    {
                        _opponentHandImage.color = winColor;
                        _opponentHandImage.rectTransform.localScale = winScale;
                    }
                    break;
                case GameResult.Draw:
                    if (_playerHandImage != null)
                    {
                        _playerHandImage.color = winColor;
                        _playerHandImage.rectTransform.localScale = normalScale;
                    }
                    if (_opponentHandImage != null)
                    {
                        _opponentHandImage.color = winColor;
                        _opponentHandImage.rectTransform.localScale = normalScale;
                    }
                    break;
            }
        }

        #endregion

        #region Hand Sprites

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

        #endregion

        #region Result Text

        private void ShowResultText(GameResult result)
        {
            if (_resultText != null)
            {
                _resultText.text = result switch
                {
                    GameResult.Win => "승리!",
                    GameResult.Lose => "패배...",
                    GameResult.Draw => "무승부!",
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

        #endregion

        #region Cleanup

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
                _playerHandImage.rectTransform.localScale = Vector3.one;
                Color color = _playerHandImage.color;
                color.a = 1f;
                _playerHandImage.color = color;
            }

            if (_opponentHandImage != null)
            {
                _opponentHandImage.rectTransform.anchoredPosition = _opponentHandOriginalPosition;
                _opponentHandImage.rectTransform.localScale = Vector3.one;
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

        #endregion
    }
}
