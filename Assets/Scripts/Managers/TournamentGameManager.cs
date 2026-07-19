using System.Collections.Generic;
using System.Threading;
using Core.Data;
using Core.Enums;
using Core.Interfaces;
using Cysharp.Threading.Tasks;
using DG.Tweening;
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
        [SerializeField] private string[] _playerWinTriggers = { "Win", "Win2", "Win3" };
        [SerializeField] private string[] _opponentWinTriggers = { "Win" };
        [SerializeField] private string _playerLoseTrigger = "Lose";
        [SerializeField] private string _playerDrawTrigger = "Draw";
        [SerializeField] private string _opponentLoseTrigger = "";
        [SerializeField] private string _opponentDrawTrigger = "";
        [Tooltip("가위바위보 승리 춤을 출 때 티라노의 월드 Y 좌표")]
        [SerializeField] private float _tyrannoWinDanceY = 7f;
        [Tooltip("티라노와 승리 카메라가 시상대처럼 올라가는 시간")]
        [SerializeField, Min(0.1f)] private float _tyrannoWinRiseDuration = 1.8f;
        [Tooltip("상승 카메라가 바라볼 티라노 루트 기준 Y 오프셋")]
        [SerializeField] private float _tyrannoWinCameraLookOffsetY = 2.5f;

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
        [SerializeField] private Transform _tyrannoHandCam;
        [SerializeField] private Transform _chickenHandCam;
        [SerializeField, Range(0.7f, 0.99f)] private float _handPoseFreezeNormalizedTime = 0.98f;
        [SerializeField, Min(0f)] private float _handPoseFreezeDuration = 3f;

        [Header("Camera & Settings")] [SerializeField]
        private CameraManager _camController;

        [SerializeField] private float _resultDisplayTime = 2f;
        [SerializeField, Range(1f, 5f)] private float _rawImageDisplayTime = 2f;
        [SerializeField, Range(0.05f, 0.5f)] private float _idleBlendDuration = 0.2f;
        [SerializeField, Min(0f)] private float _hpApplyDelay = 2f;
        [SerializeField, Min(0f)] private float _postHpDelay = 2f;
        [Tooltip("Win/Lose 댄스 애니메이션 총 길이 — 이 시간이 지난 후 HP가 감소합니다")]
        [SerializeField, Min(0f)] private float _danceAnimationDuration = 2f;
        [SerializeField, Min(0f)] private float _resultTextDelay = 2f;
        [SerializeField, Min(0.5f)] private float _countdownStepDuration = 2.4f;
        [SerializeField, Range(0f, 1f)] private float _countdownGapDuration = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _paperAdvanceDuration = 0.8f;
        [Tooltip("\"보\" 표시 길이 배율 (0.5 = 절반). 보 뒤 말풍선(Background)도 함께 줄어듭니다.")]
        [SerializeField, Range(0.1f, 1f)] private float _paperDurationScale = 0.5f;

        [Header("Hit Effects")]
        [SerializeField, Range(0.03f, 0.15f)] private float _hitStopDuration = 0.07f;
        [SerializeField, Range(0.5f, 2f)] private float _shakeMultiplier = 1f;

        [Header("Chicken Final Defeat")]
        [Tooltip("최종 패배 시 닭이 올라갈 로컬 Y 높이")]
        [SerializeField, Min(0f)] private float _chickenDefeatHeight = 3f;
        [Tooltip("닭이 위로 날아가는 시간")]
        [SerializeField, Min(0.05f)] private float _chickenDefeatRiseDuration = 0.65f;
        [Tooltip("닭이 바닥으로 떨어지는 시간")]
        [SerializeField, Min(0.05f)] private float _chickenDefeatFallDuration = 0.45f;
        [Tooltip("착지할 닭의 로컬 Y 좌표")]
        [SerializeField] private float _chickenDefeatLandingY = 0f;
        [Tooltip("날아가며 추가할 로컬 Z 회전값")]
        [SerializeField, Range(-180f, 0f)] private float _chickenDefeatZRotation = -90f;
        [Tooltip("티라노 춤 종료 후 전장 전체 샷으로 이동하는 시간")]
        [SerializeField, Min(0.1f)] private float _chickenDefeatWideShotDuration = 1.4f;

        [Header("Tutorial - Sealed Hands (첫 라운드만 적용)")]
        [SerializeField] private List<HandType> _sealedHands = new();
        [SerializeField] private TMP_Text _sealedWarningText;

        //Private Value
        private const int AnimationLayerIndex = 0;
        private HandType? _selectedHand;
        private IGameJudge _gameJudge;
        private IOpponentHandGenerator _opponentAI;
        private bool _canInput;
        private bool _roundInProgress;
        private bool _isCountingDown;
        private Vector3 _tyrannoOriginalPosition;
        private bool _hasTyrannoOriginalPosition;

        // 씬에 배치된 QWE 아이콘의 원래 스케일 (고정값 대신 이 값을 기준으로 사용)
        private Vector3 _rpsRockOriginalScale = Vector3.one;
        private Vector3 _rpsPaperOriginalScale = Vector3.one;
        private Vector3 _rpsScissorsOriginalScale = Vector3.one;

        private readonly struct BattleAnimationSelection
        {
            public BattleAnimationSelection(bool hasWinner, bool playerWon, string winnerTrigger)
            {
                HasWinner = hasWinner;
                PlayerWon = playerWon;
                WinnerTrigger = winnerTrigger;
            }

            public bool HasWinner { get; }
            public bool PlayerWon { get; }
            public string WinnerTrigger { get; }
        }

        #endregion


        #region Component Initialization

        private void Awake()
        {
            if (_sfxManager == null)
            {
                _sfxManager = FindObjectOfType<SFXManager>();
                if (_sfxManager == null)
                    Debug.LogError("[TournamentGameManager] SFXManager not found. Assign it in the Inspector.", this);
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
                if (_uiManager == null)
                    Debug.LogError("[TournamentGameManager] UIManager not found. Assign it in the Inspector.", this);
            }

            if (_playerAnimator != null)
            {
                _tyrannoOriginalPosition = _playerAnimator.transform.position;
                _hasTyrannoOriginalPosition = true;
            }

            CaptureRPSOriginalScales();
        }

        private void CaptureRPSOriginalScales()
        {
            if (_rpsRockImage != null) _rpsRockOriginalScale = _rpsRockImage.rectTransform.localScale;
            if (_rpsPaperImage != null) _rpsPaperOriginalScale = _rpsPaperImage.rectTransform.localScale;
            if (_rpsScissorsImage != null) _rpsScissorsOriginalScale = _rpsScissorsImage.rectTransform.localScale;
        }

        private Vector3 GetRPSOriginalScale(HandType hand)
        {
            return hand switch
            {
                HandType.Rock => _rpsRockOriginalScale,
                HandType.Paper => _rpsPaperOriginalScale,
                HandType.Scissors => _rpsScissorsOriginalScale,
                _ => Vector3.one
            };
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

            // 캐릭터 두 개가 닿을 때까지 걸리는 시간은 빠르게(2f→1f),
            // 줄인 만큼(1f)은 닿은 뒤 대기 시간에 보태서 전체 길이는 유지
            var animationDuration = 1f;
            var meetTime = 2f;
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
            PlayRPSSelectionPop(_selectedHand.Value);
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
                    _countdownStepDuration,
                    0.5f,
                    cancellationToken
                );
                await DelayIfNeeded(_countdownGapDuration, cancellationToken);
            }

            if (_roundStartUIRock)
            {
                _roundStartUIRock.SetActive(true);
                await UITweenUtil.ScaleUpAndFadeOutAsync(
                    _roundStartUIRock.transform,
                    new Vector3(1.2f, 1.2f, 1f),
                    new Vector3(1.8f, 1.8f, 1.5f),
                    Mathf.Max(0.5f, _countdownStepDuration - _paperAdvanceDuration),
                    0.5f,
                    cancellationToken
                );
                await DelayIfNeeded(_countdownGapDuration, cancellationToken);
            }

            if (_roundStartUIPaper)
            {
                _roundStartUIPaper.SetActive(true);
                await UITweenUtil.ScaleUpAndFadeOutAsync(
                    _roundStartUIPaper.transform,
                    new Vector3(1.2f, 1.2f, 1f),
                    new Vector3(1.8f, 1.8f, 1.5f),
                    _countdownStepDuration * _paperDurationScale,
                    0.5f * _paperDurationScale,
                    cancellationToken
                );
            }

            if (_balloon)
            {
                // "보" 타이밍에 말풍선을 가위/바위처럼 가만히 두고 그냥 비활성화
                await DelayIfNeeded(Mathf.Max(0.25f, _countdownGapDuration + 0.1f), cancellationToken);
                _balloon.SetActive(false);
            }
        }

        private static async UniTask DelayIfNeeded(float delaySeconds, CancellationToken cancellationToken)
        {
            if (delaySeconds <= 0f)
            {
                return;
            }

            await UniTask.Delay((int)(delaySeconds * 1000f), cancellationToken: cancellationToken);
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
                SetBattleUIVisible(false);
                await _camController.MoveToResultPosition();

                await ShowHandResultPreview(playerHand, opponentHand, cancellationToken);
                HideRPSSelectUI();
                SetBattleUIVisible(true);

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
            SetBattleUIVisible(false);
            await _camController.MoveToResultPosition();

            await ShowHandResultPreview(playerHand, opponentHand, cancellationToken);

            SetBattleUIVisible(true);

            // === 승패 문구를 먼저 화면 가운데에 표시 ===
            if (_uiManager != null)
            {
                _uiManager.ShowResultUI(result);
            }

            // === 문구 표시 후 대기 → 그 다음 댄스 ===
            await UniTask.Delay((int)(_resultTextDelay * 1000f), cancellationToken: cancellationToken);

            // === 배틀 애니메이션 재생 (Win/Lose 모션) ===
            int playerStateHashBeforeBattle = GetCurrentStateHash(_playerAnimator);
            int opponentStateHashBeforeBattle = GetCurrentStateHash(_opponentAnimator);
            string playerWinTrigger = result == GameResult.Win
                ? GetRandomTrigger(_playerWinTriggers, "Win")
                : null;

            if (result == GameResult.Win)
            {
                if (_camController != null)
                {
                    _camController.RestoreCinemachine();
                    _camController.SwitchWinCamera(true, playerWinTrigger);
                    await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, cancellationToken);
                }

                await RaiseTyrannoWithCamera(cancellationToken);
            }

            var battleAnimation = PlayBattleAnimations(result, playerWinTrigger);
            bool isChickenFinalDefeat = IsChickenFinalDefeat(result);
            if (battleAnimation.HasWinner && _camController != null && result != GameResult.Win)
            {
                _camController.RestoreCinemachine();
                _camController.SwitchWinCamera(battleAnimation.PlayerWon, battleAnimation.WinnerTrigger);
            }

            if (_uiManager != null)
            {
                _uiManager.ShowBattleResult(playerHand, opponentHand, result);
            }

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

            // === 댄스 애니메이션이 끝날 때까지 대기한 후 HP 감소 ===
            if (isChickenFinalDefeat)
            {
                // 티라노의 승리 춤이 완전히 끝난 다음 전장 전체 샷으로 빠진다.
                await WaitForBattleAnimationsToFinish(
                    playerStateHashBeforeBattle,
                    opponentStateHashBeforeBattle,
                    cancellationToken);
                await MoveCameraToBattlefieldWideShot(cancellationToken);

                // 전체 전장이 보이는 상태에서 닭이 날아가 바닥에 고꾸라진다.
                await PlayChickenFinalDefeat(cancellationToken);

                // 씬이 전환될 때까지 쓰러진 포즈가 다시 Idle로 돌아가지 않게 유지한다.
                if (_opponentAnimator != null)
                {
                    _opponentAnimator.speed = 0f;
                }
            }
            else
            {
                await WaitForBattleAnimationsToFinish(
                    playerStateHashBeforeBattle,
                    opponentStateHashBeforeBattle,
                    cancellationToken);
            }

            UpdateHealthBars();

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
            bool isDeathRound = CheckDeathAnimation(result) || isChickenFinalDefeat;
            if (isDeathRound)
            {
                // TODO: 실제 Death/Fall 애니메이션 길이에 맞춰 조정
                await UniTask.Delay(1000, cancellationToken: cancellationToken);
            }

            await UniTask.Delay((int)(_resultDisplayTime * 1000f), cancellationToken: cancellationToken);

            if (!isDeathRound)
            {
                ResetAnimations();
            }

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

        private void ApplyHandCamPositions()
        {
            if (_tyrannoHandCam != null)
            {
                var follow = _tyrannoHandCam.GetComponent<HandPreviewFollow>();
                if (follow != null) follow.enabled = false;
                _tyrannoHandCam.position = new Vector3(-3.858f, 2.232f, 1.201f);
                _tyrannoHandCam.rotation = Quaternion.Euler(0f, 90f, 0f);
            }
            if (_chickenHandCam != null)
            {
                var follow = _chickenHandCam.GetComponent<HandPreviewFollow>();
                if (follow != null) follow.enabled = false;
                _chickenHandCam.position = new Vector3(-4.632f, 1.468f, -1.245f);
                _chickenHandCam.rotation = Quaternion.Euler(0f, 90f, 0f);
            }
        }

        private void RestoreHandCamFollow()
        {
            if (_tyrannoHandCam != null)
            {
                var follow = _tyrannoHandCam.GetComponent<HandPreviewFollow>();
                if (follow != null) follow.enabled = true;
            }
            if (_chickenHandCam != null)
            {
                var follow = _chickenHandCam.GetComponent<HandPreviewFollow>();
                if (follow != null) follow.enabled = true;
            }
        }

        private async UniTask ShowHandResultPreview(
            HandType playerHand,
            HandType opponentHand,
            CancellationToken cancellationToken)
        {
            int playerStateHashBeforeHand = GetCurrentStateHash(_playerAnimator);
            int opponentStateHashBeforeHand = GetCurrentStateHash(_opponentAnimator);

            PlayHandAnimation(_playerAnimator, playerHand);
            PlayHandAnimation(_opponentAnimator, opponentHand);

            ApplyHandCamPositions();
            if (_cameraCanvas != null)
                _cameraCanvas.SetActive(true);

            try
            {
                await UniTask.WhenAll(
                    HoldHandPoseAtEndAsync(
                        playerStateHashBeforeHand,
                        opponentStateHashBeforeHand,
                        cancellationToken),
                    UniTask.Delay((int)(_rawImageDisplayTime * 1000f), cancellationToken: cancellationToken)
                );
            }
            finally
            {
                RestoreHandCamFollow();
                if (_cameraCanvas != null)
                    _cameraCanvas.SetActive(false);
            }
        }

        private async UniTask HoldHandPoseAtEndAsync(
            int playerPreviousStateHash,
            int opponentPreviousStateHash,
            CancellationToken cancellationToken)
        {
            if (_handPoseFreezeDuration <= 0f)
            {
                return;
            }

            float playerSpeed = _playerAnimator != null ? _playerAnimator.speed : 1f;
            float opponentSpeed = _opponentAnimator != null ? _opponentAnimator.speed : 1f;

            try
            {
                await UniTask.WhenAll(
                    FreezeAnimatorAtHandPoseEndAsync(_playerAnimator, playerPreviousStateHash, cancellationToken),
                    FreezeAnimatorAtHandPoseEndAsync(_opponentAnimator, opponentPreviousStateHash, cancellationToken)
                );

                await UniTask.Delay((int)(_handPoseFreezeDuration * 1000f), cancellationToken: cancellationToken);
            }
            finally
            {
                if (_playerAnimator != null)
                    _playerAnimator.speed = playerSpeed;

                if (_opponentAnimator != null)
                    _opponentAnimator.speed = opponentSpeed;
            }
        }

        private async UniTask FreezeAnimatorAtHandPoseEndAsync(
            Animator animator,
            int previousStateHash,
            CancellationToken cancellationToken)
        {
            if (animator == null)
            {
                return;
            }

            float waitForEnterTimeout = 1.5f;
            float waitForEndTimeout = Mathf.Max(1f, _rawImageDisplayTime);
            float elapsed = 0f;
            int handStateHash = 0;

            while (elapsed < waitForEnterTimeout)
            {
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(AnimationLayerIndex);
                if (stateInfo.fullPathHash != previousStateHash)
                {
                    handStateHash = stateInfo.fullPathHash;
                    break;
                }

                elapsed += Time.deltaTime;
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            if (handStateHash == 0)
            {
                return;
            }

            elapsed = 0f;
            while (elapsed < waitForEndTimeout)
            {
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(AnimationLayerIndex);
                if (stateInfo.fullPathHash != handStateHash)
                {
                    return;
                }

                if (animator.IsInTransition(AnimationLayerIndex) ||
                    stateInfo.normalizedTime >= _handPoseFreezeNormalizedTime)
                {
                    animator.speed = 0f;
                    return;
                }

                elapsed += Time.deltaTime;
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
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

        private bool IsChickenFinalDefeat(GameResult result)
        {
            return result == GameResult.Win
                   && _matchData.IsMatchOver()
                   && _matchData.GetWinner() == GameResult.Win;
        }

        private async UniTask PlayChickenFinalDefeat(CancellationToken cancellationToken)
        {
            if (_opponentAnimator == null)
            {
                return;
            }

            Transform chicken = _opponentAnimator.transform;
            Vector3 startPosition = chicken.localPosition;
            Vector3 peakPosition = new Vector3(
                startPosition.x,
                _chickenDefeatLandingY + _chickenDefeatHeight,
                startPosition.z);
            Vector3 landingPosition = new Vector3(
                startPosition.x,
                _chickenDefeatLandingY,
                startPosition.z);

            Quaternion startRotation = chicken.localRotation;
            Quaternion fallenRotation =
                startRotation * Quaternion.Euler(0f, 0f, _chickenDefeatZRotation);

            await AnimateChickenTransform(
                chicken,
                startPosition,
                peakPosition,
                startRotation,
                fallenRotation,
                _chickenDefeatRiseDuration,
                true,
                cancellationToken);

            await AnimateChickenTransform(
                chicken,
                peakPosition,
                landingPosition,
                fallenRotation,
                fallenRotation,
                _chickenDefeatFallDuration,
                false,
                cancellationToken);

            chicken.localPosition = landingPosition;
            chicken.localRotation = fallenRotation;
        }

        private async UniTask RaiseTyrannoWithCamera(CancellationToken cancellationToken)
        {
            if (_playerAnimator == null)
            {
                return;
            }

            Transform tyranno = _playerAnimator.transform;
            Vector3 tyrannoStartPosition = tyranno.position;
            Vector3 tyrannoTargetPosition = tyrannoStartPosition;
            tyrannoTargetPosition.y = _tyrannoWinDanceY;

            Camera mainCamera = Camera.main;
            Vector3 cameraStartPosition = mainCamera != null
                ? mainCamera.transform.position
                : Vector3.zero;
            Vector3 cameraTargetPosition = cameraStartPosition
                                           + Vector3.up
                                           * (tyrannoTargetPosition.y - tyrannoStartPosition.y);

            if (mainCamera != null)
            {
                var brain = mainCamera.GetComponent<Unity.Cinemachine.CinemachineBrain>();
                if (brain != null)
                {
                    brain.enabled = false;
                }
            }

            float elapsed = 0f;
            while (elapsed < _tyrannoWinRiseDuration)
            {
                cancellationToken.ThrowIfCancellationRequested();

                elapsed += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsed / _tyrannoWinRiseDuration);
                float easedTime = Mathf.SmoothStep(0f, 1f, normalizedTime);

                tyranno.position = Vector3.LerpUnclamped(
                    tyrannoStartPosition,
                    tyrannoTargetPosition,
                    easedTime);

                if (mainCamera != null)
                {
                    mainCamera.transform.position = Vector3.LerpUnclamped(
                        cameraStartPosition,
                        cameraTargetPosition,
                        easedTime);

                    Vector3 lookTarget =
                        tyranno.position + Vector3.up * _tyrannoWinCameraLookOffsetY;
                    Vector3 lookDirection = lookTarget - mainCamera.transform.position;
                    if (lookDirection.sqrMagnitude > 0.0001f)
                    {
                        mainCamera.transform.rotation = Quaternion.LookRotation(
                            lookDirection,
                            Vector3.up);
                    }
                }

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            tyranno.position = tyrannoTargetPosition;
        }

        private async UniTask MoveCameraToBattlefieldWideShot(
            CancellationToken cancellationToken)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null || _camController == null || _camController.idleCam == null)
            {
                return;
            }

            // 상승 연출에서 수동 제어 중인 Main Camera를 전장 전체용 IdleCam 위치로 이동한다.
            var brain = mainCamera.GetComponent<Unity.Cinemachine.CinemachineBrain>();
            if (brain != null)
            {
                brain.enabled = false;
            }

            Vector3 startPosition = mainCamera.transform.position;
            Quaternion startRotation = mainCamera.transform.rotation;
            Vector3 targetPosition = _camController.idleCam.transform.position;
            Quaternion targetRotation = _camController.idleCam.transform.rotation;

            float elapsed = 0f;
            while (elapsed < _chickenDefeatWideShotDuration)
            {
                cancellationToken.ThrowIfCancellationRequested();

                elapsed += Time.deltaTime;
                float normalizedTime =
                    Mathf.Clamp01(elapsed / _chickenDefeatWideShotDuration);
                float easedTime = Mathf.SmoothStep(0f, 1f, normalizedTime);

                mainCamera.transform.position = Vector3.LerpUnclamped(
                    startPosition,
                    targetPosition,
                    easedTime);
                mainCamera.transform.rotation = Quaternion.SlerpUnclamped(
                    startRotation,
                    targetRotation,
                    easedTime);

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            mainCamera.transform.position = targetPosition;
            mainCamera.transform.rotation = targetRotation;
        }

        private static async UniTask AnimateChickenTransform(
            Transform chicken,
            Vector3 fromPosition,
            Vector3 toPosition,
            Quaternion fromRotation,
            Quaternion toRotation,
            float duration,
            bool easeOut,
            CancellationToken cancellationToken)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                cancellationToken.ThrowIfCancellationRequested();

                elapsed += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsed / duration);
                float easedTime = easeOut
                    ? 1f - Mathf.Pow(1f - normalizedTime, 2f)
                    : normalizedTime * normalizedTime;

                chicken.localPosition = Vector3.LerpUnclamped(
                    fromPosition,
                    toPosition,
                    easedTime);
                chicken.localRotation = Quaternion.SlerpUnclamped(
                    fromRotation,
                    toRotation,
                    easedTime);

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
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

        private BattleAnimationSelection PlayBattleAnimations(
            GameResult result,
            string configuredPlayerWinTrigger = null)
        {
            if (_playerAnimator != null)
            {
                switch (result)
                {
                    case GameResult.Win:
                        string playerWinTrigger = string.IsNullOrWhiteSpace(configuredPlayerWinTrigger)
                            ? GetRandomTrigger(_playerWinTriggers, "Win")
                            : configuredPlayerWinTrigger;
                        _playerAnimator.SetTrigger(playerWinTrigger);
                        if (_opponentAnimator != null)
                            SetTriggerIfConfigured(_opponentAnimator, _opponentLoseTrigger);
                        return new BattleAnimationSelection(true, true, playerWinTrigger);
                    case GameResult.Lose:
                        SetTriggerIfConfigured(_playerAnimator, _playerLoseTrigger);
                        break;
                    case GameResult.Draw:
                        SetTriggerIfConfigured(_playerAnimator, _playerDrawTrigger);
                        break;
                }
            }

            if (_opponentAnimator != null)
            {
                switch (result)
                {
                    case GameResult.Win:
                        Debug.LogError("[Tyranno] SetTrigger: Lose");
                        break;
                    case GameResult.Lose:
                        string opponentWinTrigger = GetRandomTrigger(_opponentWinTriggers, "Win");
                        Debug.LogError($"[Tyranno] SetTrigger: {opponentWinTrigger}");
                        _opponentAnimator.SetTrigger(opponentWinTrigger);
                        return new BattleAnimationSelection(true, false, opponentWinTrigger);
                    case GameResult.Draw:
                        Debug.LogError("[Tyranno] SetTrigger: Draw");
                        SetTriggerIfConfigured(_opponentAnimator, _opponentDrawTrigger);
                        break;
                }
            }

            return new BattleAnimationSelection(false, false, string.Empty);
        }

        private static string GetRandomTrigger(string[] triggers, string fallback)
        {
            if (triggers == null || triggers.Length == 0)
            {
                return fallback;
            }

            string trigger = triggers[Random.Range(0, triggers.Length)];
            return string.IsNullOrWhiteSpace(trigger) ? fallback : trigger;
        }

        private static void SetTriggerIfConfigured(Animator animator, string triggerName)
        {
            if (animator != null && !string.IsNullOrWhiteSpace(triggerName))
            {
                animator.SetTrigger(triggerName);
            }
        }

        private int GetCurrentStateHash(Animator animator)
        {
            if (animator == null)
            {
                return 0;
            }

            return animator.GetCurrentAnimatorStateInfo(AnimationLayerIndex).fullPathHash;
        }

        private async UniTask WaitForBattleAnimationsToFinish(
            int playerStateHashBeforeBattle,
            int opponentStateHashBeforeBattle,
            CancellationToken cancellationToken)
        {
            await UniTask.WhenAll(
                WaitForAnimatorStateCycle(_playerAnimator, playerStateHashBeforeBattle, cancellationToken),
                WaitForAnimatorStateCycle(_opponentAnimator, opponentStateHashBeforeBattle, cancellationToken)
            );
        }

        private async UniTask WaitForAnimatorStateCycle(
            Animator animator,
            int previousStateHash,
            CancellationToken cancellationToken)
        {
            if (animator == null)
            {
                return;
            }

            float waitForEnterTimeout = 1.5f;
            float waitForEndTimeout = Mathf.Max(1f, _danceAnimationDuration + 1f);
            float elapsed = 0f;
            int enteredStateHash = 0;

            while (elapsed < waitForEnterTimeout)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!animator.IsInTransition(AnimationLayerIndex))
                {
                    AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(AnimationLayerIndex);
                    if (stateInfo.fullPathHash != previousStateHash)
                    {
                        enteredStateHash = stateInfo.fullPathHash;
                        break;
                    }
                }

                elapsed += Time.deltaTime;
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            if (enteredStateHash == 0)
            {
                return;
            }

            elapsed = 0f;
            while (elapsed < waitForEndTimeout)
            {
                cancellationToken.ThrowIfCancellationRequested();

                bool isInTransition = animator.IsInTransition(AnimationLayerIndex);
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(AnimationLayerIndex);

                if (stateInfo.fullPathHash != enteredStateHash)
                {
                    return;
                }

                if (!isInTransition && stateInfo.normalizedTime >= 1f)
                {
                    return;
                }

                elapsed += Time.deltaTime;
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
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

        private void SetHealthUIVisible(bool isVisible)
        {
            if (_gameHealthCanvas != null)
                _gameHealthCanvas.gameObject.SetActive(isVisible);
        }

        private void SetBattleUIVisible(bool isVisible)
        {
            SetHealthUIVisible(isVisible);

            if (_rpsSelectCanvas != null)
                _rpsSelectCanvas.SetActive(isVisible);
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
            {
                // 선택 중에는 색만 바꾸고 스케일은 원래대로 유지 (팝은 확정 시에만)
                targetImage.color = _selectedColor;
            }
        }

        /// <summary>
        /// 선택이 최종 확정됐을 때만 해당 아이콘을 원래 스케일의 1.2배로 팝 했다가 원래대로 복귀
        /// </summary>
        private void PlayRPSSelectionPop(HandType handType)
        {
            var targetImage = handType switch
            {
                HandType.Rock => _rpsRockImage,
                HandType.Paper => _rpsPaperImage,
                HandType.Scissors => _rpsScissorsImage,
                _ => null
            };

            if (targetImage == null) return;

            var rt = targetImage.rectTransform;
            var originalScale = GetRPSOriginalScale(handType);
            rt.DOKill();
            rt.localScale = originalScale;
            rt.DOScale(originalScale * 1.2f, 0.12f).SetLoops(2, LoopType.Yoyo);
        }


        private void ResetRPSColors()
        {
            // 색만 기본으로, 스케일은 씬에 배치된 원래 값으로 복원 (고정값 사용 안 함)
            ResetRPSImage(_rpsRockImage, _rpsRockOriginalScale);
            ResetRPSImage(_rpsPaperImage, _rpsPaperOriginalScale);
            ResetRPSImage(_rpsScissorsImage, _rpsScissorsOriginalScale);
        }

        private void ResetRPSImage(Image image, Vector3 originalScale)
        {
            if (image == null) return;
            image.color = _defaultColor;
            image.rectTransform.DOKill();
            image.rectTransform.localScale = originalScale;
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

            var selectedImage = _selectedHand.Value switch
            {
                HandType.Rock => _rpsRockImage,
                HandType.Paper => _rpsPaperImage,
                HandType.Scissors => _rpsScissorsImage,
                _ => null
            };

            if (selectedImage == null) return;

            // 원래 스케일 기준으로 승/패 강조 (이긴 손 확대, 진 손 축소)
            var originalScale = GetRPSOriginalScale(_selectedHand.Value);
            var rt = selectedImage.rectTransform;
            rt.DOKill();

            if (result == GameResult.Win)
            {
                selectedImage.color = winGlow;
                rt.localScale = originalScale * 1.3f;
            }
            else if (result == GameResult.Lose)
            {
                selectedImage.color = loseGray;
                rt.localScale = originalScale * 0.8f;
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
                if (_hasTyrannoOriginalPosition)
                {
                    _playerAnimator.transform.position = _tyrannoOriginalPosition;
                }

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
