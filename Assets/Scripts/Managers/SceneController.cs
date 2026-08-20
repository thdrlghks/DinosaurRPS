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
        // 전체 흐름:
        // 00MainMenu →(StartGame) 01Start(오프닝) →영상→ 02Forest(튜토리얼)
        //   →승리→ 03End80(닭 패배) →영상→ QuarterFinals(8강)
        //   →승리→ 04End8 →영상→ 05Start4 →영상→ 06Lava(4강)
        //   →승리→ 07End4 →영상→ 08Start2 →영상→ FinalRound(결승)
        //   →승리→ 10End2 →영상→ VictoryAnimation(굿 엔딩)
        //   →결승 패배→ BadAnimation(배드 엔딩)
        // 컷신 간 전환은 각 컷신 씬의 VideoSceneTransition(_nextSceneName)이 담당하고,
        // 전투 종료 후 "다음 컷신 한 편"만 이 컨트롤러가 로드한다.
        [SerializeField] private string _mainMenuScene = "00MainMenu";
        [SerializeField] private string _startScene = "01Start";           // 오프닝 컷신
        [Tooltip("닭 튜토리얼 전투 씬 (TournamentStage.Qualifiers)")]
        [SerializeField] private string _tutorialScene = "02Forest";
        [SerializeField] private string _quarterFinalsScene = "QuarterFinals"; // 8강 전투
        [SerializeField] private string _end80Scene = "03End80";           // 닭 패배 컷신
        [SerializeField] private string _end8Scene = "04End8";             // 8강 승리 컷신
        [SerializeField] private string _start4Scene = "05Start4";         // 4강 시작 컷신
        [SerializeField] private string _semiFinalsScene = "06Lava";       // 4강 전투
        [SerializeField] private string _end4Scene = "07End4";             // 4강 승리 컷신
        [SerializeField] private string _start2Scene = "08Start2";         // 결승 시작 컷신
        [SerializeField] private string _finalRoundScene = "FinalRound";   // 결승 전투
        [SerializeField] private string _end2Scene = "10End2";             // 최종 승리 컷신
        [Tooltip("결승 승리 시 최종 승리 컷신 뒤에 재생될 굿 엔딩 씬")]
        [SerializeField] private string _goodEndingScene = "VictoryAnimation";
        [Tooltip("결승 패배 시 재생될 배드 엔딩 씬")]
        [SerializeField] private string _badEndingScene = "BadAnimation";

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
        /// 00MainMenu에서 게임 시작 시 호출 - 01Start(오프닝 컷신)로 이동.
        /// 이후 01Start → 02Forest(튜토리얼) 전환은 01Start의 VideoSceneTransition이 담당한다.
        /// </summary>
        public void StartGame()
        {
            LoadScene(_startScene);
        }

        /// <summary>
        /// 튜토리얼을 건너뛰고 곧바로 8강 전투로 진입 (디버그/테스트용).
        /// </summary>
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
                TournamentStage.Qualifiers => _tutorialScene,          // 02Forest (튜토리얼)
                TournamentStage.QuarterFinals => _quarterFinalsScene,  // QuarterFinals (8강)
                TournamentStage.SemiFinals => _semiFinalsScene,        // 06Lava (4강)
                TournamentStage.Finals => _finalRoundScene,            // FinalRound (결승)
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
                StartCoroutine(HandleTournamentVictory(currentStage));
            else
                StartCoroutine(HandleTournamentDefeat(currentStage));
        }

        // 전투 승리 시 "다음 컷신 한 편"만 로드한다.
        // 컷신 이후 전개(다음 컷신 / 다음 전투)는 각 컷신의 VideoSceneTransition이 이어간다.
        //   튜토리얼 승리 → 03End80(닭 패배) →영상→ QuarterFinals(8강)
        //   8강 승리      → 04End8 →영상→ 05Start4 →영상→ 06Lava(4강)
        //   4강 승리      → 07End4 →영상→ 08Start2 →영상→ FinalRound(결승)
        //   결승 승리     → 10End2(최종 승리) →영상→ VictoryAnimation(굿 엔딩)
        private IEnumerator HandleTournamentVictory(TournamentStage currentStage)
        {
            string nextScene = currentStage switch
            {
                TournamentStage.Qualifiers    => _end80Scene, // 닭 패배 컷신
                TournamentStage.QuarterFinals => _end8Scene,  // 8강 승리 컷신
                TournamentStage.SemiFinals    => _end4Scene,  // 4강 승리 컷신
                TournamentStage.Finals        => _end2Scene,  // 최종 승리 컷신 → (영상) 굿 엔딩
                _ => _mainMenuScene
            };

            yield return new WaitForSeconds(1f);
            yield return LoadSceneAndWait(nextScene);
        }

        // 패배 처리: 결승 패배만 배드 엔딩으로, 그 외 단계 패배는 메인 메뉴로.
        private IEnumerator HandleTournamentDefeat(TournamentStage currentStage)
        {
            yield return new WaitForSeconds(1f);

            string nextScene = currentStage == TournamentStage.Finals
                ? _badEndingScene   // 결승 패배 → 배드 엔딩
                : _mainMenuScene;   // 그 외 패배 → 메인 메뉴

            yield return LoadSceneAndWait(nextScene);
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
