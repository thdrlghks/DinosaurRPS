using UnityEngine;

/// <summary>
/// 타격 시 파티클 이펙트를 스폰하는 트리거.
/// 캐릭터에 붙여서 사용. 파티클 프리팹은 Inspector에서 연결.
/// </summary>
public class ImpactVFX : MonoBehaviour
{
    [Header("VFX Prefabs")]
    [Tooltip("피격 시 생성할 파티클 프리팹 (없으면 스킵)")]
    [SerializeField] private GameObject _hitVFXPrefab;
    [Tooltip("승리 시 생성할 파티클 프리팹 (없으면 스킵)")]
    [SerializeField] private GameObject _winVFXPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private Vector3 _spawnOffset = new Vector3(0f, 1.5f, 0f);
    [SerializeField, Range(1f, 5f)] private float _vfxLifetime = 2f;

    /// <summary>
    /// 피격 이펙트 생성
    /// </summary>
    public void PlayHitVFX()
    {
        SpawnVFX(_hitVFXPrefab);
    }

    /// <summary>
    /// 승리 이펙트 생성
    /// </summary>
    public void PlayWinVFX()
    {
        SpawnVFX(_winVFXPrefab);
    }

    private void SpawnVFX(GameObject prefab)
    {
        if (prefab == null) return;

        Vector3 pos = _spawnPoint != null
            ? _spawnPoint.position
            : transform.position + _spawnOffset;

        var vfx = Instantiate(prefab, pos, Quaternion.identity);
        Destroy(vfx, _vfxLifetime);
    }
}
