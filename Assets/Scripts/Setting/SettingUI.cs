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
}