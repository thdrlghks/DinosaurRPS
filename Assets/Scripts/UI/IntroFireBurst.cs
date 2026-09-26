using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>A short flame and ember burst drawn directly over the VS canvas.</summary>
    public static class IntroFireBurst
    {
        private sealed class Particle
        {
            public RawImage Image;
            public Vector2 Origin, Velocity, Size;
            public float Delay, Lifetime, Rotation, Spin;
            public bool IsEmber;
        }

        public static async UniTask PlayAsync(
            RectTransform parent, Texture flameTexture, Texture emberTexture,
            float duration, CancellationToken token)
        {
            if (parent == null || flameTexture == null || emberTexture == null) return;
            token.ThrowIfCancellationRequested();

            var root = new GameObject("VS Fire Burst", typeof(RectTransform));
            root.layer = parent.gameObject.layer;
            var rect = root.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.SetAsLastSibling();

            try
            {
                Canvas.ForceUpdateCanvases();
                float scale = Mathf.Min(rect.rect.width / 1920f, rect.rect.height / 1080f);
                var glow = CreateImage(rect, emberTexture, "Warm impact glow");
                var particles = new List<Particle>(54);
                // Cosmetic randomness must not advance the hand-selection random stream.
                var random = new System.Random();
                float Range(float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());

                for (int i = 0; i < 54; i++)
                {
                    bool ember = i >= 18;
                    float angle = Range(-Mathf.PI, Mathf.PI);
                    float speed = Range(450f, 1000f);
                    var velocity = ember
                        ? new Vector2(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed * 0.65f)
                        : new Vector2(Range(-210f, 210f), Range(220f, 470f));
                    particles.Add(new Particle
                    {
                        Image = CreateImage(rect, ember ? emberTexture : flameTexture, ember ? "Ember" : "Flame"),
                        Origin = new Vector2(Range(-30f, 30f), Range(-330f, 250f)),
                        Velocity = velocity,
                        Size = ember
                            ? new Vector2(Range(4f, 8f), Range(22f, 45f))
                            : new Vector2(Range(135f, 225f), Range(220f, 340f)),
                        Delay = Range(0f, 0.18f),
                        Lifetime = ember ? Range(0.45f, 0.95f) : Range(0.45f, 0.75f),
                        Rotation = ember ? Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg - 90f : Range(-22f, 22f),
                        Spin = ember ? 0f : Range(-28f, 28f),
                        IsEmber = ember
                    });
                }

                for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
                {
                    token.ThrowIfCancellationRequested();
                    if (root == null) return;
                    float time = elapsed / duration * 1.2f;
                    float flash = Mathf.Clamp01(1f - time / 0.35f);
                    glow.rectTransform.sizeDelta = new Vector2(260f + time * 500f, 1050f) * scale;
                    glow.color = new Color(1f, 0.35f, 0.04f, flash * flash * 0.6f);

                    foreach (var particle in particles)
                    {
                        float age = time - particle.Delay;
                        float life = Mathf.Clamp01(age / particle.Lifetime);
                        if (age < 0f || life >= 1f)
                        {
                            particle.Image.color = Color.clear;
                            continue;
                        }

                        var particleRect = particle.Image.rectTransform;
                        Vector2 acceleration = particle.IsEmber ? new Vector2(0f, -360f) : new Vector2(0f, 180f);
                        particleRect.anchoredPosition = (particle.Origin + particle.Velocity * age
                            + 0.5f * acceleration * age * age) * scale;
                        float size = particle.IsEmber ? Mathf.Lerp(1f, 0.35f, life)
                            : (0.7f + life * 0.8f) * (1f + 0.06f * Mathf.Sin(age * 35f + particle.Rotation));
                        particleRect.sizeDelta = particle.Size * (size * scale);
                        particleRect.localRotation = Quaternion.Euler(0f, 0f, particle.Rotation + particle.Spin * age);

                        var tint = Color.Lerp(new Color(1f, 0.9f, 0.4f), new Color(1f, 0.13f, 0.015f), life);
                        tint.a = Mathf.Clamp01(age / 0.035f) * Mathf.Pow(1f - life, 1.25f)
                            * (particle.IsEmber ? 1f : 0.82f);
                        particle.Image.color = tint;
                    }

                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }
            }
            finally
            {
                if (root != null) Object.Destroy(root);
            }
        }

        private static RawImage CreateImage(RectTransform parent, Texture texture, string name)
        {
            var imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            imageObject.layer = parent.gameObject.layer;
            var image = imageObject.GetComponent<RawImage>();
            image.rectTransform.SetParent(parent, false);
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            image.texture = texture;
            image.color = Color.clear;
            image.raycastTarget = false;
            return image;
        }
    }
}
