using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    public float basePower = 3f;
    public float shakeDuration = 0.35f;
    public float stepInterval = 0.5f;

    [Header("Impact Shake (승패 결과 시)")]
    [SerializeField] private float _impactPower = 0.3f;
    [SerializeField] private float _impactDuration = 0.25f;
    [SerializeField] private int _impactVibrato = 15;

    public void StartDinoSteps(CinemachineCamera cam)
    {
        StartCoroutine(DinoSteps(cam));
    }

    IEnumerator DinoSteps(CinemachineCamera cam)
    {
        var noise = cam.GetComponent<CinemachineBasicMultiChannelPerlin>();

        float[] stepPowers = { basePower * 0.8f, basePower, basePower * 1.4f };

        for (int i = 0; i < 3; i++)
        {
            yield return new WaitForSeconds(stepInterval);
            yield return StartCoroutine(SingleShake(noise, stepPowers[i]));
        }
    }

    IEnumerator SingleShake(CinemachineBasicMultiChannelPerlin noise, float power)
    {
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / shakeDuration;

            noise.AmplitudeGain = Mathf.Lerp(power, 0f, t);

            yield return null;
        }

        noise.AmplitudeGain = 0;
    }

    /// <summary>
    /// MainCamera를 직접 흔드는 임팩트 쉐이크 (Cinemachine 비활성 상태에서도 동작)
    /// </summary>
    public void PlayImpactShake(Camera cam = null, float powerMultiplier = 1f)
    {
        if (cam == null) cam = Camera.main;
        if (cam != null)
            StartCoroutine(ImpactShakeCoroutine(cam.transform, powerMultiplier));
    }

    IEnumerator ImpactShakeCoroutine(Transform camTransform, float powerMultiplier)
    {
        Vector3 originalPos = camTransform.localPosition;
        float elapsed = 0f;
        float power = _impactPower * powerMultiplier;

        while (elapsed < _impactDuration)
        {
            elapsed += Time.unscaledDeltaTime; // HitStop 중에도 동작
            float t = elapsed / _impactDuration;
            float dampingCurve = 1f - t; // 점점 감소

            // 비대칭 진동: 빠른 사인파 + 랜덤 오프셋
            float shakeX = Mathf.Sin(elapsed * _impactVibrato * Mathf.PI * 2f) * power * dampingCurve;
            float shakeY = Mathf.Cos(elapsed * _impactVibrato * Mathf.PI * 1.7f) * power * dampingCurve * 0.7f;

            camTransform.localPosition = originalPos + new Vector3(shakeX, shakeY, 0f);

            yield return null;
        }

        camTransform.localPosition = originalPos;
    }
}
