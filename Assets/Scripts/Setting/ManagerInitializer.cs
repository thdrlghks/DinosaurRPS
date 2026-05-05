using UnityEngine;

public static class ManagerInitializer
{
    // 유니티가 씬을 로드하기 직전에 자동으로 딱 한 번 실행합니다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void InitializeManagers()
    {
        // 1. 이미 씬에 매니저가 있는지 확인합니다. (예: 0번 씬에서 정상적으로 시작한 경우)
        // 두 스크립트가 같은 프리팹에 있으므로 SettingsManager 하나만 존재하는지 체크해도 충분합니다.
        if (Object.FindFirstObjectByType<SettingsManager>() != null) return;

        // 2. 매니저가 없다면 (예: 3번 씬에서 바로 플레이를 누른 경우) 프리팹을 불러옵니다.
        GameObject prefab = Resources.Load<GameObject>("SettingController");

        if (prefab != null)
        {
            // 프리팹 생성
            GameObject go = Object.Instantiate(prefab);
            go.name = "SettingController"; // 이름 깔끔하게 정리

            // 생성됨과 동시에 프리팹에 붙어있는 SettingsManager와 SFXManager의 Awake가 실행되며
            // SingletonMonoBehaviour에 의해 자동으로 DontDestroyOnLoad 처리가 됩니다.
        }
        else
        {
            Debug.LogError("Resources 폴더에 'SettingController' 프리팹이 없습니다! 위치와 이름을 확인해주세요.");
        }
    }
}