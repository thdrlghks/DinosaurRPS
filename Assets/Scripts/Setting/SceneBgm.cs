using UnityEngine;

/// <summary>
/// 씬에 빈 오브젝트로 하나 놓고 클립만 꽂으면 그 씬의 BGM이 된다.
/// SFXManager가 DontDestroyOnLoad라 씬을 넘어가도 크로스페이드로 자연스럽게 이어진다.
/// </summary>
public class SceneBgm : MonoBehaviour
{
    [SerializeField] private AudioClip _bgm;
    [SerializeField] private bool _loop = true;

    private void Start()
    {
        if (_bgm == null) return;

        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.PlayBgm(_bgm, _loop);
        }
    }
}
