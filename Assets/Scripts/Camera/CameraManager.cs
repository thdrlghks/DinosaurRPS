using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;
using System.Collections;
using System.Threading.Tasks;


public class CameraManager : MonoBehaviour
{
    bool isStartBattle = true;
    public CinemachineCamera introPlayerCam;
    public CinemachineCamera introEnemyCam;
    public CinemachineCamera introZoomOutCam;
    public CinemachineCamera enemyWinCam;
    public CinemachineCamera playerWinCam;
    public CinemachineCamera idleCam;
    [Range(1.2f, 1.8f)] public float resultCameraHoldDuration = 1.5f;

    [Header("�ð� ����")]
    public float waitBeforeStart = 3.1f;   // ���� ��� �ð�
    public float introDuration = 3.0f;    // �� ��Ʈ�� �̵� �ð�
    public float zoomOutDuration = 2.5f;  // �ܾƿ� ���� �ð�

    public float holdBeforeZoomOut = 0f;

    public CameraShake cameraShake;

    [Header("카메라 전환 페이드")]
    [Tooltip("화면 페이드용 검은 Image (CanvasGroup 필요 없음, 풀스크린 Image)")]
    public Image fadeOverlay;
    [Range(0.05f, 0.5f)] public float fadeDuration = 0.15f;

    [Header("결과 카메라 이동 위치")]
    public Vector3 resultCamPosition = new Vector3(-5.76f, 2.57f, -0.18f);
    public Vector3 resultCamRotation = new Vector3(15.708f, 90.049f, -0.013f);
    [Range(0.3f, 2f)] public float resultCamMoveDuration = 0.8f;

    [Header("줌아웃 풀-푸시 (보 이후 카메라 연출)")]
    [Tooltip("줌아웃 시작 시 FOV를 얼마나 넓힐지 (넓힘 = 뒤로 빠지는 느낌)")]
    [Range(0f, 10f)] public float zoomOutPullFOV = 4f;

    private Camera _mainCamera;

    void Start()
    {
        _mainCamera = Camera.main;
        isStartBattle = true;
        StartCoroutine(PlayFullIntroSequence());
    }

    IEnumerator PlayFullIntroSequence()
    {
        // 1. ���� 3�� ���
        if (isStartBattle)
        {
            yield return new WaitForSeconds(waitBeforeStart);
            isStartBattle=false;
        }

        // 2. �÷��̾� ī�޶� ��Ʈ��
        cameraShake.StartDinoSteps(introPlayerCam);
        yield return StartCoroutine(MoveAlongSpline (introPlayerCam, introDuration));

        // 3. �� ī�޶� ��Ʈ��
        cameraShake.StartDinoSteps(introEnemyCam);
        yield return StartCoroutine(MoveAlongSpline(introEnemyCam, introDuration));

        // 4. �߾� �ܾƿ� ���� + �ü� ���߱�
        SwitchCamera(introZoomOutCam);
        if (holdBeforeZoomOut > 0f)
        {
            yield return new WaitForSeconds(holdBeforeZoomOut);
        }

        // ������ ���� ������Ʈ ��������
        var composer = introZoomOutCam.GetComponent<CinemachineRotationComposer>();
        var splineDolly = introZoomOutCam.GetComponent<CinemachineSplineDolly>();
        var settings = introZoomOutCam.GetComponent<CameraMoveSettings>();
        float elapsed = 0f;

        // FOV 풀-푸시: 시작 시 살짝 넓혀두고(뒤로 빠진 느낌) → 줌아웃 동안 원래대로 좁혀옴(앞으로 오는 느낌)
        float originalFOV = introZoomOutCam.Lens.FieldOfView;
        if (zoomOutPullFOV > 0f)
        {
            var lens = introZoomOutCam.Lens;
            lens.FieldOfView = originalFOV + zoomOutPullFOV;
            introZoomOutCam.Lens = lens;
        }

        while (elapsed < zoomOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / zoomOutDuration;
            float progress = settings?.moveCurve.Evaluate(t) ?? t;

            // B. �ü� ���߱� (0���� -1.5f�� ���� ������)
            if (composer != null)
            {
                composer.TargetOffset.y = Mathf.Lerp(0f, -7f, progress);
            }
            // C. ���ö��� �̵� �߰� (0���� 1�� �̵�)
            if (splineDolly != null)
            {
                splineDolly.CameraPosition = progress;
            }
            // D. FOV: 넓힌 상태 → 원래대로 천천히 좁혀옴 (앞으로 밀려오는 느낌)
            if (zoomOutPullFOV > 0f)
            {
                var lens = introZoomOutCam.Lens;
                lens.FieldOfView = Mathf.Lerp(originalFOV + zoomOutPullFOV, originalFOV, progress);
                introZoomOutCam.Lens = lens;
            }
            yield return null;
        }

        // FOV 복구
        if (zoomOutPullFOV > 0f)
        {
            var lens = introZoomOutCam.Lens;
            lens.FieldOfView = originalFOV;
            introZoomOutCam.Lens = lens;
        }

        //yield return new WaitForSeconds(zoomOutDuration);

        // 5. ���� Idle ����
        //SwitchCamera(idleCam);
    }

    // Spline �̵��� ó���ϴ� ���� �ڷ�ƾ
    IEnumerator MoveAlongSpline(CinemachineCamera cam, float duration)
    {
        SwitchCamera(cam);
        var splineDolly = cam.GetComponent<CinemachineSplineDolly>();
        var settings = cam.GetComponent<CameraMoveSettings>(); // Ŀ��

        if (splineDolly != null)
        {
            splineDolly.CameraPosition = 0f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                float t = elapsed / duration;

                float curveValue = t;

                if (settings != null)
                    curveValue = settings.moveCurve.Evaluate(t);
                //Debug.Log($"CurveValue: {curveValue}");
                splineDolly.CameraPosition = curveValue;

                yield return null;
            }
            splineDolly.CameraPosition = 1f;
        }
    }

    public void SwitchCamera(CinemachineCamera targetCam)
    {
        introPlayerCam.Priority = 10;
        introEnemyCam.Priority = 10;
        introZoomOutCam.Priority = 10;
        enemyWinCam.Priority = 10;
        playerWinCam.Priority = 10;
        idleCam.Priority = 10;

        targetCam.Priority = 20;
    }
    public async Task PlayWinCamera()
    {
        SwitchCamera(playerWinCam);
        await Task.Delay((int)(resultCameraHoldDuration * 1000f));
        SwitchCamera(idleCam);
    }

    public async Task PlayLoseCamera()
    {
        SwitchCamera(enemyWinCam);
        await Task.Delay((int)(resultCameraHoldDuration * 1000f));
        SwitchCamera(idleCam);
    }
    public async Task PlayIdleCamera()
    {
        SwitchCamera(idleCam);
        await Task.Delay((int)(resultCameraHoldDuration * 1000f));
    }

    /// <summary>
    /// Cinemachine을 끄고 MainCamera를 결과 좌표로 직접 이동시킴
    /// </summary>
    private bool _isMovingToResult = false;

    public async Task MoveToResultPosition()
    {
        if (_isMovingToResult) return;
        _isMovingToResult = true;

        try
        {
            if (_mainCamera == null) _mainCamera = Camera.main;

            // Cinemachine Brain ???? (MainCamera ?? ??? ??)
            var brain = _mainCamera.GetComponent<CinemachineBrain>();
            if (brain != null) brain.enabled = false;

            Vector3 startPos = _mainCamera.transform.position;
            Quaternion startRot = _mainCamera.transform.rotation;
            Quaternion targetRot = Quaternion.Euler(resultCamRotation);

            float elapsed = 0f;
            while (elapsed < resultCamMoveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / resultCamMoveDuration);

                _mainCamera.transform.position = Vector3.Lerp(startPos, resultCamPosition, t);
                _mainCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);

                await Task.Yield();
            }

            _mainCamera.transform.position = resultCamPosition;
            _mainCamera.transform.rotation = targetRot;
        }
        finally
        {
            _isMovingToResult = false;
        }
    }

    /// <summary>
    /// 페이드 아웃 → 카메라 전환 → 페이드 인 (부드러운 전환)
    /// </summary>
    public void SwitchCameraWithFade(CinemachineCamera targetCam)
    {
        StartCoroutine(FadeSwitchCoroutine(targetCam));
    }

    IEnumerator FadeSwitchCoroutine(CinemachineCamera targetCam)
    {
        // Fade out (검은색으로)
        yield return StartCoroutine(FadeCoroutine(0f, 1f, fadeDuration));

        // 카메라 전환
        SwitchCamera(targetCam);

        // 1프레임 대기 (Cinemachine 전환 적용)
        yield return null;

        // Fade in (투명으로)
        yield return StartCoroutine(FadeCoroutine(1f, 0f, fadeDuration));
    }

    IEnumerator FadeCoroutine(float from, float to, float duration)
    {
        if (fadeOverlay == null) yield break;

        fadeOverlay.gameObject.SetActive(true);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            float alpha = Mathf.Lerp(from, to, t);

            Color c = fadeOverlay.color;
            c.a = alpha;
            fadeOverlay.color = c;

            yield return null;
        }

        Color final_c = fadeOverlay.color;
        final_c.a = to;
        fadeOverlay.color = final_c;

        // 완전 투명이면 비활성화
        if (to <= 0f)
            fadeOverlay.gameObject.SetActive(false);
    }

    /// <summary>
    /// Cinemachine Brain을 다시 활성화하여 가상 카메라 제어로 복귀
    /// </summary>
    public void RestoreCinemachine()
    {
        if (_mainCamera == null) _mainCamera = Camera.main;
        var brain = _mainCamera.GetComponent<Unity.Cinemachine.CinemachineBrain>();
        if (brain != null) brain.enabled = true;
    }
    // ��Ʈ�� ����� �帧
    public async Task RestartSequence()
    {
        await Task.Delay(100);

        // 3. ��Ʈ�� �ڷ�ƾ ����
        StopAllCoroutines();
        StartCoroutine(PlayFullIntroSequence());
    }

}

