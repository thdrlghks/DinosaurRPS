using UnityEngine;
using Unity.Cinemachine; // 최신 버전 네임스페이스
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

    [Header("시간 설정")]
    public float waitBeforeStart = 3.1f;   // 최초 대기 시간
    public float introDuration = 3.0f;    // 각 인트로 이동 시간
    public float zoomOutDuration = 1.5f;  // 줌아웃 연출 시간

    public CameraShake cameraShake;

    void Start()
    {
        isStartBattle = true;
        StartCoroutine(PlayFullIntroSequence());
    }

    IEnumerator PlayFullIntroSequence()
    {
        // 1. 최초 3초 대기
        if (isStartBattle)
        {
            yield return new WaitForSeconds(waitBeforeStart);
            isStartBattle=false;
        }

        // 2. 플레이어 카메라 인트로
        cameraShake.StartDinoSteps(introPlayerCam);
        yield return StartCoroutine(MoveAlongSpline (introPlayerCam, introDuration));

        // 3. 적 카메라 인트로
        cameraShake.StartDinoSteps(introEnemyCam);
        yield return StartCoroutine(MoveAlongSpline(introEnemyCam, introDuration));

        // 4. 중앙 줌아웃 연출 + 시선 낮추기
        SwitchCamera(introZoomOutCam);

        // 연출을 위한 컴포넌트 가져오기
        var composer = introZoomOutCam.GetComponent<CinemachineRotationComposer>();
        var splineDolly = introZoomOutCam.GetComponent<CinemachineSplineDolly>();
        var settings = introZoomOutCam.GetComponent<CameraMoveSettings>();
        float elapsed = 0f;

        while (elapsed < zoomOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / zoomOutDuration;
            float progress = settings?.moveCurve.Evaluate(t) ?? t;

            // B. 시선 낮추기 (0에서 -1.5f로 점점 낮아짐)
            if (composer != null)
            {
                composer.TargetOffset.y = Mathf.Lerp(0f, -7f, progress);
            }
            // C. 스플라인 이동 추가 (0에서 1로 이동)
            if (splineDolly != null)
            {
                splineDolly.CameraPosition = progress;
            }
            yield return null;
        }

        //yield return new WaitForSeconds(zoomOutDuration);

        // 5. 최종 Idle 유지
        //SwitchCamera(idleCam);
    }

    // Spline 이동을 처리하는 공용 코루틴
    IEnumerator MoveAlongSpline(CinemachineCamera cam, float duration)
    {
        SwitchCamera(cam);
        var splineDolly = cam.GetComponent<CinemachineSplineDolly>();
        var settings = cam.GetComponent<CameraMoveSettings>(); // 커브

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
                Debug.Log($"CurveValue: {curveValue}");
                splineDolly.CameraPosition = curveValue;

                yield return null;
            }
            splineDolly.CameraPosition = 1f;
        }
    }

    public void SwitchCamera(CinemachineCamera targetCam)
    {
        // 모든 카메라 우선순위 초기화
        introPlayerCam.Priority = 10;
        introEnemyCam.Priority = 10;
        introZoomOutCam.Priority = 10;
        enemyWinCam.Priority = 10;
        playerWinCam.Priority = 10;

        // 선택된 카메라만 높임
        targetCam.Priority = 20;
    }
    public async Task PlayWinCamera()
    {
        SwitchCamera(playerWinCam);
        await Task.Delay(6500);
        SwitchCamera(idleCam);
    }

    // 패배 연출
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
    // 인트로 재시작 흐름
    public async Task RestartSequence()
    {
        await Task.Delay(100); 

        // 3. 인트로 코루틴 실행
        StopAllCoroutines();
        StartCoroutine(PlayFullIntroSequence());
    }
}