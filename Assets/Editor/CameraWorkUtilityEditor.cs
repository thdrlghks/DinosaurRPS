using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 카메라 연출 유틸리티
/// 원하는 오브젝트에 붙이고 인스펙터에서 설정 후
/// 코드에서 Play___() 메서드를 호출하거나 타임라인/이벤트에서 직접 호출 가능
/// </summary>
public class CameraWorkUtility : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  카메라 참조
    // ─────────────────────────────────────────────
    [Header("카메라 참조")]
    [Tooltip("비워두면 Camera.main 자동 사용")]
    public Camera targetCamera;

    // ─────────────────────────────────────────────
    //  1. 카메라 쉐이크 (Camera Shake)
    // ─────────────────────────────────────────────
    [Header("━━ 1. 카메라 쉐이크 ━━")]
    [Tooltip("쉐이크 강도 (픽셀 단위 이동 거리)")]
    [Range(0f, 3f)] public float shakeStrength = 0.3f;
    [Tooltip("쉐이크 지속 시간 (초)")]
    [Range(0f, 2f)] public float shakeDuration = 0.4f;
    [Tooltip("쉐이크 진동 횟수 (높을수록 잔진동)")]
    [Range(1, 50)] public int shakeVibrato = 20;
    [Tooltip("쉐이크 랜덤성 (0=일정 방향, 90=완전 랜덤)")]
    [Range(0f, 180f)] public float shakeRandomness = 90f;
    [Tooltip("X축만 쉐이크 (수평 충격)")]
    public bool shakeHorizontalOnly = false;
    [Tooltip("쉐이크 후 원위치 복귀 여부")]
    public bool shakeSnapBack = true;

    // ─────────────────────────────────────────────
    //  2. 줌 펀치 (Zoom Punch)
    // ─────────────────────────────────────────────
    [Header("━━ 2. 줌 펀치 ━━")]
    [Tooltip("FOV 줄어드는 양 (앞으로 돌진 느낌, 음수면 줌아웃)")]
    [Range(-30f, 30f)] public float zoomPunchFOV = -8f;
    [Tooltip("줌 펀치 지속 시간 (초)")]
    [Range(0f, 1f)] public float zoomPunchDuration = 0.25f;
    [Tooltip("줌 펀치 Ease 종류")]
    public Ease zoomPunchEase = Ease.OutQuart;

    // ─────────────────────────────────────────────
    //  3. FOV 버스트 (FOV Burst) — 충격파 느낌
    // ─────────────────────────────────────────────
    [Header("━━ 3. FOV 버스트 (충격파) ━━")]
    [Tooltip("순간 넓어지는 FOV 양")]
    [Range(0f, 40f)] public float fovBurstAmount = 15f;
    [Tooltip("넓어지는 시간 (초)")]
    [Range(0f, 0.5f)] public float fovBurstInDuration = 0.08f;
    [Tooltip("복귀하는 시간 (초)")]
    [Range(0f, 1f)] public float fovBurstOutDuration = 0.4f;
    public Ease fovBurstOutEase = Ease.OutCubic;

    // ─────────────────────────────────────────────
    //  4. 더치 틸트 (Dutch Tilt) — 긴장/패배감
    // ─────────────────────────────────────────────
    [Header("━━ 4. 더치 틸트 ━━")]
    [Tooltip("기울어지는 Z축 각도 (양수=오른쪽 기울기)")]
    [Range(-30f, 30f)] public float dutchTiltAngle = 12f;
    [Tooltip("틸트되는 시간 (초)")]
    [Range(0f, 1f)] public float dutchTiltInDuration = 0.3f;
    [Tooltip("틸트 유지 시간 (초)")]
    [Range(0f, 5f)] public float dutchTiltHoldDuration = 1.5f;
    [Tooltip("원위치 복귀 시간 (초)")]
    [Range(0f, 2f)] public float dutchTiltOutDuration = 0.6f;
    public Ease dutchTiltEase = Ease.InOutSine;

    // ─────────────────────────────────────────────
    //  5. 돌리 푸시인 (Dolly Push In) — 인트로/집중
    // ─────────────────────────────────────────────
    [Header("━━ 5. 돌리 푸시인 ━━")]
    [Tooltip("이동 방향 및 거리 (로컬 좌표계 기준)")]
    public Vector3 dollyMoveOffset = new Vector3(0f, 0f, 2f);
    [Tooltip("이동 시간 (초)")]
    [Range(0f, 5f)] public float dollyDuration = 1.5f;
    public Ease dollyEase = Ease.InOutQuad;
    [Tooltip("완료 후 원위치 복귀 여부")]
    public bool dollyAutoReturn = false;
    [Tooltip("복귀 시간 (초)")]
    [Range(0f, 5f)] public float dollyReturnDuration = 1.0f;

    // ─────────────────────────────────────────────
    //  6. 빅토리 오빗 (Victory Orbit) — 승리 회전
    // ─────────────────────────────────────────────
    [Header("━━ 6. 빅토리 오빗 ━━")]
    [Tooltip("회전 중심 타겟 (비워두면 원점 기준)")]
    public Transform orbitTarget;
    [Tooltip("회전 반지름")]
    [Range(0.5f, 20f)] public float orbitRadius = 5f;
    [Tooltip("오빗 높이 (Y 오프셋)")]
    [Range(-5f, 10f)] public float orbitHeight = 1.5f;
    [Tooltip("총 회전 각도 (도)")]
    [Range(0f, 720f)] public float orbitTotalAngle = 180f;
    [Tooltip("오빗 총 시간 (초)")]
    [Range(0f, 10f)] public float orbitDuration = 3f;
    [Tooltip("항상 타겟을 바라볼지 여부")]
    public bool orbitLookAtTarget = true;
    public Ease orbitEase = Ease.InOutSine;

    // ─────────────────────────────────────────────
    //  7. 슬로우 줌인 (Cinematic Slow Zoom) — 긴장 고조
    // ─────────────────────────────────────────────
    [Header("━━ 7. 시네마틱 슬로우 줌 ━━")]
    [Tooltip("목표 FOV (작을수록 더 줌인)")]
    [Range(20f, 100f)] public float slowZoomTargetFOV = 45f;
    [Tooltip("슬로우 줌 시간 (초)")]
    [Range(0f, 10f)] public float slowZoomDuration = 2.5f;
    public Ease slowZoomEase = Ease.InSine;
    [Tooltip("줌 완료 후 원래 FOV로 복귀 여부")]
    public bool slowZoomAutoReturn = false;

    // ─────────────────────────────────────────────
    //  8. 레터박스 (Letterbox) — 시네마틱 바
    // ─────────────────────────────────────────────
    [Header("━━ 8. 레터박스 ━━")]
    [Tooltip("레터박스 표시에 사용할 UI 패널 (위아래 검은 바)")]
    public RectTransform letterboxTop;
    public RectTransform letterboxBottom;
    [Tooltip("바 높이 (화면 픽셀 기준)")]
    [Range(0f, 200f)] public float letterboxBarHeight = 80f;
    [Tooltip("등장 시간 (초)")]
    [Range(0f, 2f)] public float letterboxInDuration = 0.4f;
    [Tooltip("퇴장 시간 (초)")]
    [Range(0f, 2f)] public float letterboxOutDuration = 0.4f;

    // ─────────────────────────────────────────────
    //  내부 상태
    // ─────────────────────────────────────────────
    private float _originalFOV;
    private Vector3 _originalPosition;
    private Quaternion _originalRotation;
    private CancellationTokenSource _cts;

    // ─────────────────────────────────────────────
    //  초기화
    // ─────────────────────────────────────────────
    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        CacheOriginals();
    }

    private void CacheOriginals()
    {
        if (targetCamera == null) return;
        _originalFOV = targetCamera.fieldOfView;
        _originalPosition = targetCamera.transform.localPosition;
        _originalRotation = targetCamera.transform.localRotation;
    }

    private void OnDestroy()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }

    // ─────────────────────────────────────────────
    //  PUBLIC API — 인스펙터 버튼 또는 코드에서 호출
    // ─────────────────────────────────────────────

    /// <summary>패 공개, 결정타 순간에 사용</summary>
    [ContextMenu("▶ 카메라 쉐이크 테스트")]
    public void PlayShake()
    {
        if (targetCamera == null) return;
        KillAll();

        Vector3 shakeDir = shakeHorizontalOnly
            ? new Vector3(1, 0, 0)
            : Vector3.one;

        targetCamera.transform
            .DOShakePosition(shakeDuration, shakeStrength * shakeDir,
                shakeVibrato, shakeRandomness, snapping: false,
                fadeOut: shakeSnapBack)
            .SetLink(targetCamera.gameObject);
    }

    /// <summary>카운트다운 마지막 박자, 라운드 시작에 사용</summary>
    [ContextMenu("▶ 줌 펀치 테스트")]
    public void PlayZoomPunch()
    {
        if (targetCamera == null) return;
        KillAll();

        float targetFOV = targetCamera.fieldOfView + zoomPunchFOV;
        targetFOV = Mathf.Clamp(targetFOV, 10f, 170f);

        DOTween.Sequence()
            .Append(targetCamera.DOFieldOfView(targetFOV, zoomPunchDuration * 0.4f)
                .SetEase(Ease.OutQuart))
            .Append(targetCamera.DOFieldOfView(_originalFOV, zoomPunchDuration * 0.6f)
                .SetEase(zoomPunchEase))
            .SetLink(targetCamera.gameObject);
    }

    /// <summary>결정타, 큰 충격 순간에 사용 — 쉐이크와 함께 쓰면 굿</summary>
    [ContextMenu("▶ FOV 버스트 테스트")]
    public void PlayFOVBurst()
    {
        if (targetCamera == null) return;
        KillAll();

        float burstFOV = _originalFOV + fovBurstAmount;
        burstFOV = Mathf.Clamp(burstFOV, 10f, 170f);

        DOTween.Sequence()
            .Append(targetCamera.DOFieldOfView(burstFOV, fovBurstInDuration)
                .SetEase(Ease.OutCubic))
            .Append(targetCamera.DOFieldOfView(_originalFOV, fovBurstOutDuration)
                .SetEase(fovBurstOutEase))
            .SetLink(targetCamera.gameObject);
    }

    /// <summary>패배 판정, 긴장감 높은 장면에 사용</summary>
    [ContextMenu("▶ 더치 틸트 테스트")]
    public async void PlayDutchTilt()
    {
        if (targetCamera == null) return;
        KillAll();

        var ct = RefreshCTS();
        try
        {
            var tf = targetCamera.transform;
            var tiltRotation = Quaternion.Euler(
                tf.localEulerAngles.x,
                tf.localEulerAngles.y,
                dutchTiltAngle);

            tf.DOLocalRotateQuaternion(tiltRotation, dutchTiltInDuration)
                .SetEase(dutchTiltEase);
            await UniTask.Delay(TimeSpan.FromSeconds(dutchTiltInDuration), cancellationToken: ct);

            await UniTask.Delay(TimeSpan.FromSeconds(dutchTiltHoldDuration), cancellationToken: ct);

            tf.DOLocalRotateQuaternion(_originalRotation, dutchTiltOutDuration)
                .SetEase(dutchTiltEase);
            await UniTask.Delay(TimeSpan.FromSeconds(dutchTiltOutDuration), cancellationToken: ct);
        }
        catch (OperationCanceledException) { }
    }

    /// <summary>인트로, 캐릭터 소개, 결승 진입 씬에 사용</summary>
    [ContextMenu("▶ 돌리 푸시인 테스트")]
    public async void PlayDollyPushIn()
    {
        if (targetCamera == null) return;
        KillAll();

        var ct = RefreshCTS();
        try
        {
            var tf = targetCamera.transform;
            Vector3 targetPos = _originalPosition + tf.TransformDirection(dollyMoveOffset);

            tf.DOLocalMove(targetPos, dollyDuration).SetEase(dollyEase);
            await UniTask.Delay(TimeSpan.FromSeconds(dollyDuration), cancellationToken: ct);

            if (dollyAutoReturn)
            {
                tf.DOLocalMove(_originalPosition, dollyReturnDuration).SetEase(dollyEase);
                await UniTask.Delay(TimeSpan.FromSeconds(dollyReturnDuration), cancellationToken: ct);
            }
        }
        catch (OperationCanceledException) { }
    }

    /// <summary>승리 확정 후 축하 연출에 사용</summary>
    [ContextMenu("▶ 빅토리 오빗 테스트")]
    public async void PlayVictoryOrbit()
    {
        if (targetCamera == null) return;
        KillAll();

        var ct = RefreshCTS();
        try
        {
            Vector3 center = orbitTarget != null
                ? orbitTarget.position
                : Vector3.zero;

            float startAngle = 0f;
            float endAngle = orbitTotalAngle;
            float elapsed = 0f;
            // 시작 위치 각도 계산
            Vector3 startOffset = targetCamera.transform.position - center;
            startOffset.y = 0;
            startAngle = Mathf.Atan2(startOffset.z, startOffset.x) * Mathf.Rad2Deg;

            while (elapsed < orbitDuration && !ct.IsCancellationRequested)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / orbitDuration);
                float easedT = DOVirtual.EasedValue(0f, 1f, t, orbitEase);
                float angle = (startAngle + easedT * endAngle) * Mathf.Deg2Rad;

                Vector3 newPos = center + new Vector3(
                    Mathf.Cos(angle) * orbitRadius,
                    orbitHeight,
                    Mathf.Sin(angle) * orbitRadius);

                targetCamera.transform.position = newPos;

                if (orbitLookAtTarget)
                    targetCamera.transform.LookAt(center + Vector3.up * (orbitHeight * 0.5f));

                await UniTask.NextFrame(PlayerLoopTiming.Update, ct);
            }
        }
        catch (OperationCanceledException) { }
    }

    /// <summary>패 공개 전 긴장감 조성, 4강/결승 인트로에 사용</summary>
    [ContextMenu("▶ 시네마틱 슬로우 줌 테스트")]
    public async void PlaySlowZoom()
    {
        if (targetCamera == null) return;
        KillAll();

        var ct = RefreshCTS();
        try
        {
            targetCamera.DOFieldOfView(slowZoomTargetFOV, slowZoomDuration)
                .SetEase(slowZoomEase);
            await UniTask.Delay(TimeSpan.FromSeconds(slowZoomDuration), cancellationToken: ct);

            if (slowZoomAutoReturn)
            {
                float returnDur = slowZoomDuration * 0.5f;
                targetCamera.DOFieldOfView(_originalFOV, returnDur).SetEase(Ease.OutSine);
                await UniTask.Delay(TimeSpan.FromSeconds(returnDur), cancellationToken: ct);
            }
        }
        catch (OperationCanceledException) { }
    }

    /// <summary>레터박스 등장 (시네마틱 시작 시 호출)</summary>
    [ContextMenu("▶ 레터박스 IN 테스트")]
    public async void PlayLetterboxIn()
    {
        if (letterboxTop == null || letterboxBottom == null) return;
        var ct = RefreshCTS();
        try
        {
            letterboxTop.gameObject.SetActive(true);
            letterboxBottom.gameObject.SetActive(true);

            letterboxTop.DOAnchorPosY(0f, letterboxInDuration).SetEase(Ease.OutCubic);
            letterboxBottom.DOAnchorPosY(0f, letterboxInDuration).SetEase(Ease.OutCubic);
            await UniTask.Delay(TimeSpan.FromSeconds(letterboxInDuration), cancellationToken: ct);
        }
        catch (OperationCanceledException) { }
    }

    /// <summary>레터박스 퇴장 (시네마틱 종료 시 호출)</summary>
    [ContextMenu("▶ 레터박스 OUT 테스트")]
    public async void PlayLetterboxOut()
    {
        if (letterboxTop == null || letterboxBottom == null) return;
        var ct = RefreshCTS();
        try
        {
            letterboxTop.DOAnchorPosY(letterboxBarHeight, letterboxOutDuration).SetEase(Ease.InCubic);
            letterboxBottom.DOAnchorPosY(-letterboxBarHeight, letterboxOutDuration).SetEase(Ease.InCubic);
            await UniTask.Delay(TimeSpan.FromSeconds(letterboxOutDuration), cancellationToken: ct);

            letterboxTop.gameObject.SetActive(false);
            letterboxBottom.gameObject.SetActive(false);
        }
        catch (OperationCanceledException) { }
    }

    // ─────────────────────────────────────────────
    //  콤보 연출 — 자주 쓰이는 조합을 미리 묶어놓음
    // ─────────────────────────────────────────────

    /// <summary>패 공개 순간 — FOV버스트 + 쉐이크 동시</summary>
    [ContextMenu("▶ [콤보] 패 공개 임팩트")]
    public void PlayHandRevealImpact()
    {
        PlayFOVBurst();
        PlayShake();
    }

    /// <summary>패배 판정 — 더치틸트 + 슬로우 줌인</summary>
    [ContextMenu("▶ [콤보] 패배 드라마틱")]
    public async void PlayDefeatDramatic()
    {
        PlayDutchTilt();
        await UniTask.Delay(TimeSpan.FromSeconds(0.1f));
        PlaySlowZoom();
    }

    /// <summary>승리 확정 — 줌펀치 → 레터박스IN → 오빗</summary>
    [ContextMenu("▶ [콤보] 승리 셀레브레이션")]
    public async void PlayVictoryCelebration()
    {
        PlayZoomPunch();
        await UniTask.Delay(TimeSpan.FromSeconds(0.3f));
        PlayLetterboxIn();
        await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
        PlayVictoryOrbit();
    }

    /// <summary>씬 전환 전 FOV 원위치 복귀 (씬 전환 직전에 호출 권장)</summary>
    public void ResetToOriginal(float duration = 0.3f)
    {
        KillAll();
        if (targetCamera == null) return;

        targetCamera.DOFieldOfView(_originalFOV, duration).SetEase(Ease.OutSine);
        targetCamera.transform.DOLocalMove(_originalPosition, duration).SetEase(Ease.OutSine);
        targetCamera.transform.DOLocalRotateQuaternion(_originalRotation, duration).SetEase(Ease.OutSine);
    }

    /// <summary>외부에서 originalFOV/Position/Rotation을 현재 값으로 업데이트</summary>
    public void BakeCurrentAsOriginal()
    {
        CacheOriginals();
    }

    // ─────────────────────────────────────────────
    //  내부 헬퍼
    // ─────────────────────────────────────────────
    private void KillAll()
    {
        if (targetCamera != null)
        {
            DOTween.Kill(targetCamera);
            DOTween.Kill(targetCamera.transform);
        }
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private CancellationToken RefreshCTS()
    {
        _cts = new CancellationTokenSource();
        return _cts.Token;
    }
}