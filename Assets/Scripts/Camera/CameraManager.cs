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

    [Header("�ð� ����")]
    public float waitBeforeStart = 3.1f;   // ���� ��� �ð�
    public float introDuration = 3.0f;    // �� ��Ʈ�� �̵� �ð�
    public float zoomOutDuration = 1.5f;  // �ܾƿ� ���� �ð�

    public CameraShake cameraShake;

    void Start()
    {
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
        // ��� ī�޶� �켱���� �ʱ�ȭ
        introPlayerCam.Priority = 10;
        introEnemyCam.Priority = 10;
        introZoomOutCam.Priority = 10;
        enemyWinCam.Priority = 10;
        playerWinCam.Priority = 10;

        // ���õ� ī�޶� ����
        targetCam.Priority = 20;
    }
    public async Task PlayWinCamera()
    {
        SwitchCamera(playerWinCam);
        await Task.Delay(6500);
        SwitchCamera(idleCam);
    }

    // �й� ����
    public async Task PlayLoseCamera()
    {
        SwitchCamera(enemyWinCam);
        await Task.Delay(6500);
        SwitchCamera(idleCam);
    }
    public async Task PlayIdleCamera()
    {
        SwitchCamera(idleCam);
        await Task.Delay(6500);
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