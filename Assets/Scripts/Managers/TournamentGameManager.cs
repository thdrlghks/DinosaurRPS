using System.Collections.Generic;
using System.Threading;
using Core.Data;
using Core.Enums;
using Core.Interfaces;
using Cysharp.Threading.Tasks;
using DefaultNamespace;
using DefaultNamespace.Gameplay;
using Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils;

namespace Managers
{
    public class TournamentGameManager : MonoBehaviour
    {
        #region Variables

        [Header("Game Data")] [SerializeField] private MatchData _matchData;
        [SerializeField] private TournamentStage _currentStage = TournamentStage.QuarterFinals;
        
        [SerializeField] private SFXManager _sfxManager;

        [Header("UI Management")] [SerializeField]
        private UIManager _uiManager;

        [SerializeField] private GameObject _startBackGround;

        [SerializeField] private Canvas _gameHealthCanvas;

        [Header("Player & Opponent")] [SerializeField]
        private Animator _playerAnimator;

        [SerializeField] private Animator _opponentAnimator;
        [SerializeField] private GameObject _playerCanvas;
        [SerializeField] private GameObject _opponentCanvas;

        [Header("RoundStartUI")] [SerializeField]
        private GameObject _roundStartUIRock;

        [SerializeField] private GameObject _roundStartUIPaper;
        [SerializeField] private GameObject _roundStartUIScissors;
        [SerializeField] private GameObject _balloon;

        [Header("RPS Select UI")]
        [SerializeField] private GameObject _rpsSelectCanvas;
        [SerializeField] private Image _rpsRockImage;
        [SerializeField] private Image _rpsPaperImage;
        [SerializeField] private Image _rpsScissorsImage;

        [Header("Hand Cam RawImages")]
        [SerializeField] private GameObject _cameraCanvas;

        [Header("Camera & Settings")] [SerializeField]
        private CameraManager _camController;

        [SerializeField] private float _resultDisplayTime = 2f;
        [SerializeField, Range(0.2f, 0.4f)] private float _handRevealDelay = 0.3f;
        [SerializeField, Range(0f, 0.8f)] private float _paperRevealExtraDelay = 0.25f;
        [SerializeField, Range(0.05f, 0.5f)] private float _idleBlendDuration = 0.2f;

        [Header("Tutorial - Sealed Hands (첫 라운드만 적용)")]
        [SerializeField] private List<HandType> _sealedHands = new();
        [SerializeField] private TMP_Text _sealedWarningText;

        //Private Value
        private HandType? _selectedHand;
        private IGameJudge _gameJudge;
        private IOpponentHandGenerator _opponentAI;
        private bool _canInput;
        private bool _roundInProgress;
        private bool _isCountingDown;

        #endregion


        #region Component Initialization

        private void Awake()
        {
            if (_sfxManager == null)
            {
                _sfxManager = FindObjectOfType<SFXManager>();
            }
            var gameJudgeComponent = GetComponent<GameJudge>();
            if (gameJudgeComponent != null)
            {
                _gameJudge = gameJudgeComponent;
            }
            else
            {
                _gameJudge = gameObject.AddComponent<GameJudge>();
            }

            var opponentAIComponent = GetComponent<TournamentOpponentAI>();
            _opponentAI = opponentAIComponent != null
                ? opponentAIComponent
                : gameObject.AddComponent<TournamentOpponentAI>();

            if (_uiManager == null)
            {
                _uiManager = FindObjectOfType<UIManager>();
            }
        }

        #endregion

        private void Start()
        {
            TournamentIntro().Forget();
        }

        #region TournamentIntro

        private async UniTask TournamentIntro()
        {
            await PlayCanvasIntroAnimation();
            InitializeMatch();
            _canInput = true;
            _gameHealthCanvas.gameObject.SetActive(true);
            StartRound().Forget();
        }

        #endregion

        #region PlayCanvasIntroAnimation

        private async UniTask PlayCanvasIntroAnimation()
        {
            var playerRect = _playerCanvas.GetComponent<RectTransform>();
            var opponentRect = _opponentCanvas.GetComponent<RectTransform>();
            var playerFinalPos = Vector2.zero;
            var opponentFinalPos = Vector2.zero;

            Vector2 playerStartPos = new Vector2(-1050f, 0);
            Vector2 opponentStartPos = new Vector2(1050f, 0);

            if (playerRect != null)
            {
                playerRect.anchoredPosition = playerStartPos;
                _playerCanvas.gameObject.SetActive(true);
            }

            if (opponentRect != null)
            {
                opponentRect.anchoredPosition = opponentStartPos;
                _opponentCanvas.gameObject.SetActive(true);
            }

            var animationDuration = 2f;
            var meetTime = 1f;
            var fadeOutTime = 0.5f;

            var elapsedTime = 0f;
            while (elapsedTime < animationDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsedTime / animationDuration);

                if (playerRect != null)
                    playerRect.anchoredPosition = Vector2.Lerp(playerStartPos, playerFinalPos, t);

                if (opponentRect != null)
                    opponentRect.anchoredPosition = Vector2.Lerp(opponentStartPos, opponentFinalPos, t);

                await UniTask.Yield();
            }

            await UniTask.Delay((int)(meetTime * 1000));

            var playerGraphics = _playerCanvas.GetComponentsInChildren<Graphic>(includeInactive: true);
            var opponentGraphics = _opponentCanvas.GetComponentsInChildren<Graphic>(includeInactive: true);

            // 원래 색 저장(재사용 대비)
            var playerOriginal = new Color[playerGraphics.Length];
            var opponentOriginal = new Color[opponentGraphics.Length];
            for (int i = 0; i < playerGraphics.Length; i++) playerOriginal[i] = playerGraphics[i].color;
            for (int i = 0; i < opponentGraphics.Length; i++) opponentOriginal[i] = opponentGraphics[i].color;

            var elapsed = 0f;
            while (elapsed < fadeOutTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeOutTime;
                float a = Mathf.Lerp(1f, 0f, t);

                SetAlpha(playerGraphics, a);
                SetAlpha(opponentGraphics, a);

                await UniTask.Yield();
            }

            _startBackGround.SetActive(false);
            _playerCanvas.SetActive(false);
            _opponentCanvas.SetActive(false);

            // 알파/색상 복구 (다음번 인트로 재사용용)
            RestoreColors(playerGraphics, playerOriginal);
            RestoreColors(opponentGraphics, opponentOriginal);
        }

        private void SetAlpha(Graphic[] graphics, float a)
        {
            for (int i = 0; i < graphics.Length; i++)
            {
                var c = graphics[i].color;
                c.a = a;
                graphics[i].color = c;
            }
        }

        private void RestoreColors(Graphic[] graphics, Color[] originals)
        {
            int len = Mathf.Min(graphics.Length, originals.Length);
            for (int i = 0; i < len; i++)
            {
                graphics[i].color = originals[i];
            }
        }

        #endregion

        private void Update()
        {
            HandleInput();
        }

        #region HandleInput

        private void HandleInput()
        {
            if (!_canInput && !_isCountingDown) return;

            if (Input.GetKeyDown(KeyCode.Q))
            {
                Debug.Log("Q key pressed!");
                SelectHand(HandType.Rock);
            }

            else if (Input.GetKeyDown(KeyCode.W))
            {
                if (IsHandSealed(HandType.Paper))
                {
                    ShowSealedWarning("W를 누를 수 없습니다!");
                    return;
                }
                Debug.Log("W key pressed!");
                SelectHand(HandType.Paper);
            }
            else if (Input.GetKeyDown(KeyCode.E))
            {
                if (IsHandSealed(HandType.Scissors))
                {
                    ShowSealedWarning("E를 누를 수 없습니다!");
                    return;
                }
                Debug.Log("E key pressed!");
                SelectHand(HandType.Scissors);
            }
        }

        #endregion

        private bool IsHandSealed(HandType hand)
        {
            return _matchData.TotalRounds == 0 && _sealedHands.Contains(hand);
        }

        private CancellationTokenSource _warningCts;

        private void ShowSealedWarning(string message)
        {
            if (_sealedWarningText == null) return;

            _warningCts?.Cancel();
            _warningCts = new CancellationTokenSource();

            _sealedWarningText.text = message;
            _sealedWarningText.gameObject.SetActive(true);
            HideSealedWarningAfterDelay(_warningCts.Token).Forget();
        }

        private async UniTaskVoid HideSealedWarningAfterDelay(CancellationToken token)
        {
            await UniTask.Delay(1000, cancellationToken: token);
            if (_sealedWarningText != null)
                _sealedWarningText.gameObject.SetActive(false);
        }

        private void SelectHand(HandType handType)
        {
            Debug.Log($"SelectHand called - handType: {handType}, canInput: {_canInput}, isCountingDown: {_isCountingDown}");
            if (!_isCountingDown && !_canInput) return;

            _selectedHand = handType;
            HighlightRPSSelection(handType);
        }

        private async UniTaskVoid StartRound()
        {
            if (_roundInProgress) return;

            _roundInProgress = true;

            //abc_camController.playIntro = true;

            await UniTask.Delay(500);
            await StartCountdownAndBattle(this.GetCancellationTokenOnDestroy());
        }

        #region StartCountdownAndBattle

        private async UniTask StartCountdownAndBattle(CancellationToken cancellationToken)
        {
            _isCountingDown = true;
            _canInput = false;
            _selectedHand = null;

            _balloon.SetActive(true);
            ShowRPSSelectUI();

            var countdownTask = ExecuteCountdownAnimations(cancellationToken);

            await countdownTask;

            _isCountingDown = false;

            if (!_selectedHand.HasValue)
            {
                var available = new List<HandType>();
                foreach (HandType hand in System.Enum.GetValues(typeof(HandType)))
                {
                    if (!IsHandSealed(hand)) available.Add(hand);
                }
                _selectedHand = available[Random.Range(0, available.Count)];
            }

            HighlightRPSSelection(_selectedHand.Value);
            await ProcessRound(_selectedHand.Value, cancellationToken);

            _selectedHand = null;
            _roundInProgress = false;
        }

        #endregion

        #region ExecuteCountdownAnimations

        private async UniTask ExecuteCountdownAnimations(CancellationToken cancellationToken)
        {
            if (_roundStartUIScissors)
            {
                _roundStartUIScissors.SetActive(true);
                await UITweenUtil.ScaleUpAndFadeOutAsync(
                    _roundStartUIScissors.transform,
                    new Vector3(1.2f, 1.2f, 1f),
                    new Vector3(1.8f, 1.8f, 1.5f),
                    2f,
                    0.5f,
                    cancellationToken
                );
            }

            if (_roundStartUIRock)
            {
                _roundStartUIRock.SetActive(true);
                await UITweenUtil.ScaleUpAndFadeOutAsync(
                    _roundStartUIRock.transform,
                    new Vector3(1.2f, 1.2f, 1f),
                    new Vector3(1.8f, 1.8f, 1.5f),
                    2f,
                    0.5f,
                    cancellationToken
                );
            }

            if (_roundStartUIPaper)
            {
                _roundStartUIPaper.SetActive(true);
                await UITweenUtil.ScaleUpAndFadeOutAsync(
                    _roundStartUIPaper.transform,
                    new Vector3(1.2f, 1.2f, 1f),
                    new Vector3(1.8f, 1.8f, 1.5f),
                    2f,
                    0.5f,
                    cancellationToken
                );
            }

            if (_balloon)
            {
                await UniTask.Delay(1000, cancellationToken: cancellationToken);
                await UITweenUtil.ScaleUpAndFadeOutAsync(
                    _balloon.transform,
                    new Vector3(10.8f, 8.5f, 7.5f),
                    new Vector3(40.0f, 31.5f, 27.7f),
                    2f,
                    0.2f,
                    cancellationToken
                );
            }
        }

        #endregion

        #region InitializeMatch

        private void InitializeMatch()
        {
            _matchData.SetStage(_currentStage);
            BattleHistoryManager.Instance?.StartNewMatch(_currentStage);

            if (_uiManager != null)
            {
                _uiManager.InitializeHealthBars(_currentStage);
                _uiManager.SetMaxHealth(
                    GetMaxHealthForStage(_currentStage, true),
                    GetMaxHealthForStage(_currentStage, false)
                );
            }

            UpdateUI();
        }

        #endregion

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

        #region ProcessRound

        private async UniTask ProcessRound(HandType playerHand, CancellationToken cancellationToken)
        {
            _canInput = false;
            var opponentHand = GenerateOpponentHand(playerHand);
            var result = _gameJudge.DetermineResult(playerHand, opponentHand);
            _matchData.RecordResult(result);
            BattleHistoryManager.Instance?.RecordRound(playerHand, opponentHand, result);

            // 플레이어와 상대방의 손 애니메이션 재생
            PlayHandAnimation(_playerAnimator, playerHand);
            PlayHandAnimation(_opponentAnimator, opponentHand);

            // HandCam RawImage 표시
            if (_cameraCanvas != null)
                _cameraCanvas.SetActive(true);

            // 손 모션을 자세히 보여주는 시간
            float revealDelay = GetHandRevealDelay(playerHand, opponentHand);
            await UniTask.Delay((int)(revealDelay * 1000f), cancellationToken: cancellationToken);

            // HandCam RawImage 숨기기
            if (_cameraCanvas != null)
                _cameraCanvas.SetActive(false);
            HideRPSSelectUI();

            if (_uiManager != null)
            {
                _uiManager.ShowBattleResult(playerHand, opponentHand, result);
            }

            PlayBattleAnimations(result);
            UpdateHealthBars();
            if (result == GameResult.Win)
            {
                await _camController.PlayWinCamera();
            }
            else if (result == GameResult.Lose)
            {
                await _camController.PlayLoseCamera();
            }
            else // Draw
            {
                // 비김은 짧은 대기만 (원하면 조정)
                await UniTask.Delay((int)(_resultDisplayTime * 000), cancellationToken: cancellationToken);
                await _camController.PlayIdleCamera();
            }

            // 카메라가 메인으로 돌아온 뒤 결과 표시 후(필요시) 다음 단계
            await UniTask.Delay((int)(_resultDisplayTime * 1000), cancellationToken: cancellationToken);

            ResetAnimations();
            UpdateUI();
            if (_matchData.IsMatchOver())
            {
                await HandleMatchEnd(cancellationToken);
            }
            else
            {
                _roundInProgress = false;
                _canInput = true;
                StartRound().Forget();
                await _camController.RestartSequence();
            }
        }

        #endregion

        private void UpdateUI()
        {
        }

        #region GenerateOpponentHand

        private HandType GenerateOpponentHand(HandType playerHand)
        {
            if (_opponentAI != null)
            {
                return _opponentAI.GenerateOpponentHand(
                    playerHand,
                    _matchData.TournamentStage,
                    _matchData.HasPlayerWonOnce
                );
            }

            return (HandType)Random.Range(0, 3);
        }

        #endregion

        #region PlayRoundAnimations

        private void PlayHandAnimation(Animator animator, HandType hand)
        {
            if (animator == null) return;

            string label = animator == _opponentAnimator ? "Tyranno" : "Player";

            switch (hand)
            {
                case HandType.Rock:
                    Debug.LogError($"[{label}] SetTrigger: Rock");
                    animator.SetTrigger("Rock");
                    break;
                case HandType.Paper:
                    Debug.LogError($"[{label}] SetTrigger: Paper");
                    animator.SetTrigger("Paper");
                    break;
                case HandType.Scissors:
                    Debug.LogError($"[{label}] SetTrigger: Scissors");
                    animator.SetTrigger("Scissors");
                    break;
            }
        }

        private void PlayBattleAnimations(GameResult result)
        {
            if (_playerAnimator != null)
            {
                switch (result)
                {
                    case GameResult.Win:
                        var playerWinIndex = Random.Range(0, 3);
                        var playerWinTrigger = playerWinIndex == 0 ? "Win" : playerWinIndex == 1 ? "Win2" : "Win3";
                        _playerAnimator.SetTrigger(playerWinTrigger);
                        break;
                    case GameResult.Lose:
                        _playerAnimator.SetTrigger("Lose");
                        break;
                    case GameResult.Draw:
                        _playerAnimator.SetTrigger("Draw");
                        break;
                }
            }

            if (_opponentAnimator != null)
            {
                switch (result)
                {
                    case GameResult.Win:
                        Debug.LogError("[Tyranno] SetTrigger: Lose");
                        _opponentAnimator.SetTrigger("Lose");
                        break;
                    case GameResult.Lose:
                        var opponentWinIndex = Random.Range(0, 3);
                        var opponentWinTrigger = opponentWinIndex == 0 ? "Win" : opponentWinIndex == 1 ? "Win2" : "Win3";
                        Debug.LogError($"[Tyranno] SetTrigger: {opponentWinTrigger}");
                        _opponentAnimator.SetTrigger(opponentWinTrigger);
                        break;
                    case GameResult.Draw:
                        Debug.LogError("[Tyranno] SetTrigger: Draw");
                        _opponentAnimator.SetTrigger("Draw");
                        break;
                }
            }
        }

        #endregion

        #region RPSSelectUI

        private readonly Color _defaultColor = Color.white;
        private readonly Color _selectedColor = Color.red;

        private void ShowRPSSelectUI()
        {
            if (_rpsSelectCanvas != null)
                _rpsSelectCanvas.SetActive(true);

            ResetRPSColors();
        }

        private void HideRPSSelectUI()
        {
            if (_rpsSelectCanvas != null)
                _rpsSelectCanvas.SetActive(false);
        }

        private void HighlightRPSSelection(HandType handType)
        {
            ResetRPSColors();

            var targetImage = handType switch
            {
                HandType.Rock => _rpsRockImage,
                HandType.Paper => _rpsPaperImage,
                HandType.Scissors => _rpsScissorsImage,
                _ => null
            };

            if (targetImage != null)
                targetImage.color = _selectedColor;
        }

        private float GetHandRevealDelay(HandType playerHand, HandType opponentHand)
        {
            float delay = _handRevealDelay;
            if (playerHand == HandType.Paper || opponentHand == HandType.Paper)
            {
                delay += _paperRevealExtraDelay;
            }

            return delay;
        }

        private void ResetRPSColors()
        {
            if (_rpsRockImage != null) _rpsRockImage.color = _defaultColor;
            if (_rpsPaperImage != null) _rpsPaperImage.color = _defaultColor;
            if (_rpsScissorsImage != null) _rpsScissorsImage.color = _defaultColor;
        }

        #endregion

        #region ResetAnimations

        private void ResetAllTriggers(Animator animator)
        {
            animator.ResetTrigger("Rock");
            animator.ResetTrigger("Paper");
            animator.ResetTrigger("Scissors");
            animator.ResetTrigger("Win");
            animator.ResetTrigger("Win2");
            animator.ResetTrigger("Win3");
            animator.ResetTrigger("Lose");
            animator.ResetTrigger("Draw");
        }

        private void ResetAnimations()
        {
            if (_playerAnimator != null)
            {
                ResetAllTriggers(_playerAnimator);
                _playerAnimator.CrossFadeInFixedTime("Idle", _idleBlendDuration, 0);
            }

            if (_opponentAnimator != null)
            {
                ResetAllTriggers(_opponentAnimator);
                Debug.LogError("[Tyranno] ResetTriggers + Play Idle");
                _opponentAnimator.CrossFadeInFixedTime("Idle", _idleBlendDuration, 0);
            }
        }

        #endregion

        #region UpdateHealthBars

        private void UpdateHealthBars()
        {
            if (_uiManager == null)
            {
                Debug.LogWarning("[UpdateHealthBars] _uiManager is null!");
                return;
            }

            var playerHealth = GetMaxHealthForStage(_currentStage, true) - _matchData.OpponentWins;
            var opponentHealth = GetMaxHealthForStage(_currentStage, false) - _matchData.PlayerWins;

            Debug.Log($"[UpdateHealthBars] stage={_currentStage}, playerHP={playerHealth}, opponentHP={opponentHealth}");
            _uiManager.UpdateHealthBars(playerHealth, opponentHealth);
        }

        #endregion

        #region HandleMatchEnd

        private async UniTask HandleMatchEnd(CancellationToken cancellationToken)
        {
            GameResult? winner = _matchData.GetWinner();
            
            if (winner.HasValue)
            {
                BattleHistoryManager.Instance?.CompleteMatch(winner.Value);
            }

            await UniTask.Delay(2000, cancellationToken: cancellationToken);

            if (SceneController.Instance != null)
            {
                SceneController.Instance.HandleTournamentResult(
                    winner == GameResult.Win, _currentStage);
            }
        }

        #endregion
    }
}
