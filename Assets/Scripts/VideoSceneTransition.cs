using System;
using System.Collections;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Managers
{
    public class VideoSceneTransition : MonoBehaviour
    {
        [Header("Video Settings")]
        [SerializeField] private VideoPlayer _videoPlayer;
        [Header("Scene Settings")]
        [SerializeField] private string _nextSceneName = "SemiFinals";
        [Header("Next UI (shown after playback)")]
        [SerializeField] private Image _skipButton;
        [SerializeField] private Sprite _nextButtonIdleSprite;
        [SerializeField] private Sprite _nextButtonClickedSprite;
        [SerializeField] private bool _handleCompletionKeyboard = true;
        private const float NextSceneDelay = .5f;

        [Header("Playback Recovery")]
        [SerializeField, Min(1f)] private float _prepareTimeout = 15f;
        [SerializeField, Min(1f)] private float _playbackStallTimeout = 15f;
        [SerializeField] private string _mainMenuSceneName = "00MainMenu";
        [SerializeField] private TMP_FontAsset _recoveryFont;
        [Header("Diagnostics")]
        [Tooltip("Logs dropped frames and clock resyncs. Errors always enable recovery.")]
        [SerializeField] private bool _enablePlaybackDiagnostics = true;

        private enum PlaybackState { Idle, Preparing, Playing, Completed, Failed }
        private PlaybackState _state;
        private bool _started, _restartOnEnable, _hasTransitioned, _clickAccepted, _resumeVideo, _awaitingInputRelease;
        private int _attempt, _droppedFrameCount, _clockResyncCount;
        private float _prepareElapsed, _stallElapsed;
        private long _lastObservedFrame = -1;
        private Button _nextButton;
        private Coroutine _prepareRoutine, _transitionRoutine;
        private VideoPlayer _subscribedPlayer;
        private VideoPlayer.EventHandler _preparedHandler, _endedHandler, _droppedHandler;
        private VideoPlayer.ErrorEventHandler _errorHandler;
        private VideoPlayer.TimeEventHandler _resyncHandler;
        private VideoRecoveryView _recovery;

        public bool IsPlaybackComplete => _state == PlaybackState.Completed;
        public bool HasPlaybackFailed => _state == PlaybackState.Failed;
        public bool CanReturnToMainMenu => (HasPlaybackFailed || IsPlaybackComplete) && CanAcceptInput();
        public string PlaybackFailureReason { get; private set; } = string.Empty;

        private void OnEnable()
        {
            GamePause.Changed += OnPauseChanged;
            if (!_started || _hasTransitioned) return;
            _awaitingInputRelease = true;
            if (_restartOnEnable) BeginPlayback();
            else if (HasPlaybackFailed) ShowRecovery();
            else if (IsPlaybackComplete && _skipButton != null) _skipButton.gameObject.SetActive(true);
        }

        private void Start()
        {
            _started = true;
            BeginPlayback();
        }

        private void OnDisable()
        {
            GamePause.Changed -= OnPauseChanged;
            _restartOnEnable = _state == PlaybackState.Preparing || _state == PlaybackState.Playing;
            CancelTransition();
            StopAttempt();
            if (_nextButton != null) _nextButton.interactable = false;
            if (_recovery != null) _recovery.Hide();
        }

        private bool ConfigureNextButton()
        {
            if (_nextButton != null) _nextButton.onClick.RemoveListener(SkipVideo);
            if (_skipButton == null || _nextButtonIdleSprite == null || _nextButtonClickedSprite == null) return false;
            _nextButton = _skipButton.GetComponent<Button>();
            if (_nextButton == null) _nextButton = _skipButton.gameObject.AddComponent<Button>();
            _nextButton.targetGraphic = _skipButton;
            _nextButton.transition = Selectable.Transition.None;
            _nextButton.navigation = new Navigation { mode = Navigation.Mode.None };
            _nextButton.interactable = false;
            _nextButton.onClick.AddListener(SkipVideo);
            _skipButton.sprite = _nextButtonIdleSprite;
            _skipButton.preserveAspect = true;
            _skipButton.gameObject.SetActive(false);
            return true;
        }

        private void BeginPlayback()
        {
            CancelTransition();
            StopAttempt();
            _restartOnEnable = false;
            _state = PlaybackState.Idle;
            PlaybackFailureReason = string.Empty;
            _prepareElapsed = _stallElapsed = 0f;
            _lastObservedFrame = -1;
            _droppedFrameCount = _clockResyncCount = 0;
            if (_recovery != null) _recovery.Hide();
            if (!ConfigureNextButton())
            {
                FailPlayback("다음 화면 버튼을 표시하지 못했어요.\n아래의 계속 또는 메인 메뉴를 선택해 주세요.");
                return;
            }
            if (_videoPlayer == null) _videoPlayer = GetComponent<VideoPlayer>();
            if (_videoPlayer == null)
            {
                FailPlayback("영상 재생기를 찾지 못했어요.\n다시 시도하거나 다음 화면으로 이동할 수 있어요.");
                return;
            }
            if ((_videoPlayer.source == VideoSource.VideoClip && _videoPlayer.clip == null) ||
                (_videoPlayer.source == VideoSource.Url && string.IsNullOrWhiteSpace(_videoPlayer.url)))
            {
                FailPlayback("영상 파일을 찾지 못했어요.\n다시 시도하거나 다음 화면으로 이동할 수 있어요.");
                return;
            }
            var firstFrame = _videoPlayer.GetComponent<VideoFirstFrameDisplay>();
            if (firstFrame != null && !firstFrame.IsConfigured)
            {
                FailPlayback("영상 화면을 표시하지 못했어요.\n다시 시도하거나 다음 화면으로 이동할 수 있어요.");
                return;
            }
            _videoPlayer.playOnAwake = false;
            _videoPlayer.isLooping = false;
            _videoPlayer.waitForFirstFrame = true;
            _state = PlaybackState.Preparing;
            // Detach and Stop before waiting a frame, so old decoder events can drain.
            _prepareRoutine = StartCoroutine(PrepareNextFrame(_attempt));
        }

        private IEnumerator PrepareNextFrame(int attempt)
        {
            yield return null;
            _prepareRoutine = null;
            if (!IsCurrentAttempt(_videoPlayer, attempt) || _state != PlaybackState.Preparing) yield break;
            SubscribePlayback(attempt);
            _videoPlayer.GetComponent<VideoFirstFrameDisplay>()?.ResetForPlayback();
            try { _videoPlayer.Prepare(); }
            catch (Exception exception)
            {
                FailPlayback("영상을 준비하지 못했어요.\n다시 시도하거나 다음 화면으로 이동할 수 있어요.", exception.Message);
            }
        }

        private bool IsCurrentAttempt(VideoPlayer player, int attempt) =>
            isActiveAndEnabled && !_hasTransitioned && attempt == _attempt && player != null && player == _videoPlayer;

        private void SubscribePlayback(int attempt)
        {
            var player = _videoPlayer;
            _subscribedPlayer = player;
            _preparedHandler = vp => { if (IsCurrentAttempt(vp, attempt)) OnVideoPrepared(vp); };
            _endedHandler = vp => { if (IsCurrentAttempt(vp, attempt)) OnVideoEnd(vp); };
            _errorHandler = (vp, message) => { if (IsCurrentAttempt(vp, attempt)) OnVideoError(vp, message); };
            player.prepareCompleted += _preparedHandler;
            player.loopPointReached += _endedHandler;
            // Error recovery is independent of optional diagnostic logging.
            player.errorReceived += _errorHandler;
            if (!_enablePlaybackDiagnostics) return;
            _droppedHandler = vp => { if (IsCurrentAttempt(vp, attempt)) OnVideoFrameDropped(vp); };
            _resyncHandler = (vp, seconds) => { if (IsCurrentAttempt(vp, attempt)) OnVideoClockResync(vp, seconds); };
            player.frameDropped += _droppedHandler;
            player.clockResyncOccurred += _resyncHandler;
        }

        private void StopAttempt()
        {
            ++_attempt;
            if (_prepareRoutine != null) StopCoroutine(_prepareRoutine);
            _prepareRoutine = null;
            if (_subscribedPlayer != null)
            {
                _subscribedPlayer.prepareCompleted -= _preparedHandler;
                _subscribedPlayer.loopPointReached -= _endedHandler;
                _subscribedPlayer.errorReceived -= _errorHandler;
                if (_droppedHandler != null) _subscribedPlayer.frameDropped -= _droppedHandler;
                if (_resyncHandler != null) _subscribedPlayer.clockResyncOccurred -= _resyncHandler;
            }
            _subscribedPlayer = null;
            _preparedHandler = _endedHandler = _droppedHandler = null;
            _errorHandler = null;
            _resyncHandler = null;
            _resumeVideo = false;
            if (_videoPlayer != null)
            {
                _videoPlayer.GetComponent<VideoFirstFrameDisplay>()?.HoldPlaceholder();
                _videoPlayer.Stop();
            }
        }

        private void OnVideoPrepared(VideoPlayer player)
        {
            if (_state != PlaybackState.Preparing || player != _videoPlayer || !player.isPrepared) return;
            _state = PlaybackState.Playing;
            _stallElapsed = 0f;
            _lastObservedFrame = -1;
            _resumeVideo = GamePause.IsPaused;
            if (!_resumeVideo) PlayPreparedVideo();
        }

        private void PlayPreparedVideo()
        {
            if (_videoPlayer == null || _state != PlaybackState.Playing) return;
            try { _videoPlayer.Play(); }
            catch (Exception exception)
            {
                FailPlayback("영상을 재생하지 못했어요.\n다시 시도하거나 다음 화면으로 이동할 수 있어요.", exception.Message);
            }
        }

        private void OnVideoEnd(VideoPlayer player)
        {
            if (_state != PlaybackState.Playing || player != _videoPlayer || _hasTransitioned) return;
            _state = PlaybackState.Completed;
            _resumeVideo = false;
            _awaitingInputRelease = true;
            if (_enablePlaybackDiagnostics)
                Debug.Log($"[Video] Playback finished. droppedFrames={_droppedFrameCount}, " +
                          $"clockResyncs={_clockResyncCount}, frame={player.frame}, time={player.time:F2}s", this);
            if (_skipButton == null || _nextButton == null)
            {
                FailPlayback("영상이 끝났지만 다음 화면 버튼을 표시하지 못했어요.\n아래의 계속 또는 메인 메뉴를 선택해 주세요.");
                return;
            }
            _skipButton.sprite = _nextButtonIdleSprite;
            _skipButton.gameObject.SetActive(true);
            UpdateInteraction();
        }

        private void Update()
        {
            if (_awaitingInputRelease && !GamePause.BlocksGameplayInput && RecoveryInputsReleased())
                _awaitingInputRelease = false;
            UpdateInteraction();
            UpdateWatchdog();
            if (!CanAcceptInput()) return;
            if (HasPlaybackFailed)
            {
                if (Input.GetKeyDown(KeyCode.R)) RetryVideo();
                else if (Input.GetKeyDown(KeyCode.M)) ReturnToMainMenu();
                else if (AdvancePressed()) ContinueAfterError();
            }
            else if (_handleCompletionKeyboard && IsPlaybackComplete && AdvancePressed()) SkipVideo();
        }

        private void UpdateWatchdog()
        {
            if (_hasTransitioned || _clickAccepted || GamePause.IsPaused) return;
            if (_state != PlaybackState.Preparing && _state != PlaybackState.Playing) return;
            if (_videoPlayer == null)
            {
                FailPlayback("영상 재생기가 중단되었어요.\n다시 시도하거나 다음 화면으로 이동할 수 있어요.");
                return;
            }
            if (_state == PlaybackState.Preparing)
            {
                _prepareElapsed += Time.unscaledDeltaTime;
                if (_prepareElapsed >= Mathf.Max(1f, _prepareTimeout))
                    FailPlayback("영상 준비가 오래 걸리고 있어요.\n다시 시도하거나 다음 화면으로 이동할 수 있어요.");
                return;
            }
            long frame = _videoPlayer.frame;
            if (frame >= 0 && frame != _lastObservedFrame)
            {
                _lastObservedFrame = frame;
                _stallElapsed = 0f;
            }
            else
            {
                // Playback time can advance despite a stalled decoder. Watch actual frames.
                _stallElapsed += Time.unscaledDeltaTime;
                if (_stallElapsed >= Mathf.Max(1f, _playbackStallTimeout))
                    FailPlayback("영상 재생이 멈췄어요.\n다시 시도하거나 다음 화면으로 이동할 수 있어요.");
            }
        }

        private bool CanAcceptInput() => isActiveAndEnabled && !_hasTransitioned && !_clickAccepted &&
            !_awaitingInputRelease && !GamePause.BlocksGameplayInput && !Input.GetKeyDown(KeyCode.Escape);
        private static bool AdvancePressed() => Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space);
        private static bool RecoveryInputsReleased() => !Input.GetKey(KeyCode.Return) &&
            !Input.GetKey(KeyCode.KeypadEnter) && !Input.GetKey(KeyCode.Space) && !Input.GetKey(KeyCode.R) &&
            !Input.GetKey(KeyCode.M) && !Input.GetKey(KeyCode.Escape) && !Input.GetMouseButton(0);

        private void UpdateInteraction()
        {
            bool canInput = CanAcceptInput();
            if (_nextButton != null) _nextButton.interactable = IsPlaybackComplete && canInput;
            if (_recovery != null) _recovery.SetInteraction(HasPlaybackFailed && canInput, !GamePause.IsPaused);
        }

        // Existing serialized callbacks retain this name; it never skips normal playback.
        public void SkipVideo()
        {
            if (IsPlaybackComplete && CanAcceptInput()) RequestTransition(_nextSceneName, true);
        }
        public void RetryVideo()
        {
            if (!HasPlaybackFailed || !CanAcceptInput()) return;
            _awaitingInputRelease = true;
            BeginPlayback();
        }
        public void ContinueAfterError()
        {
            if (HasPlaybackFailed && CanAcceptInput()) RequestTransition(_nextSceneName, false);
        }
        public void ReturnToMainMenu()
        {
            if (CanReturnToMainMenu) RequestTransition(_mainMenuSceneName, false);
        }
        private static bool CanLoadScene(string scene) =>
            !string.IsNullOrWhiteSpace(scene) && Application.CanStreamedLevelBeLoaded(scene);

        private void RequestTransition(string scene, bool showClickedArtwork)
        {
            if (!CanLoadScene(scene))
            {
                FailPlayback("선택한 화면을 열 수 없어요.\n다시 시도하거나 메인 메뉴를 선택해 주세요.", "Unavailable scene: " + scene);
                return;
            }
            _clickAccepted = true;
            if (showClickedArtwork && _skipButton != null) _skipButton.sprite = _nextButtonClickedSprite;
            UpdateInteraction();
            _transitionRoutine = StartCoroutine(TransitionAfterClick(scene));
        }

        private IEnumerator TransitionAfterClick(string scene)
        {
            // Preserve the existing half-second clicked artwork and final frame.
            yield return new WaitForSecondsRealtime(NextSceneDelay);
            while (GamePause.BlocksGameplayInput) yield return null;
            _transitionRoutine = null;
            if (_hasTransitioned || !isActiveAndEnabled) yield break;
            try
            {
                var controller = SceneController.Instance;
                if (controller != null && controller.IsLoading())
                {
                    _clickAccepted = false;
                    FailPlayback("다른 화면으로 이동 중이에요.\n잠시 후 다시 선택해 주세요.");
                }
                else
                {
                    _hasTransitioned = true;
                    if (controller != null) controller.LoadScene(scene);
                    else SceneManager.LoadScene(scene);
                }
            }
            catch (Exception exception)
            {
                _hasTransitioned = false;
                _clickAccepted = false;
                FailPlayback("다음 화면을 열지 못했어요.\n계속 또는 메인 메뉴를 다시 선택해 주세요.", exception.Message);
            }
        }

        private void CancelTransition()
        {
            if (_transitionRoutine != null) StopCoroutine(_transitionRoutine);
            _transitionRoutine = null;
            _clickAccepted = false;
            if (_skipButton != null) _skipButton.sprite = _nextButtonIdleSprite;
        }
        private void OnPauseChanged(bool paused)
        {
            _awaitingInputRelease = true;
            UpdateInteraction();
            if (_state != PlaybackState.Playing || _videoPlayer == null) return;
            if (paused)
            {
                _resumeVideo = _videoPlayer.isPlaying || _resumeVideo;
                if (_videoPlayer.isPlaying) _videoPlayer.Pause();
            }
            else if (_resumeVideo)
            {
                _resumeVideo = false;
                PlayPreparedVideo();
            }
        }
        private void FailPlayback(string message, string diagnostic = null)
        {
            if (_hasTransitioned || !isActiveAndEnabled) return;
            _state = PlaybackState.Failed;
            PlaybackFailureReason = message;
            _awaitingInputRelease = true;
            CancelTransition();
            StopAttempt();
            if (_skipButton != null) _skipButton.gameObject.SetActive(false);
            Debug.LogWarning("[Video] Recovery: " + (diagnostic ?? message.Replace("\n", " ")), this);
            ShowRecovery();
        }
        private void ShowRecovery()
        {
            if (_recovery == null)
            {
                _recovery = gameObject.AddComponent<VideoRecoveryView>();
                _recovery.Initialize(this, _recoveryFont);
            }
            _recovery.Show(PlaybackFailureReason, CanLoadScene(_nextSceneName), CanLoadScene(_mainMenuSceneName));
            UpdateInteraction();
        }
        private void OnVideoError(VideoPlayer player, string message)
        {
            if (player != _videoPlayer || (_state != PlaybackState.Preparing && _state != PlaybackState.Playing)) return;
            FailPlayback("영상을 재생하지 못했어요.\n다시 시도하거나 다음 화면으로 이동할 수 있어요.",
                $"Decoder error: {message} (clip={GetClipName(player)})");
        }
        private void OnVideoFrameDropped(VideoPlayer player)
        {
            _droppedFrameCount++;
            if (_droppedFrameCount <= 5 || _droppedFrameCount % 30 == 0)
                Debug.LogWarning($"[Video] Frame dropped #{_droppedFrameCount} " +
                                 $"(clip={GetClipName(player)}, frame={player.frame}, time={player.time:F2}s)", this);
        }
        private void OnVideoClockResync(VideoPlayer player, double seconds)
        {
            _clockResyncCount++;
            if (_clockResyncCount <= 5 || _clockResyncCount % 30 == 0)
                Debug.LogWarning($"[Video] Clock resync #{_clockResyncCount}: {seconds:F3}s " +
                                 $"(clip={GetClipName(player)}, frame={player.frame})", this);
        }
        private static string GetClipName(VideoPlayer player) =>
            player != null && player.clip != null ? player.clip.name : "URL/Unknown";
        private void OnDestroy()
        {
            if (_nextButton != null) _nextButton.onClick.RemoveListener(SkipVideo);
        }
    }
}
