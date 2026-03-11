using UnityEngine;
using Unity.Cinemachine; // �ֽ� ���� ���ӽ����̽�
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
    public float zoomOutDuration = 1.5f;  // �ܾƿ� ���� �ð�

    public CameraShake cameraShake;

    [Header("결과 카메라 이동 위치")]
    public Vector3 resultCamPosition = new Vector3(-5.76f, 2.57f, -0.18f);
    public Vector3 resultCamRotation = new Vector3(15.708f, 90.049f, -0.013f);
    [Range(0.3f, 2f)] public float resultCamMoveDuration = 0.8f;

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

        // ������ ���� ������Ʈ ��������
        var composer = introZoomOutCam.GetComponent<CinemachineRotationComposer>();
        var splineDolly = introZoomOutCam.GetComponent<CinemachineSplineDolly>();
        var settings = introZoomOutCam.GetComponent<CameraMoveSettings>();
        float elapsed = 0f;

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
            yield return null;
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

    public void MoveToResultPosition()
    {
        if (!_isMovingToResult)
            StartCoroutine(MoveToResultCoroutine());
    }

    IEnumerator MoveToResultCoroutine()
    {
        _isMovingToResult = true;

        if (_mainCamera == null) _mainCamera = Camera.main;

        // Cinemachine Brain 비활성화 (MainCamera 직접 제어를 위해)
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

            yield return null;
        }

        _mainCamera.transform.position = resultCamPosition;
        _mainCamera.transform.rotation = targetRot;
        _isMovingToResult = false;
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
