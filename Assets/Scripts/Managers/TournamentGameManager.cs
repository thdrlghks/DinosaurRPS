using System.Threading;
using Core.Data;
using Core.Enums;
using Core.Interfaces;
using Cysharp.Threading.Tasks;
using DefaultNamespace;
using DefaultNamespace.Gameplay;
using Gameplay;
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
        [SerializeField] private Canvas _rpsSelectCanvas;

        [Header("Player & Opponent")] [SerializeField]
        private Animator _playerAnimator;

        [SerializeField] private Animator _opponentAnimator;
        [SerializeField] private GameObject _playerCanvas;
        [SerializeField] private GameObject _opponentCanvas;

        [Header("RPS Selection UI")] [SerializeField]
        private GameObject _rockImage;

        [SerializeField] private GameObject _rockText;
        [SerializeField] private GameObject _paperImage;
        [SerializeField] private GameObject _paperText;
        [SerializeField] private GameObject _scissorsImage;
        [SerializeField] private GameObject _scissorsText;

        [Header("RoundStartUI")] [SerializeField]
        private GameObject _roundStartUIRock;

        [SerializeField] private GameObject _roundStartUIPaper;
        [SerializeField] private GameObject _roundStartUIScissors;
        [SerializeField] private GameObject _balloon;

        [Header("Camera & Settings")] [SerializeField]
        private CAMController _camController;

        [SerializeField] private float _resultDisplayTime = 2f;

        //Private Value
        private HandType? _selectedHand;
        private IGameJudge _gameJudge;
        private IOpponentHandGenerator _opponentAI;
        private bool _canInput;
        private HandType _currentDecisionRps;
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
            //await UniTask.Delay(2000);
            _canInput = true;
            _gameHealthCanvas.gameObject.SetActive(true);
            _rpsSelectCanvas.gameObject.SetActive(true);
            StartRound().Forget();
        }

        #endregion

        #region PlayCanvasIntroAnimation

        private async UniTask PlayCanvasIntroAnimation()
        {
            _sfxManager.PlayFightSound();
            var playerRect = _playerCanvas.GetComponent<RectTransform>();
            var opponentRect = _opponentCanvas.GetComponent<RectTransform>();
            float screenWidth = playerRect?.parent.GetComponent<RectTransform>().rect.width ?? Screen.width;

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
                Debug.Log("W key pressed!");
                SelectHand(HandType.Paper);
                
            }
            else if (Input.GetKeyDown(KeyCode.E))
            {
                Debug.Log("E key pressed!");
                SelectHand(HandType.Scissors);
                
            }
        }

        #endregion

        private void SelectHand(HandType handType)
        {
            Debug.Log($"SelectHand called - handType: {handType}, canInput: {_canInput}, isCountingDown: {_isCountingDown}");
            if (!_isCountingDown && !_canInput) return;

            _selectedHand = handType;

            HighlightSelectedHand(handType);
        }

        #region ColorChange

        private void HighlightSelectedHand(HandType selectedHand)
        {
            ResetHandColors();

            switch (selectedHand)
            {
                case HandType.Rock:
                    _rockImage.GetComponent<UnityEngine.UI.Image>().color = Color.red;
                    _rockText.GetComponent<UnityEngine.UI.Image>().color = Color.red;
                    break;
                case HandType.Paper:
                    _paperImage.GetComponent<UnityEngine.UI.Image>().color = Color.red;
                    _paperText.GetComponent<UnityEngine.UI.Image>().color = Color.red;
                    break;
                case HandType.Scissors:
                    _scissorsImage.GetComponent<UnityEngine.UI.Image>().color = Color.red;
                    _scissorsText.GetComponent<UnityEngine.UI.Image>().color = Color.red;
                    break;
            }
        }

        private void ResetHandColors()
        {
            _rockImage.GetComponent<UnityEngine.UI.Image>().color = Color.white;
            _rockText.GetComponent<UnityEngine.UI.Image>().color = Color.white;

            _paperImage.GetComponent<UnityEngine.UI.Image>().color = Color.white;
            _paperText.GetComponent<UnityEngine.UI.Image>().color = Color.white;

            _scissorsImage.GetComponent<UnityEngine.UI.Image>().color = Color.white;
            _scissorsText.GetComponent<UnityEngine.UI.Image>().color = Color.white;
        }

        #endregion

        private async UniTaskVoid StartRound()
        {
            if (_roundInProgress) return;

            _roundInProgress = true;

            _camController.playIntro = true;

            await UniTask.Delay(500);
            //카메라 딜레이 시간 생각해야함. 

            await StartCountdownAndBattle(this.GetCancellationTokenOnDestroy());
        }

        #region StartCountdownAndBattle

        private async UniTask StartCountdownAndBattle(CancellationToken cancellationToken)
        {
            _isCountingDown = true;
            _canInput = false;
            _selectedHand = null;
            ResetHandColors();

            _balloon.SetActive(true);

            var countdownTask = ExecuteCountdownAnimations(cancellationToken);

            await countdownTask;

            _isCountingDown = false;

            if (!_selectedHand.HasValue)
            {
                _selectedHand = (HandType)Random.Range(0, 3);
            }

            HighlightSelectedHand(_selectedHand.Value);

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
                await UITweenUtil.ScaleUpAndFadeOutAsync(
                    _roundStartUIScissors.transform,
                    new Vector3(1.2f, 1.2f, 1f),
                    new Vector3(3.6f, 3.6f, 3f),
                    2f,
                    0.5f,
                    cancellationToken
                );
            }

            if (_roundStartUIRock)
            {
                await UITweenUtil.ScaleUpAndFadeOutAsync(
                    _roundStartUIRock.transform,
                    new Vector3(1.2f, 1.2f, 1f),
                    new Vector3(3.6f, 3.6f, 3f),
                    2f,
                    0.5f,
                    cancellationToken
                );
            }

            if (_roundStartUIPaper)
            {
                await UITweenUtil.ScaleUpAndFadeOutAsync(
                    _roundStartUIPaper.transform,
                    new Vector3(1.2f, 1.2f, 1f),
                    new Vector3(3.6f, 3.6f, 3f),
                    2f,
                    0.5f,
                    cancellationToken
                );
            }

            if (_balloon)
            {
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
            if (_uiManager != null)
            {
                _uiManager.ShowBattleResult(playerHand, opponentHand, result);
            }

            PlayBattleAnimations(result);
            UpdateHealthBars(result);
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
                await UniTask.Delay((int)(_resultDisplayTime * 1000), cancellationToken: cancellationToken);
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
                ResetHandColors();
                _canInput = true;
                StartRound().Forget();
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

        private void PlayBattleAnimations(GameResult result)
        {
            if (_playerAnimator != null)
            {
                switch (result)
                {
                    case GameResult.Win:
                        _playerAnimator.SetTrigger("Win");
                        //camController.playWin = true;
                        break;
                    case GameResult.Lose:
                        _playerAnimator.SetTrigger("Defeat");
                        //camController.playLose = true;
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
                        _opponentAnimator.SetTrigger("Defeat");
                        break;
                    case GameResult.Lose:
                        _opponentAnimator.SetTrigger("Victory");
                        break;
                    case GameResult.Draw:
                        _opponentAnimator.SetTrigger("Draw");
                        break;
                }
            }
        }

        #endregion

        #region ResetAnimations

        private void ResetAnimations()
        {
            if (_playerAnimator != null)
            {
                _playerAnimator.SetTrigger("Idle");
                //camController.returnMainCam = true;
            }

            if (_opponentAnimator != null)
            {
                _opponentAnimator.SetTrigger("Idle");
            }
        }

        #endregion

        #region UpdateHealthBars

        private void UpdateHealthBars(GameResult result)
        {
            if (_uiManager != null)
            {
                var playerHealth = GetMaxHealthForStage(_currentStage, true) - _matchData.OpponentWins;
                var opponentHealth = GetMaxHealthForStage(_currentStage, false) - _matchData.PlayerWins;

                _uiManager.UpdateHealthBars(playerHealth, opponentHealth);
            }
        }

        #endregion

        #region HandleMatchEnd

        private async UniTask HandleMatchEnd(CancellationToken cancellationToken)
        {
            GameResult? winner = _matchData.GetWinner();

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