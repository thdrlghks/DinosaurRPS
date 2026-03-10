using Managers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;
using Entry = UnityEngine.EventSystems.EventTrigger.Entry;

namespace DefaultNamespace
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

        private bool _hasTransitioned = false;

        private void Start()
        {
            if (_videoPlayer == null)
            {
                _videoPlayer = GetComponent<VideoPlayer>();
            }

            if (_videoPlayer != null)
            {
                _videoPlayer.loopPointReached += OnVideoEnd;
                _videoPlayer.Play();
                Debug.Log("Start Video");
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
            }

            if (_skipButton != null)
            {
                var trigger = _skipButton.gameObject.GetComponent<EventTrigger>();
                if (trigger != null) Destroy(trigger);
            }
        }
    }
}