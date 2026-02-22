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
        [SerializeField] private string _mainMenuScene = "00MainMenu";
        [SerializeField] private string _startScene = "01Start";
        [SerializeField] private string _start8Scene = "02Start8";
        [SerializeField] private string _quarterFinalsScene = "03Forest";  // 8강 전투
        [SerializeField] private string _end8Scene = "04End8";
        [SerializeField] private string _start4Scene = "05Start4";
        [SerializeField] private string _semiFinalsScene = "06Lava";       // 4강 전투
        [SerializeField] private string _end4Scene = "07End4";
        [SerializeField] private string _start2Scene = "08Start2";
        [SerializeField] private string _finalRoundScene = "09Space";      // 결승 전투
        [SerializeField] private string _end2Scene = "10End2";             // 최종 승리
        
        [Header("Settings")]
        [SerializeField] private float _animationSceneDisplayTime = 3f;
        [SerializeField] private float _shortAnimationDisplayTime = 2f;
        
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

        /// <summary>
        /// 씬 로딩 완료까지 대기하는 코루틴 버전. 시퀀스 내부에서 사용.
        /// </summary>
        private IEnumerator LoadSceneAndWait(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) yield break;

            OnSceneLoadStarted?.Invoke(sceneName);
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
                yield return null;
            }

            HideLoadingScreen();
            _isLoading = false;
            OnSceneLoadCompleted?.Invoke(sceneName);
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
        
        /// <summary>
        /// 00MainMenu에서 게임 시작 시 호출 - 01Start로 이동
        /// </summary>
        public void StartGame()
        {
            StartCoroutine(StartGameSequence());
        }

        private IEnumerator StartGameSequence()
        {
            // 01Start (시작 애니메이션)
            yield return LoadSceneAndWait(_startScene);
            yield return new WaitForSeconds(_animationSceneDisplayTime);

            // 02Start8 (8강 시작 애니메이션)
            yield return LoadSceneAndWait(_start8Scene);
            yield return new WaitForSeconds(_shortAnimationDisplayTime);

            // 03Forest (8강 전투)
            yield return LoadSceneAndWait(_quarterFinalsScene);
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
                TournamentStage.QuarterFinals => _quarterFinalsScene,  // 03Forest
                TournamentStage.SemiFinals => _semiFinalsScene,        // 06Lava
                TournamentStage.Finals => _finalRoundScene,            // 09Space
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
                    // 03Forest 승리 → 04End8 → 05Start4 → 06Lava
                    yield return LoadSceneAndWait(_end8Scene);
                    yield return new WaitForSeconds(_animationSceneDisplayTime);
                    yield return LoadSceneAndWait(_start4Scene);
                    yield return new WaitForSeconds(_shortAnimationDisplayTime);
                    yield return LoadSceneAndWait(_semiFinalsScene);
                    break;

                case TournamentStage.SemiFinals:
                    // 06Lava 승리 → 07End4 → 08Start2 → 09Space
                    yield return LoadSceneAndWait(_end4Scene);
                    yield return new WaitForSeconds(_animationSceneDisplayTime);
                    yield return LoadSceneAndWait(_start2Scene);
                    yield return new WaitForSeconds(_shortAnimationDisplayTime);
                    yield return LoadSceneAndWait(_finalRoundScene);
                    break;

                case TournamentStage.Finals:
                    // 09Space 승리 → 10End2 (최종 승리)
                    yield return LoadSceneAndWait(_end2Scene);
                    yield return new WaitForSeconds(_animationSceneDisplayTime);
                    yield return LoadSceneAndWait(_mainMenuScene);
                    break;
            }
        }

        private IEnumerator HandleTournamentDefeat()
        {
            // 패배 시 메인 메뉴로 (나중에 별도 패배 씬 추가 가능)
            yield return new WaitForSeconds(1f);
            yield return LoadSceneAndWait(_mainMenuScene);
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