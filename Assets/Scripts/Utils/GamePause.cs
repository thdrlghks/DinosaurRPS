using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Keeps the settings pause and short hit stops from overwriting one another.</summary>
public static class GamePause
{
    private static readonly HashSet<UnityEngine.Object> Owners = new();
    private static float _unpausedTimeScale = 1f;
    private static float? _hitStopScale;
    private static bool _previousAudioPause;
    private static int _resumeFrame = -2;

    public static bool IsPaused => Owners.Count > 0;
    // The click/key that closes settings must not also advance the screen underneath.
    public static bool BlocksGameplayInput => IsPaused || Time.frameCount <= _resumeFrame + 1;
    public static event Action<bool> Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        Owners.Clear();
        _hitStopScale = null;
        _unpausedTimeScale = 1f;
        _resumeFrame = -2;
        _previousAudioPause = false;
        Changed = null;
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    public static void SetPaused(UnityEngine.Object owner, bool paused)
    {
        bool wasPaused = IsPaused;
        if (paused)
        {
            CaptureTimeScale();
            if (!Owners.Add(owner)) return;
            if (!wasPaused)
            {
                _previousAudioPause = AudioListener.pause;
                AudioListener.pause = true;
            }
        }
        else if (!Owners.Remove(owner)) return;

        ApplyTimeScale();
        if (wasPaused == IsPaused) return;
        if (!IsPaused)
        {
            AudioListener.pause = _previousAudioPause;
            _resumeFrame = Time.frameCount;
        }
        Changed?.Invoke(IsPaused);
    }

    public static void SetHitStop(float? timeScale)
    {
        CaptureTimeScale();
        _hitStopScale = timeScale.HasValue ? Mathf.Max(0f, timeScale.Value) : null;
        ApplyTimeScale();
    }

    private static void CaptureTimeScale()
    {
        if (!IsPaused && !_hitStopScale.HasValue)
            _unpausedTimeScale = Time.timeScale;
    }

    private static void ApplyTimeScale() =>
        Time.timeScale = IsPaused ? 0f : _hitStopScale ?? _unpausedTimeScale;
}
