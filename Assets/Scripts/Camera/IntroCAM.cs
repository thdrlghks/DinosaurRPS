using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;

public class IntroCAM : MonoBehaviour
{
    [System.Serializable]
    public class CameraShot
    {
        public Transform position;
        public Transform lookTarget;
        [Min(0.0001f)] public float duration = 1f; 
        public float delay = 0f;
        public float shakeStrength = 0.3f;
        public Ease easeType = Ease.Linear;
    }
    public List<CameraShot> shots = new List<CameraShot>();
    public Transform mainCamera; 

    public bool playIntro = false;
    public bool introPlaying = false;

    public void Update()
    {
        if (playIntro && !introPlaying)
        {
            playIntro = false;
            introPlaying = true;
            _ = PlayIntroAsync(); 
        }
        else if (playIntro)
        {
            playIntro = false;
        }
    }

    private async UniTask PlayIntroAsync()
    {
        await IntroCinema();
        introPlaying = false;
    }

    public async UniTask IntroCinema()
    {
        foreach (var shot in shots)
        {
            float dur = (shot.duration <= 0f) ? 0.0001f : shot.duration;

            bool doShake = shot.shakeStrength > 0f;

            Quaternion lookRot = Quaternion.LookRotation(shot.lookTarget.position - shot.position.position);

            var moveTween   = transform.DOMove(shot.position.position, dur).SetEase(shot.easeType);
            var rotateTween = transform.DORotateQuaternion(lookRot, dur).SetEase(shot.easeType);

            Tween shakeTween = null;
            if (doShake && mainCamera != null)
            {
                shakeTween = mainCamera.DOShakePosition(
                    dur,
                    shot.shakeStrength,
                    vibrato: Mathf.Max(1, 4),
                    randomness: 1,
                    snapping: false,
                    fadeOut: false
                ).SetRelative();
            }

            moveTween.Play();
            rotateTween.Play();
            shakeTween?.Play();

            await moveTween.AsyncWaitForCompletion();

            if (shot.delay > 0f)
                await UniTask.Delay(System.TimeSpan.FromSeconds(shot.delay));
        }
    }


}