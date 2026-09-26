using System.Collections;
using UnityEngine;
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
        private const float NextSceneDelay = 0.5f;

        [Header("Diagnostics")]
        [Tooltip("Logs decoder errors, dropped frames, and playback clock resyncs.")]
        [SerializeField] private bool _enablePlaybackDiagnostics = true;

        private bool _hasTransitioned = false;
        private bool _diagnosticsSubscribed;
        private int _droppedFrameCount;
        private int _clockResyncCount;
        private Button _nextButton;
        private bool _videoEnded;
        private bool _clickAccepted;
        private bool _resumeVideo;
        private Coroutine _transitionRoutine;

        private void OnEnable() => GamePause.Changed += OnPauseChanged;

        private void OnDisable()
        {
            GamePause.Changed -= OnPauseChanged;
            if (_transitionRoutine != null)
            {
                StopCoroutine(_transitionRoutine);
                _transitionRoutine = null;
                _clickAccepted = false;
                if (_skipButton != null) _skipButton.sprite = _nextButtonIdleSprite;
            }
        }

        private void Start()
        {
            if (_skipButton == null || _nextButtonIdleSprite == null || _nextButtonClickedSprite == null)
            {
                Debug.LogError("Video Next button and both sprites must be assigned.", this);
                return;
            }
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

            if (_videoPlayer == null)
            {
                _videoPlayer = GetComponent<VideoPlayer>();
            }

            if (_videoPlayer != null)
            {
                _videoPlayer.loopPointReached += OnVideoEnd;
                SubscribePlaybackDiagnostics();

                // 첫 프레임 디코딩 대기 후 재생 -> 씬 노출(플래시) 방지
                _videoPlayer.playOnAwake = false;
                _videoPlayer.isLooping = false;
                _videoPlayer.waitForFirstFrame = true;
                _videoPlayer.prepareCompleted += OnVideoPrepared;
                _videoPlayer.Prepare();
                Debug.Log("Prepare Video");
            }
            else
            {
                Debug.LogError("VideoPlayer not found!");
            }

        }

        private void OnVideoPrepared(VideoPlayer vp)
        {
            // VideoFirstFrameDisplay keeps the still image until a frame is actually rendered.
            _resumeVideo = GamePause.IsPaused;
            if (!_resumeVideo) vp.Play();

            Debug.Log("Start Video");
        }

        private void OnVideoEnd(VideoPlayer vp)
        {
            if (_videoEnded || _hasTransitioned) return;
            _videoEnded = true;
            _resumeVideo = false;
            if (_enablePlaybackDiagnostics)
            {
                Debug.Log(
                    $"[Video] Playback finished. droppedFrames={_droppedFrameCount}, " +
                    $"clockResyncs={_clockResyncCount}, frame={vp.frame}, time={vp.time:F2}s",
                    this);
            }

            _skipButton.sprite = _nextButtonIdleSprite;
            _skipButton.gameObject.SetActive(true);
            _nextButton.interactable = !GamePause.BlocksGameplayInput;
        }

        private void LoadNextScene()
        {
            if (_hasTransitioned) return;
            _hasTransitioned = true;

            if (SceneController.Instance != null)
            {
                SceneController.Instance.LoadScene(_nextSceneName);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(_nextSceneName);
            }
        }

        private void Update()
        {
            if (_nextButton != null)
                _nextButton.interactable = _videoEnded && !_clickAccepted && !GamePause.BlocksGameplayInput;
        }

        // Keep the existing public method name for any serialized UI callbacks.
        public void SkipVideo()
        {
            if (!_videoEnded || _clickAccepted || _hasTransitioned || !isActiveAndEnabled ||
                GamePause.BlocksGameplayInput || Input.GetKeyDown(KeyCode.Escape)) return;

            _clickAccepted = true;
            _nextButton.interactable = false;
            _skipButton.sprite = _nextButtonClickedSprite;
            // Leave the final video frame visible behind the clicked artwork.
            _transitionRoutine = StartCoroutine(TransitionAfterClick());
        }

        private IEnumerator TransitionAfterClick()
        {
            yield return new WaitForSecondsRealtime(NextSceneDelay);
            while (GamePause.BlocksGameplayInput) yield return null;
            _transitionRoutine = null;
            LoadNextScene();
        }

        private void OnPauseChanged(bool paused)
        {
            if (_nextButton != null) _nextButton.interactable = false;
            if (_videoPlayer == null || _videoEnded) return;
            if (paused)
            {
                _resumeVideo = _videoPlayer.isPlaying;
                if (_resumeVideo) _videoPlayer.Pause();
            }
            else if (_resumeVideo)
            {
                _resumeVideo = false;
                _videoPlayer.Play();
            }
        }

        private void SubscribePlaybackDiagnostics()
        {
            if (!_enablePlaybackDiagnostics || _videoPlayer == null || _diagnosticsSubscribed)
                return;

            _videoPlayer.errorReceived += OnVideoError;
            _videoPlayer.frameDropped += OnVideoFrameDropped;
            _videoPlayer.clockResyncOccurred += OnVideoClockResync;
            _diagnosticsSubscribed = true;
        }

        private void OnVideoError(VideoPlayer vp, string message)
        {
            Debug.LogError(
                $"[Video] Decoder error: {message} (clip={GetClipName(vp)}, time={vp.time:F2}s)",
                this);
        }

        private void OnVideoFrameDropped(VideoPlayer vp)
        {
            _droppedFrameCount++;

            // Logging every dropped frame can create more stalls, so sample the warnings.
            if (_droppedFrameCount <= 5 || _droppedFrameCount % 30 == 0)
            {
                Debug.LogWarning(
                    $"[Video] Frame dropped #{_droppedFrameCount} " +
                    $"(clip={GetClipName(vp)}, frame={vp.frame}, time={vp.time:F2}s)",
                    this);
            }
        }

        private void OnVideoClockResync(VideoPlayer vp, double seconds)
        {
            _clockResyncCount++;

            if (_clockResyncCount <= 5 || _clockResyncCount % 30 == 0)
            {
                Debug.LogWarning(
                    $"[Video] Clock resync #{_clockResyncCount}: {seconds:F3}s " +
                    $"(clip={GetClipName(vp)}, frame={vp.frame})",
                    this);
            }
        }

        private static string GetClipName(VideoPlayer vp)
        {
            return vp != null && vp.clip != null ? vp.clip.name : "URL/Unknown";
        }

        private void OnDestroy()
        {
            if (_videoPlayer != null)
            {
                _videoPlayer.loopPointReached -= OnVideoEnd;
                _videoPlayer.prepareCompleted -= OnVideoPrepared;

                if (_diagnosticsSubscribed)
                {
                    _videoPlayer.errorReceived -= OnVideoError;
                    _videoPlayer.frameDropped -= OnVideoFrameDropped;
                    _videoPlayer.clockResyncOccurred -= OnVideoClockResync;
                    _diagnosticsSubscribed = false;
                }
            }

            if (_nextButton != null) _nextButton.onClick.RemoveListener(SkipVideo);
        }
    }
}
