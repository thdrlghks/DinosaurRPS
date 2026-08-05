using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class SettingUI : MonoBehaviour
{
    public Slider masterVolumeSlider;
    public Slider effectVolumeSlider;
    public Toggle fullScreenToggle;

    public VideoPlayer videoPlayer;
    public AudioSource videoAudioSource;

    private float backupMasterVolume;
    private float backupEffectVolume;
    private bool backupIsFullScreen;

    private void OnEnable()
    {
        var s = SettingsManager.Instance.settings;

        // 1. 창이 켜질 때 현재의 설정값을 백업 변수에 저장해 둡니다.
        backupMasterVolume = s.masterVolume;
        backupEffectVolume = s.effectVolume;
        backupIsFullScreen = s.isFullScreen;

        // 2. UI 슬라이더와 토글의 위치를 현재 설정값에 맞춥니다.
        masterVolumeSlider.value = s.masterVolume;
        effectVolumeSlider.value = s.effectVolume;
        fullScreenToggle.isOn = s.isFullScreen;

        // 3. 세팅창이 켜질 때 게임 내 모든 사운드를 일시정지합니다.
        AudioListener.pause = true;

        // 만약 VideoPlayer의 소리도 같이 멈추고 싶다면 아래 추가
        if (videoAudioSource != null && videoAudioSource.isPlaying)
            videoAudioSource.Pause();
    }

    private void Start()
    {
        var s = SettingsManager.Instance.settings;

        masterVolumeSlider.value = s.masterVolume;
        effectVolumeSlider.value = s.effectVolume;
        fullScreenToggle.isOn = s.isFullScreen;

        masterVolumeSlider.onValueChanged.AddListener(val =>
        {
            s.masterVolume = val;
            AudioListener.volume = val;


            if (videoAudioSource != null)
                videoAudioSource.volume = val;
        });

        effectVolumeSlider.onValueChanged.AddListener(val =>
        {
            s.effectVolume = val;

            if (SFXManager.Instance != null)
                SFXManager.Instance.UpdateEffectVolume(val);

        });

        fullScreenToggle.onValueChanged.AddListener(val =>
        {
            s.isFullScreen = val;
            Screen.fullScreen = val;
        });
    }

    public void OnClickConfirm()
    {
        // 확인을 누를 때 설정을 저장합니다.
        SettingsManager.Instance.SaveSettings();

        // 사운드 일시정지를 해제합니다.
        AudioListener.pause = false;
        if (videoAudioSource != null)
            videoAudioSource.UnPause();

        gameObject.SetActive(false); // 창 닫기
        Time.timeScale = 1f;         // 일시정지 해제
    }

    public void OnClickCancel()
    {
        var s = SettingsManager.Instance.settings;

        // 1. GameSettings 데이터를 백업해둔 원래 값으로 되돌립니다.
        s.masterVolume = backupMasterVolume;
        s.effectVolume = backupEffectVolume;
        s.isFullScreen = backupIsFullScreen;

        // 2. 실제 게임 시스템(오디오, 화면)에도 백업된 값을 다시 적용시킵니다.
        AudioListener.volume = backupMasterVolume;
        if (videoAudioSource != null) videoAudioSource.volume = backupMasterVolume;

        if (SFXManager.Instance != null) SFXManager.Instance.UpdateEffectVolume(backupEffectVolume);

        Screen.fullScreen = backupIsFullScreen;

        // 3. 취소할 때도 사운드 일시정지를 해제합니다.
        AudioListener.pause = false;
        if (videoAudioSource != null)
            videoAudioSource.UnPause();

        // 4. 창을 닫고 일시정지를 해제합니다.
        gameObject.SetActive(false);
        Time.timeScale = 1f;
    }

    public void OnClickExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}