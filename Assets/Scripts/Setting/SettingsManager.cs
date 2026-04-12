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
        // + soundEffect volume
        Screen.fullScreen = settings.isFullScreen;
    }
}