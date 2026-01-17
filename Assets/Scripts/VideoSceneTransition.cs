using Managers;
using UnityEngine;
using UnityEngine.Video;

namespace DefaultNamespace
{
    public class VideoSceneTransition : MonoBehaviour
    {
        [Header("Video Settings")]
        [SerializeField] private VideoPlayer _videoPlayer;
    
        [Header("Scene Settings")]
        [SerializeField] private string _nextSceneName = "SemiFinals";
        [SerializeField] private bool _allowSkip = true;
    
        private void Start()
        {
            if (_videoPlayer == null)
            {
                _videoPlayer = GetComponent<VideoPlayer>();
            }
        
            if (_videoPlayer != null)
            {
                _videoPlayer.loopPointReached += OnVideoEnd;
            }
            else
            {
                Debug.LogError("VideoPlayer not found!");
            }
        }
    
        private void OnVideoEnd(VideoPlayer vp)
        {
            LoadNextScene();
        }
    
        private void LoadNextScene()
        {
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
        }
    }
}