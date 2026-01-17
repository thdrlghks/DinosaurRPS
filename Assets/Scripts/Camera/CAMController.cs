using System;
using System.Linq.Expressions;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class CAMController : MonoBehaviour
{
    SwitchCAM Switchcam;

    // 0 = mainCam, 1 = player, 2 = enemy, 3 = spin
    public bool playWin = false;
    public bool playLose = false;
    public bool playIntro = false;
    public bool returnMainCam = false;

    public bool isCameraReady = true;

    private bool isRunning = false;
    [SerializeField] private IntroCAM introCam;
    [SerializeField] private float cameraTransitionTime = 3f;

    void Start()
    {
        Switchcam = GetComponent<SwitchCAM>();
    }

    public void Update()
    {
        if (playWin && !isRunning)
        {
            playWin = false;
            PlayWinCamera().Forget();
        }

        if (playLose && !isRunning)
        {
            playLose = false;
            PlayLoseCamera().Forget();
        }

        if (playIntro)
        {
            playIntro = false;
            Switchcam.SwitchCam(0);
            introCam.playIntro = true;
        }

        if (returnMainCam)
        {
            Switchcam.SwitchCam(0);
            returnMainCam = false;
            isCameraReady = true;
        }
    }

    public async UniTask PlayWinCamera()
    {
        isRunning = true;
        isCameraReady = false; // 카메라 전환 시작
        Switchcam.SwitchCam(1);

        await UniTask.Delay((int)(cameraTransitionTime * 2000));
        Switchcam.SwitchCam(0);

        // 카메라 전환 완료
        isRunning = false;
        isCameraReady = true;
    }

    public async UniTask PlayLoseCamera()
    {
        isRunning = true;
        isCameraReady = false; // 카메라 전환 시작
        Switchcam.SwitchCam(2);

        await UniTask.Delay((int)(cameraTransitionTime * 1000));
        Switchcam.SwitchCam(0);

        isRunning = false;
        isCameraReady = true;
    }
}