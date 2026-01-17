using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Utils
{
    public class UITweenUtil
    {
        public static async UniTask ScaleUpAndFadeOutAsync(
            Transform target,
            Vector3 fromScale,
            Vector3 toScale,
            float scaleTime,
            float fadeTime,
            CancellationToken ct = default)
        {
            var image = target.GetComponent<Image>();

            if (image == null) return;
            
            var originalScale = target.localScale;
            
            target.gameObject.SetActive(true);
            target.localScale = fromScale;
            image.color = new Color(image.color.r, image.color.g, image.color.b, 1f);

            await target.DOScale(toScale, scaleTime).AsyncWaitForCompletion();
            await image.DOFade(0f, fadeTime).AsyncWaitForCompletion();

            target.gameObject.SetActive(false);

            target.localScale = originalScale;
            
            image.color = new Color(image.color.r, image.color.g, image.color.b, 1f);
        }
    }
}