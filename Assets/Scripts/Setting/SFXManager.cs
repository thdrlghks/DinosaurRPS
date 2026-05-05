using System.Collections.Generic;
using Core;
using UnityEngine;

public class SFXManager : SingletonMonoBehaviour<SFXManager>
{
    [SerializeField] private List<AudioSource> sfxSources = new();
    [SerializeField] private AudioClip winSound;
    [SerializeField] private AudioClip rpsSound;
    [SerializeField] private AudioClip fightSound;

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
