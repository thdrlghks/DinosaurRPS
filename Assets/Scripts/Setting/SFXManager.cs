using UnityEngine;
using System.Collections.Generic;

public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance { get; private set; }

    [SerializeField] private List<AudioSource> sfxSources = new(); // ���� ȿ������ AudioSource
    [SerializeField] private AudioClip winSound;
    [SerializeField] private AudioClip rpsSound;

    [SerializeField] private AudioClip fightSound;
    // ... ���� �߰�

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void PlayWinSound()
    {
        PlaySFX(winSound);
    }

    public void PlayRpsSound()
    {
        PlaySFX(rpsSound);
    }

    public void PlayFightSound()
    {
        PlaySFX(fightSound);
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;

        // ��� ������ AudioSource ã��
        AudioSource source = sfxSources.Find(s => !s.isPlaying);
        if (source == null)
        {
            source = gameObject.AddComponent<AudioSource>();
            sfxSources.Add(source);
        }

        source.clip = clip;
        source.volume = SettingsManager.Instance.settings.effectVolume;
        source.Play();
    }

    public void UpdateEffectVolume(float newVolume)
    {
        foreach (var s in sfxSources)
        {
            s.volume = newVolume;
        }
    }
}
