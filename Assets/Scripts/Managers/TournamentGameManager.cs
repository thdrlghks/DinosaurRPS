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

        // HitFlash, ImpactVFX는 캐릭터 오브젝트에 붙여두면 런타임에 자동 탐색
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
        [SerializeField, Range(1f, 5f)] private float _rawImageDisplayTime = 3f;
        [SerializeField, Range(0.05f, 0.5f)] private float _idleBlendDuration = 0.2f;
        [SerializeField, Min(0f)] private float _hpApplyDelay = 2f;
        [SerializeField, Min(0f)] private float _postHpDelay = 0.8f;

        [Header("Hit Effects")]
        [SerializeField, Range(0.03f, 0.15f)] private float _hitStopDuration = 0.07f;
        [SerializeField, Range(0.5f, 2f)] private float _shakeMultiplier = 1f;

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
                // "보" 타이밍에 말풍선을 가위/바위처럼 가만히 두고 그냥 비활성화
                await UniTask.Delay(1000, cancellationToken: cancellationToken);
                _balloon.SetActive(false);
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

            // === 비김 처리: 카메라 이동 + 손 모션만 보여주고 바로 다시 라운드 ===
            if (result == GameResult.Draw)
            {
                Debug.Log("[ProcessRound] 비김! 손 모션 보여주고 바로 다시 라운드 시작");

                // 카메라 캐릭터쪽으로 이동
                await _camController.MoveToResultPosition();

                // 카메라 도착 후 손 애니메이션 트리거
                PlayHandAnimation(_playerAnimator, playerHand);
                PlayHandAnimation(_opponentAnimator, opponentHand);

                // HandCam RawImage 표시
                if (_cameraCanvas != null)
                    _cameraCanvas.SetActive(true);

                // RawImage 표시 시간 (3초)
                await UniTask.Delay((int)(_rawImageDisplayTime * 1000f), cancellationToken: cancellationToken);

                // HandCam 끄기
                if (_cameraCanvas != null)
                    _cameraCanvas.SetActive(false);
                HideRPSSelectUI();

                // 카메라 복귀
                _camController.RestoreCinemachine();
                _camController.SwitchCamera(_camController.idleCam);
                await UniTask.Delay(300, cancellationToken: cancellationToken);

                ResetAnimations();
                _roundInProgress = false;
                _canInput = true;
                StartRound().Forget();
                await _camController.RestartSequence();
                return;
            }

            // === MainCamera를 결과 좌표로 직접 이동 ===
            await _camController.MoveToResultPosition();

            // 카메라 도착 후 손 애니메이션 트리거
            PlayHandAnimation(_playerAnimator, playerHand);
            PlayHandAnimation(_opponentAnimator, opponentHand);

            // HandCam RawImage 표시 (카메라 이동 후)
            if (_cameraCanvas != null)
                _cameraCanvas.SetActive(true);

            // RawImage 표시 시간 (3초)
            await UniTask.Delay((int)(_rawImageDisplayTime * 1000f), cancellationToken: cancellationToken);

            // HandCam RawImage 숨기기 (승리모션 전에 꺼야 함)
            if (_cameraCanvas != null)
                _cameraCanvas.SetActive(false);

            HideRPSSelectUI();

            if (_uiManager != null)
            {
                _uiManager.ShowBattleResult(playerHand, opponentHand, result);
            }

            // === 배틀 애니메이션 재생 (Win/Lose 모션) ===
            PlayBattleAnimations(result);

            // === 히트스톱 + 카메라 쉐이크 + 히트플래시 + VFX (타격감) ===
            HitStop.Instance.Play(_hitStopDuration);
            if (_camController != null && _camController.cameraShake != null)
            {
                _camController.cameraShake.PlayImpactShake(null, _shakeMultiplier);
            }
            PlayHitEffects(result);

            // === QWE 아이콘 승패 반응 ===
            HighlightRPSResult(result);

            // === 2선승제: 반피 헤롱헤롱 체크 ===
            CheckHalfHealthStagger(result);

            float hpDelay = Mathf.Max(0f, Mathf.Min(_hpApplyDelay, _resultDisplayTime));
            if (hpDelay > 0f)
            {
                await UniTask.Delay((int)(hpDelay * 1000f), cancellationToken: cancellationToken);
            }

            // === 설정한 타이밍에 HP 감소 ===
            UpdateHealthBars();

            // 결과 모션 남은 시간 대기
            float remainResultTime = Mathf.Max(0f, _resultDisplayTime - hpDelay);
            if (remainResultTime > 0f)
            {
                await UniTask.Delay((int)(remainResultTime * 1000f), cancellationToken: cancellationToken);
            }

            // === 카메라 복귀: Cinemachine 다시 활성화 ===
            _camController.RestoreCinemachine();
            _camController.SwitchCamera(_camController.idleCam);
            await UniTask.Delay(500, cancellationToken: cancellationToken);

            // HP 감소 애니메이션 추가 대기
            if (_postHpDelay > 0f)
            {
                await UniTask.Delay((int)(_postHpDelay * 1000f), cancellationToken: cancellationToken);
            }

            // === 사망 체크 (Enemy HP <= 0) ===
            bool isDeathRound = CheckDeathAnimation(result);
            if (isDeathRound)
            {
                // TODO: 실제 Death/Fall 애니메이션 길이에 맞춰 조정
                await UniTask.Delay(1000, cancellationToken: cancellationToken);
            }

            await UniTask.Delay((int)(_resultDisplayTime * 500), cancellationToken: cancellationToken);

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

        /// <summary>
        /// 피격 캐릭터에 HitFlash + ImpactVFX 실행 (SendMessage로 타입 의존성 없이 호출)
        /// </summary>
        private void PlayHitEffects(GameResult result)
        {
            // 진 쪽 = 맞은 캐릭터
            Animator loserAnim = result == GameResult.Win ? _opponentAnimator : _playerAnimator;
            Animator winnerAnim = result == GameResult.Win ? _playerAnimator : _opponentAnimator;
            GameObject loser = loserAnim != null ? loserAnim.gameObject : null;
            GameObject winner = winnerAnim != null ? winnerAnim.gameObject : null;

            if (loser != null)
            {
                loser.SendMessage("Play", SendMessageOptions.DontRequireReceiver);       // HitFlash.Play()
                loser.SendMessage("PlayHitVFX", SendMessageOptions.DontRequireReceiver);  // ImpactVFX.PlayHitVFX()
            }

            if (winner != null)
            {
                winner.SendMessage("PlayWinVFX", SendMessageOptions.DontRequireReceiver); // ImpactVFX.PlayWinVFX()
            }
        }

        /// <summary>
        /// 2선승제에서 반피일 때 헤롱헤롱 모션 체크
        /// </summary>
        private void CheckHalfHealthStagger(GameResult result)
        {
            // 2선승제 단계(4강/결승)만 적용
            if (_currentStage != TournamentStage.SemiFinals && _currentStage != TournamentStage.Finals) return;

            int maxPlayerHP = GetMaxHealthForStage(_currentStage, true);
            int maxOpponentHP = GetMaxHealthForStage(_currentStage, false);

            if (result == GameResult.Win)
            {
                // 적이 맞았을 때 - 적 남은 HP 체크
                int opponentHP = maxOpponentHP - _matchData.PlayerWins;
                if (opponentHP == 1)
                {
                    Debug.Log("[2선승] 적 반피! 헤롱헤롱 비틀거리는 모션 실행 (TODO: 애니메이션 추가)");
                    // TODO: _opponentAnimator.SetTrigger("Stagger");
                }
            }
            else if (result == GameResult.Lose)
            {
                // 플레이어가 맞았을 때 - 플레이어 남은 HP 체크
                int playerHP = maxPlayerHP - _matchData.OpponentWins;
                if (playerHP == 1)
                {
                    Debug.Log("[2선승] 플레이어 반피! 헤롱헤롱 비틀거리는 모션 실행 (TODO: 애니메이션 추가)");
                    // TODO: _playerAnimator.SetTrigger("Stagger");
                }
            }
        }

        /// <summary>
        /// HP가 0 이하일 때 쓰러지는 모션 체크
        /// </summary>
        private bool CheckDeathAnimation(GameResult result)
        {
            int maxPlayerHP = GetMaxHealthForStage(_currentStage, true);
            int maxOpponentHP = GetMaxHealthForStage(_currentStage, false);

            if (result == GameResult.Win)
            {
                int opponentHP = maxOpponentHP - _matchData.PlayerWins;
                if (opponentHP <= 0)
                {
                    Debug.Log("[사망] 적 HP 0! 쓰러지는 모션 실행 (TODO: 애니메이션 추가)");
                    // TODO: _opponentAnimator.SetTrigger("Death");
                    return true;
                }
            }
            else if (result == GameResult.Lose)
            {
                int playerHP = maxPlayerHP - _matchData.OpponentWins;
                if (playerHP <= 0)
                {
                    Debug.Log("[사망] 플레이어 HP 0! 쓰러지는 모션 실행 (TODO: 애니메이션 추가)");
                    // TODO: _playerAnimator.SetTrigger("Death");
                    return true;
                }
            }

            return false;
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


        private void ResetRPSColors()
        {
            if (_rpsRockImage != null) _rpsRockImage.color = _defaultColor;
            if (_rpsPaperImage != null) _rpsPaperImage.color = _defaultColor;
            if (_rpsScissorsImage != null) _rpsScissorsImage.color = _defaultColor;

            // 스케일 복원
            if (_rpsRockImage != null) _rpsRockImage.rectTransform.localScale = Vector3.one;
            if (_rpsPaperImage != null) _rpsPaperImage.rectTransform.localScale = Vector3.one;
            if (_rpsScissorsImage != null) _rpsScissorsImage.rectTransform.localScale = Vector3.one;
        }

        /// <summary>
        /// 승패 결과에 따라 QWE 아이콘에 시각 피드백
        /// 이긴 손: 확대 + 초록, 진 손: 회색 축소
        /// </summary>
        private void HighlightRPSResult(GameResult result)
        {
            if (!_selectedHand.HasValue) return;

            Color winGlow = new Color(0.3f, 1f, 0.3f, 1f);
            Color loseGray = new Color(0.4f, 0.4f, 0.4f, 1f);
            Vector3 winScale = new Vector3(1.3f, 1.3f, 1f);
            Vector3 loseScale = new Vector3(0.8f, 0.8f, 1f);

            var selectedImage = _selectedHand.Value switch
            {
                HandType.Rock => _rpsRockImage,
                HandType.Paper => _rpsPaperImage,
                HandType.Scissors => _rpsScissorsImage,
                _ => null
            };

            if (selectedImage == null) return;

            if (result == GameResult.Win)
            {
                selectedImage.color = winGlow;
                selectedImage.rectTransform.localScale = winScale;
            }
            else if (result == GameResult.Lose)
            {
                selectedImage.color = loseGray;
                selectedImage.rectTransform.localScale = loseScale;
            }
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
