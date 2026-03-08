using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    public float basePower = 3f;
    public float shakeDuration = 0.35f;
    public float stepInterval = 0.5f;

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
}