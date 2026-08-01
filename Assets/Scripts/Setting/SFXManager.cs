using System.Collections;
using System.Collections.Generic;
using Core;
using Core.Enums;
using UnityEngine;

public class SFXManager : SingletonMonoBehaviour<SFXManager>
{
    [System.Serializable]
    public class SfxEntry
    {
        public SfxId id;

        [Tooltip("여러 개를 넣으면 재생할 때마다 무작위로 하나를 고릅니다. (같은 소리 반복 방지)")]
        public AudioClip[] clips;

        [Range(0f, 1f)] public float volume = 1f;

        [Tooltip("재생마다 이 범위에서 피치를 무작위로 뽑습니다. x와 y가 같으면 고정.")]
        public Vector2 pitchRange = Vector2.one;
    }

    /// <summary>재생 중인 소스 하나. scale은 효과음 볼륨 슬라이더와 곱해질 개별 배율.</summary>
    private sealed class Voice
    {
        public AudioSource Source;
        public float Scale = 1f;

        /// <summary>지금 이 소스가 물고 있는 효과음. Stop(id)로 되찾기 위해 기억해 둔다.</summary>
        public SfxId Id = SfxId.None;

        /// <summary>페이드아웃 중 — 중복 정지와 볼륨 갱신에서 제외된다.</summary>
        public bool Fading;
    }

    [Header("SFX Library")]
    [SerializeField] private List<SfxEntry> _entries = new();

    [Header("Pool")]
    [Tooltip("시작할 때 미리 만들어 둘 AudioSource 개수. 모자라면 런타임에 자동으로 늘어납니다.")]
    [SerializeField, Min(1)] private int _initialSourceCount = 8;

    [Header("BGM")]
    [SerializeField] private AudioSource _bgmSource;
    [SerializeField, Range(0f, 1f)] private float _bgmVolume = 0.6f;
    [SerializeField, Min(0f)] private float _bgmFadeDuration = 1f;

    private readonly List<Voice> _voices = new();
    private Dictionary<SfxId, SfxEntry> _lookup;
    private Coroutine _bgmFade;

    private float EffectVolume =>
        SettingsManager.Instance != null && SettingsManager.Instance.settings != null
            ? SettingsManager.Instance.settings.effectVolume
            : 1f;

    protected override void OnAwake()
    {
        BuildLookup();

        for (int i = 0; i < _initialSourceCount; i++)
        {
            CreateVoice();
        }
    }

    private void BuildLookup()
    {
        _lookup = new Dictionary<SfxId, SfxEntry>(_entries.Count);
        foreach (var entry in _entries)
        {
            if (entry == null || entry.id == SfxId.None) continue;

            if (_lookup.ContainsKey(entry.id))
            {
                Debug.LogWarning($"[SFXManager] SfxId '{entry.id}'가 중복 등록되어 있습니다. 첫 번째 항목만 사용합니다.", this);
                continue;
            }

            _lookup.Add(entry.id, entry);
        }
    }

    #region Play

    /// <summary>라이브러리에 등록된 효과음을 ID로 1회 재생.</summary>
    public void Play(SfxId id, float volumeScale = 1f) => PlayEntry(id, volumeScale, false);

    /// <summary>Stop(id)로 끊을 때까지 반복 재생. 클립이 연출보다 짧을 때 사용.</summary>
    public void PlayLoop(SfxId id, float volumeScale = 1f) => PlayEntry(id, volumeScale, true);

    private void PlayEntry(SfxId id, float volumeScale, bool loop)
    {
        if (id == SfxId.None) return;
        if (_lookup == null) BuildLookup();

        if (!_lookup.TryGetValue(id, out var entry) || entry.clips == null || entry.clips.Length == 0)
        {
            // 아직 클립을 안 꽂은 항목은 조용히 무시 — 작업 중에 로그가 쏟아지지 않게.
            return;
        }

        var voice = GetFreeVoice();
        voice.Id = id;
        voice.Scale = Mathf.Clamp01(entry.volume * volumeScale);
        voice.Source.clip = entry.clips[Random.Range(0, entry.clips.Length)];
        voice.Source.loop = loop;
        voice.Source.pitch = RandomPitch(entry.pitchRange);
        voice.Source.volume = EffectVolume * voice.Scale;
        voice.Source.Play();
    }

    /// <summary>클립을 직접 1회 재생. 라이브러리에 없는 일회성 소리에 사용.</summary>
    public void PlayClip(AudioClip clip, float volumeScale = 1f, float pitch = 1f)
    {
        if (clip == null) return;

        var voice = GetFreeVoice();
        voice.Id = SfxId.None;
        voice.Scale = Mathf.Clamp01(volumeScale);
        voice.Source.clip = clip;
        voice.Source.loop = false;
        voice.Source.pitch = pitch;
        voice.Source.volume = EffectVolume * voice.Scale;
        voice.Source.Play();
    }

    /// <summary>
    /// 해당 ID로 재생 중인 소리를 멈춘다. 뚝 끊기면 귀에 거슬리므로 기본적으로 짧게 페이드아웃한다.
    /// </summary>
    public void Stop(SfxId id, float fadeDuration = 0.15f)
    {
        if (id == SfxId.None) return;

        foreach (var voice in _voices)
        {
            if (voice.Id != id || voice.Fading) continue;
            if (voice.Source == null || !voice.Source.isPlaying) continue;

            if (fadeDuration <= 0f)
            {
                ReleaseVoice(voice);
                continue;
            }

            voice.Fading = true;
            StartCoroutine(FadeOutVoice(voice, fadeDuration));
        }
    }

    // HitStop이 timeScale을 0으로 만들어도 페이드가 멈추지 않도록 unscaled 시간을 쓴다.
    private IEnumerator FadeOutVoice(Voice voice, float duration)
    {
        float startVolume = voice.Source.volume;
        float elapsed = 0f;

        while (elapsed < duration && voice.Source != null && voice.Source.isPlaying)
        {
            elapsed += Time.unscaledDeltaTime;
            voice.Source.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        ReleaseVoice(voice);
    }

    private void ReleaseVoice(Voice voice)
    {
        voice.Fading = false;
        voice.Id = SfxId.None;

        if (voice.Source == null) return;

        voice.Source.Stop();
        voice.Source.loop = false;
        voice.Source.volume = EffectVolume * voice.Scale;
    }

    private static float RandomPitch(Vector2 range)
    {
        float min = Mathf.Min(range.x, range.y);
        float max = Mathf.Max(range.x, range.y);
        if (min <= 0f && max <= 0f) return 1f;
        return Mathf.Approximately(min, max) ? min : Random.Range(min, max);
    }

    private Voice GetFreeVoice()
    {
        foreach (var voice in _voices)
        {
            if (voice.Source != null && !voice.Source.isPlaying) return voice;
        }

        return CreateVoice();
    }

    private Voice CreateVoice()
    {
        var source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f; // 2D
        source.volume = EffectVolume;

        var voice = new Voice { Source = source, Scale = 1f };
        _voices.Add(voice);
        return voice;
    }

    #endregion

    #region BGM

    /// <summary>BGM 교체. 이미 같은 곡이 재생 중이면 아무것도 하지 않는다.</summary>
    public void PlayBgm(AudioClip clip, bool loop = true)
    {
        if (clip == null) return;

        EnsureBgmSource();
        if (_bgmSource.clip == clip && _bgmSource.isPlaying) return;

        if (_bgmFade != null) StopCoroutine(_bgmFade);
        _bgmFade = StartCoroutine(CrossfadeBgm(clip, loop));
    }

    public void StopBgm()
    {
        if (_bgmSource == null) return;

        if (_bgmFade != null) StopCoroutine(_bgmFade);
        _bgmFade = StartCoroutine(FadeOutBgm());
    }

    private void EnsureBgmSource()
    {
        if (_bgmSource != null) return;

        _bgmSource = gameObject.AddComponent<AudioSource>();
        _bgmSource.playOnAwake = false;
        _bgmSource.loop = true;
        _bgmSource.spatialBlend = 0f;
        _bgmSource.volume = _bgmVolume;
    }

    // HitStop이 Time.timeScale을 0으로 만들기 때문에 페이드는 반드시 unscaled 시간으로 돈다.
    private IEnumerator CrossfadeBgm(AudioClip next, bool loop)
    {
        if (_bgmSource.isPlaying)
        {
            yield return FadeBgmVolume(_bgmSource.volume, 0f);
            _bgmSource.Stop();
        }

        _bgmSource.clip = next;
        _bgmSource.loop = loop;
        _bgmSource.volume = 0f;
        _bgmSource.Play();

        yield return FadeBgmVolume(0f, _bgmVolume);
        _bgmFade = null;
    }

    private IEnumerator FadeOutBgm()
    {
        yield return FadeBgmVolume(_bgmSource.volume, 0f);
        _bgmSource.Stop();
        _bgmFade = null;
    }

    private IEnumerator FadeBgmVolume(float from, float to)
    {
        if (_bgmFadeDuration <= 0f)
        {
            _bgmSource.volume = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < _bgmFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _bgmSource.volume = Mathf.Lerp(from, to, elapsed / _bgmFadeDuration);
            yield return null;
        }

        _bgmSource.volume = to;
    }

    #endregion

    /// <summary>설정 UI의 효과음 슬라이더가 호출. 개별 배율은 유지한 채 전체 볼륨만 갱신.</summary>
    public void UpdateEffectVolume(float newVolume)
    {
        foreach (var voice in _voices)
        {
            // 페이드아웃 중인 소스를 건드리면 다시 크게 울린다.
            if (voice.Fading || voice.Source == null) continue;
            voice.Source.volume = newVolume * voice.Scale;
        }
    }
}
