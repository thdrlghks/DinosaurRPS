using Core;
using UnityEngine;

public class SettingsManager : SingletonMonoBehaviour<SettingsManager>
{
    public GameSettings settings;

    protected override void OnAwake()
    {
        LoadSettings(); // 게임이 켜질 때 저장된 값을 먼저 불러옵니다.
        ApplySettings();
    }

    public void ApplySettings()
    {
        AudioListener.volume = settings.masterVolume;
        SFXManager sfx = GetComponent<SFXManager>();
        if (sfx != null)
        {
            sfx.UpdateEffectVolume(settings.effectVolume);
        }
        Screen.fullScreen = settings.isFullScreen;
    }

    public void LoadSettings()
    {
        // PlayerPrefs에 저장된 값이 없다면, 기존 settings의 값을 기본값으로 사용합니다.
        settings.masterVolume = PlayerPrefs.GetFloat("MasterVolume", settings.masterVolume);
        settings.effectVolume = PlayerPrefs.GetFloat("EffectVolume", settings.effectVolume);

        // bool 형식은 직접 저장이 안 되므로 int(0, 1)로 변환해서 처리합니다.
        int defaultFullScreen = settings.isFullScreen ? 1 : 0;
        settings.isFullScreen = PlayerPrefs.GetInt("IsFullScreen", defaultFullScreen) == 1;
    }

    public void SaveSettings()
    {
        PlayerPrefs.SetFloat("MasterVolume", settings.masterVolume);
        PlayerPrefs.SetFloat("EffectVolume", settings.effectVolume);
        PlayerPrefs.SetInt("IsFullScreen", settings.isFullScreen ? 1 : 0);
        PlayerPrefs.Save(); // 디스크에 확실히 씁니다.
    }
}