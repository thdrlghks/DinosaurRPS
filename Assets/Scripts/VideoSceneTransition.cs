using Managers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;
using Entry = UnityEngine.EventSystems.EventTrigger.Entry;

namespace Managers
{
    public class VideoSceneTransition : MonoBehaviour
    {
        [Header("Video Settings")]
        [SerializeField] private VideoPlayer _videoPlayer;

        [Header("Scene Settings")]
        [SerializeField] private string _nextSceneName = "SemiFinals";
        [SerializeField] private bool _allowSkip = true;

        [Header("Skip UI")]
        [SerializeField] private Image _skipButton;
        [SerializeField] private bool _showButtonAfterVideo = true;

        [Header("Loading Cover")]
        [Tooltip("영상이 준비되기 전 씬을 가려줄 풀스크린 검은 이미지")]
        [SerializeField] private Image _loadingCover;

        private bool _hasTransitioned = false;

        private void Start()
        {
            if (_videoPlayer == null)
            {
                _videoPlayer = GetComponent<VideoPlayer>();
            }

            // 준비되기 전 씬이 보이지 않도록 먼저 덮는다.
            if (_loadingCover != null)
                _loadingCover.gameObject.SetActive(true);

            if (_videoPlayer != null)
            {
                _videoPlayer.loopPointReached += OnVideoEnd;

                // 첫 프레임 디코딩 대기 후 재생 -> 씬 노출(플래시) 방지
                _videoPlayer.playOnAwake = false;
                _videoPlayer.waitForFirstFrame = true;
                _videoPlayer.prepareCompleted += OnVideoPrepared;
                _videoPlayer.Prepare();
                Debug.Log("Prepare Video");
            }
            else
            {
                Debug.LogError("VideoPlayer not found!");
            }

            if (_skipButton != null)
            {
                if (_showButtonAfterVideo)
                    _skipButton.gameObject.SetActive(false);

                var trigger = _skipButton.gameObject.AddComponent<EventTrigger>();
                var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
                entry.callback.AddListener(_ => SkipVideo());
                trigger.triggers.Add(entry);
            }
        }

        private void OnVideoPrepared(VideoPlayer vp)
        {
            // 첫 프레임까지 준비 완료 -> 재생 시작하고 가림막 제거
            vp.Play();

            if (_loadingCover != null)
                _loadingCover.gameObject.SetActive(false);

            Debug.Log("Start Video");
        }

        private void OnVideoEnd(VideoPlayer vp)
        {
            if (_showButtonAfterVideo && _skipButton != null)
            {
                _skipButton.gameObject.SetActive(true);
                return;
            }

            LoadNextScene();
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
            if (_allowSkip && Input.GetKeyDown(KeyCode.Space))
            {
                SkipVideo();
            }
        }

        public void SkipVideo()
        {
            if (_videoPlayer != null)
            {
                _videoPlayer.Stop();
            }
            LoadNextScene();
        }

        private void OnDestroy()
        {
            if (_videoPlayer != null)
            {
                _videoPlayer.loopPointReached -= OnVideoEnd;
                _videoPlayer.prepareCompleted -= OnVideoPrepared;
            }

            if (_skipButton != null)
            {
                var trigger = _skipButton.gameObject.GetComponent<EventTrigger>();
                if (trigger != null) Destroy(trigger);
            }
        }
    }
}