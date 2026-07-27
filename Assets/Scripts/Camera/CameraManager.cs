using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;
using System.Collections;
using System.Threading.Tasks;


public class CameraManager : MonoBehaviour
{
    [System.Serializable]
    public class AnimationCameraBinding
    {
        public string triggerName;
        public CinemachineCamera camera;
        public bool moveAlongSpline = true;
        [Min(0.05f)] public float moveDuration = 1.2f;
    }

    bool isStartBattle = true;
    public CinemachineCamera introPlayerCam;
    public CinemachineCamera introEnemyCam;
    public CinemachineCamera introZoomOutCam;
    public CinemachineCamera enemyWinCam;
    public CinemachineCamera playerWinCam;
    public AnimationCameraBinding[] playerWinAnimationCameras;
    public AnimationCameraBinding[] enemyWinAnimationCameras;
    public CinemachineCamera idleCam;
    [Range(1.2f, 1.8f)] public float resultCameraHoldDuration = 1.5f;
    [Min(0.05f)] public float defaultWinCameraMoveDuration = 1.2f;

    [Header("�ð� ����")]
    public float waitBeforeStart = 3.1f;   // ���� ��� �ð�
    public float introDuration = 2.0f;    // �� ��Ʈ�� �̵� �ð�
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

    private float waitTime = 0.8f;

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
        yield return new WaitForSeconds(waitTime);

        // 3. �� ī�޶� ��Ʈ��
        cameraShake.StartDinoSteps(introEnemyCam);
        yield return StartCoroutine(MoveAlongSpline(introEnemyCam, introDuration));
        yield return new WaitForSeconds(waitTime);

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
        if (targetCam == null)
        {
            return;
        }

        SetCameraPriority(introPlayerCam, 10);
        SetCameraPriority(introEnemyCam, 10);
        SetCameraPriority(introZoomOutCam, 10);
        SetCameraPriority(enemyWinCam, 10);
        SetCameraPriority(playerWinCam, 10);
        SetCameraPriority(idleCam, 10);
        SetBindingPriorities(playerWinAnimationCameras, 10);
        SetBindingPriorities(enemyWinAnimationCameras, 10);

        SetCameraPriority(targetCam, 20);
    }

    public void SwitchWinCamera(bool playerWon, string triggerName)
    {
        var bindings = playerWon ? playerWinAnimationCameras : enemyWinAnimationCameras;
        var fallbackCam = playerWon ? playerWinCam : enemyWinCam;
        var selectedCam = FindCameraForTrigger(bindings, triggerName, fallbackCam);
        Debug.Log($"[SwitchWinCamera] playerWon={playerWon}, trigger='{triggerName}', " +
                  $"selected={(selectedCam != null ? selectedCam.name : "null")}, " +
                  $"isFallback={selectedCam == fallbackCam}");
        SwitchCamera(selectedCam);
    }

    private static CinemachineCamera FindCameraForTrigger(
        AnimationCameraBinding[] bindings,
        string triggerName,
        CinemachineCamera fallbackCam)
    {
        if (bindings != null)
        {
            for (int i = 0; i < bindings.Length; i++)
            {
                var binding = bindings[i];
                if (binding != null &&
                    binding.camera != null &&
                    binding.triggerName == triggerName)
                {
                    return binding.camera;
                }
            }
        }

        return fallbackCam;
    }

    private static void SetBindingPriorities(AnimationCameraBinding[] bindings, int priority)
    {
        if (bindings == null)
        {
            return;
        }

        for (int i = 0; i < bindings.Length; i++)
        {
            if (bindings[i] != null)
            {
                SetCameraPriority(bindings[i].camera, priority);
            }
        }
    }

    private static void SetCameraPriority(CinemachineCamera camera, int priority)
    {
        if (camera != null)
        {
            camera.Priority = priority;
        }
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

        StopAllCoroutines();
        StartCoroutine(PlayFullIntroSequence());
    }

}

