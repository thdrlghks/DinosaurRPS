using System.Collections;
using UnityEngine;

/// <summary>
/// 피격 시 캐릭터의 모든 Renderer를 잠시 흰색으로 플래시.
/// 캐릭터 루트 오브젝트에 붙여서 사용.
/// </summary>
public class HitFlash : MonoBehaviour
{
    [Header("Flash Settings")]
    [SerializeField, Range(0.02f, 0.3f)] private float _flashDuration = 0.1f;
    [SerializeField] private Color _flashColor = Color.white;
    [SerializeField, Range(1, 5)] private int _flashCount = 2;

    private Renderer[] _renderers;
    private MaterialPropertyBlock _propBlock;
    private Coroutine _flashCoroutine;

    // Shader property IDs
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    private void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>();
        _propBlock = new MaterialPropertyBlock();
    }

    /// <summary>
    /// 플래시 실행. 외부에서 호출.
    /// </summary>
    public void Play(float intensityMultiplier = 1f)
    {
        if (_flashCoroutine != null)
            StopCoroutine(_flashCoroutine);
        _flashCoroutine = StartCoroutine(FlashCoroutine(intensityMultiplier));
    }

    private IEnumerator FlashCoroutine(float intensityMultiplier)
    {
        Color flashTarget = _flashColor * intensityMultiplier;

        for (int i = 0; i < _flashCount; i++)
        {
            // Flash ON
            SetFlash(flashTarget);
            yield return new WaitForSecondsRealtime(_flashDuration);

            // Flash OFF
            ClearFlash();
            if (i < _flashCount - 1)
                yield return new WaitForSecondsRealtime(_flashDuration * 0.5f);
        }

        _flashCoroutine = null;
    }

    private void SetFlash(Color color)
    {
        foreach (var renderer in _renderers)
        {
            if (renderer == null) continue;
            renderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor(EmissionColor, color);
            renderer.SetPropertyBlock(_propBlock);
        }
    }

    private void ClearFlash()
    {
        foreach (var renderer in _renderers)
        {
            if (renderer == null) continue;
            renderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor(EmissionColor, Color.black);
            renderer.SetPropertyBlock(_propBlock);
        }
    }
}
