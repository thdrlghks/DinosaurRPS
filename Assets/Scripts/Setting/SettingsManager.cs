using Core;
using UnityEngine;

public class SettingsManager : SingletonMonoBehaviour<SettingsManager>
{
    public GameSettings settings;

    protected override void OnAwake()
    {
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
}