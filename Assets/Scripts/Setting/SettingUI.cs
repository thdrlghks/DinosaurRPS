using UnityEngine;
using UnityEngine.EventSystems;
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
    private Canvas _canvas;
    private int _previousSortOrder;
    private RenderMode _previousRenderMode;
    private GameObject _inputBlocker;
    private bool _resumeVideo;
    private bool _pauseActive;

    private void OnEnable()
    {
        _pauseActive = true;
        BringToFront();
        GamePause.SetPaused(this, true);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

        var s = SettingsManager.Instance.settings;
        backupMasterVolume = s.masterVolume;
        backupEffectVolume = s.effectVolume;
        backupIsFullScreen = s.isFullScreen;
        masterVolumeSlider.SetValueWithoutNotify(s.masterVolume);
        effectVolumeSlider.SetValueWithoutNotify(s.effectVolume);
        fullScreenToggle.SetIsOnWithoutNotify(s.isFullScreen);

        _resumeVideo = videoPlayer != null && videoPlayer.isPlaying;
        if (_resumeVideo) videoPlayer.Pause();
    }

    private void BringToFront()
    {
        _canvas = GetComponentInParent<Canvas>().rootCanvas;
        _previousSortOrder = _canvas.sortingOrder;
        _previousRenderMode = _canvas.renderMode;
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 300;

        if (_inputBlocker == null)
        {
            _inputBlocker = new GameObject("Settings Input Blocker", typeof(RectTransform), typeof(Image));
            _inputBlocker.layer = gameObject.layer;
            var rect = (RectTransform)_inputBlocker.transform;
            rect.SetParent(_canvas.transform, false);
            rect.SetAsFirstSibling();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = _inputBlocker.GetComponent<Image>();
            image.color = new Color(0, 0, 0, 0.45f);
            image.raycastTarget = true;
        }
        _inputBlocker.SetActive(true);
    }

    private void OnDisable()
    {
        if (!_pauseActive) return;
        _pauseActive = false;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        if (_inputBlocker != null) _inputBlocker.SetActive(false);
        if (_canvas != null)
        {
            _canvas.sortingOrder = _previousSortOrder;
            _canvas.renderMode = _previousRenderMode;
        }
        GamePause.SetPaused(this, false);
        if (_resumeVideo && videoPlayer != null) videoPlayer.Play();
        _resumeVideo = false;
    }

    private void OnDestroy()
    {
        if (_inputBlocker != null) Destroy(_inputBlocker);
    }

    private void Start()
    {
        var s = SettingsManager.Instance.settings;
        masterVolumeSlider.onValueChanged.AddListener(val =>
        {
            s.masterVolume = val;
            AudioListener.volume = val;
            if (videoAudioSource != null) videoAudioSource.volume = val;
        });
        effectVolumeSlider.onValueChanged.AddListener(val =>
        {
            s.effectVolume = val;
            if (SFXManager.Instance != null) SFXManager.Instance.UpdateEffectVolume(val);
        });
        fullScreenToggle.onValueChanged.AddListener(val =>
        {
            s.isFullScreen = val;
            Screen.fullScreen = val;
        });
    }

    public void OnClickConfirm()
    {
        SettingsManager.Instance.SaveSettings();
        gameObject.SetActive(false);
    }

    public void OnClickCancel()
    {
        var s = SettingsManager.Instance.settings;
        s.masterVolume = backupMasterVolume;
        s.effectVolume = backupEffectVolume;
        s.isFullScreen = backupIsFullScreen;
        AudioListener.volume = backupMasterVolume;
        if (videoAudioSource != null) videoAudioSource.volume = backupMasterVolume;
        if (SFXManager.Instance != null) SFXManager.Instance.UpdateEffectVolume(backupEffectVolume);
        Screen.fullScreen = backupIsFullScreen;
        gameObject.SetActive(false);
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
