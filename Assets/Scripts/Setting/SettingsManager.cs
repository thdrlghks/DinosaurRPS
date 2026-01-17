using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    public GameSettings settings;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);


        ApplySettings();
    }
    public void ApplySettings()
    {
        AudioListener.volume = settings.masterVolume;
        // + soundEffect volume
        Screen.fullScreen = settings.isFullScreen;
    }
}