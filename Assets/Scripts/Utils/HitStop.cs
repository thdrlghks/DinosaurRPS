using System.Collections;
using UnityEngine;

/// <summary>
/// 적중 시 역경직(Hit Stop) 효과.
/// TimeScale을 잠시 0으로 멈춘 후 복구.
/// </summary>
public class HitStop : MonoBehaviour
{
    private static HitStop _instance;

    public static HitStop Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("HitStop");
                _instance = go.AddComponent<HitStop>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private Coroutine _currentHitStop;

    /// <summary>
    /// 히트스톱 실행
    /// </summary>
    /// <param name="duration">멈추는 시간 (실제 시간 기준, 기본 0.07초)</param>
    /// <param name="timeScale">멈출 때의 TimeScale (기본 0.0)</param>
    public void Play(float duration = 0.07f, float timeScale = 0f)
    {
        if (_currentHitStop != null)
            StopCoroutine(_currentHitStop);

        _currentHitStop = StartCoroutine(HitStopCoroutine(duration, timeScale));
    }

    private IEnumerator HitStopCoroutine(float duration, float timeScale)
    {
        float originalTimeScale = Time.timeScale;
        Time.timeScale = timeScale;

        // unscaledTime 사용 (timeScale이 0이어도 동작)
        yield return new WaitForSecondsRealtime(duration);

        Time.timeScale = originalTimeScale;
        _currentHitStop = null;
    }
}
