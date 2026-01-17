using System;
using System.Collections;
using Core.Enums;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Managers
{
    public class SceneController : MonoBehaviour
    {
        private static SceneController _instance;
        public static SceneController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<SceneController>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("SceneController");
                        _instance = go.AddComponent<SceneController>();
                    }
                }
                return _instance;
            }
        }

        [Header("Scene Names")]
        [SerializeField] private string _mainMenuScene = "MainMenu";
        [SerializeField] private string _storyIntroScene = "StoryIntro";
        [SerializeField] private string _quarterFinalsScene = "QuarterFinals";
        [SerializeField] private string _semiFinalsScene = "SemiFinals";
        [SerializeField] private string _finalRoundScene = "FinalRound";
        
        [Header("Animation Scene Names")]
        [SerializeField] private string _semiFinalsAnimationScene = "SemiFinalsAnimation";
        [SerializeField] private string _finalRoundAnimationScene = "FinalRoundAnimation";
        [SerializeField] private string _victoryAnimationScene = "VictoryAnimation";
        [SerializeField] private string _defeatAnimationScene = "DefeatAnimation";
        
        [Header("Settings")]
        [SerializeField] private float _animationSceneDisplayTime = 3f;
        
        [Header("Loading UI (Optional)")]
        [SerializeField] private GameObject _loadingScreenPrefab;
        
        private GameObject _currentLoadingScreen;
        private bool _isLoading = false;
        
        public event Action<string> OnSceneLoadStarted;
        public event Action<string> OnSceneLoadCompleted;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        public void LoadScene(string sceneName)
        {
            if (_isLoading)
            {
                return;
            }
            
            if (string.IsNullOrEmpty(sceneName))
            {
                return;
            }
            
            OnSceneLoadStarted?.Invoke(sceneName);
            StartCoroutine(LoadSceneAsync(sceneName));
        }
        
        private IEnumerator LoadSceneAsync(string sceneName)
        {
            _isLoading = true;
            ShowLoadingScreen();
            
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            
            if (asyncLoad == null)
            {
                Debug.LogError($"Failed to load scene: {sceneName}");
                HideLoadingScreen();
                _isLoading = false;
                yield break;
            }
            
            while (!asyncLoad.isDone)
            {
                float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
                yield return null;
            }
            
            HideLoadingScreen();
            _isLoading = false;
            OnSceneLoadCompleted?.Invoke(sceneName);
        }
        
        public void LoadMainMenu()
        {
            LoadScene(_mainMenuScene);
        }
        
        public void LoadStoryIntro()
        {
            LoadScene(_storyIntroScene);
        }
        
        public void StartTournament()
        {
            LoadScene(_quarterFinalsScene);
        }
        
        public void ReloadCurrentScene()
        {
            string currentScene = SceneManager.GetActiveScene().name;
            LoadScene(currentScene);
        }
        
        public void LoadTournamentStage(TournamentStage stage)
        {
            string sceneName = stage switch
            {
                TournamentStage.QuarterFinals => _quarterFinalsScene,
                TournamentStage.SemiFinals => _semiFinalsScene,
                TournamentStage.Finals => _finalRoundScene,
                _ => null
            };
            
            if (!string.IsNullOrEmpty(sceneName))
            {
                LoadScene(sceneName);
            }
            else
            {
                Debug.LogError($"No scene found for tournament stage: {stage}");
            }
        }
        
        public void HandleTournamentResult(bool isWin, TournamentStage currentStage)
        {
            if (isWin)
            {
                StartCoroutine(HandleTournamentVictory(currentStage));
            }
            else
            {
                StartCoroutine(HandleTournamentDefeat());
            }
        }
        
        private IEnumerator HandleTournamentVictory(TournamentStage currentStage)
        {
            switch (currentStage)
            {
                case TournamentStage.QuarterFinals:
                    LoadScene(_semiFinalsAnimationScene);
                    yield return new WaitForSeconds(_animationSceneDisplayTime);
                    LoadScene(_semiFinalsScene);
                    break;
                    
                case TournamentStage.SemiFinals:
                    LoadScene(_finalRoundAnimationScene);
                    yield return new WaitForSeconds(_animationSceneDisplayTime);
                    LoadScene(_finalRoundScene);
                    break;
                    
                case TournamentStage.Finals:
                    LoadScene(_victoryAnimationScene);
                    yield return new WaitForSeconds(_animationSceneDisplayTime);
                    LoadMainMenu();
                    break;
            }
        }
        
        private IEnumerator HandleTournamentDefeat()
        {
            LoadScene(_defeatAnimationScene);
            yield return new WaitForSeconds(_animationSceneDisplayTime);
            LoadMainMenu();
        }
        
        public string GetCurrentSceneName()
        {
            return SceneManager.GetActiveScene().name;
        }
        
        public bool IsLoading()
        {
            return _isLoading;
        }
        
        public void QuitGame()
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
        
        private void ShowLoadingScreen()
        {
            if (_loadingScreenPrefab != null && _currentLoadingScreen == null)
            {
                _currentLoadingScreen = Instantiate(_loadingScreenPrefab);
                DontDestroyOnLoad(_currentLoadingScreen);
            }
        }
        
        private void HideLoadingScreen()
        {
            if (_currentLoadingScreen != null)
            {
                Destroy(_currentLoadingScreen);
                _currentLoadingScreen = null;
            }
        }
    }
}