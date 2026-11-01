using UnityEngine;

namespace Environment
{
    /// <summary>Quiet, local-space motion for background objects in the human final.</summary>
    [DisallowMultipleComponent]
    public sealed class SpaceArenaMotion : MonoBehaviour
    {
        [SerializeField] private Vector3 _rotationDegreesPerSecond;
        [SerializeField, Min(0f)] private float _floatAmplitude;
        [SerializeField, Min(0.01f)] private float _floatPeriod = 12f;
        [SerializeField] private float _phase;

        private Vector3 _restPosition;
        private Quaternion _restRotation;
        private float _elapsed;

        public void Configure(Vector3 rotationDegreesPerSecond, float floatAmplitude = 0f,
            float floatPeriod = 12f, float phase = 0f)
        {
            _rotationDegreesPerSecond = rotationDegreesPerSecond;
            _floatAmplitude = floatAmplitude;
            _floatPeriod = Mathf.Max(0.01f, floatPeriod);
            _phase = phase;
        }

        private void OnEnable()
        {
            _restPosition = transform.localPosition;
            _restRotation = transform.localRotation;
            _elapsed = 0f;
        }

        private void Update()
        {
            // Scaled time keeps the scenery still while the game is paused.
            _elapsed += Time.deltaTime;
            transform.localRotation = _restRotation * Quaternion.Euler(_rotationDegreesPerSecond * _elapsed);
            float bob = Mathf.Sin(_phase + _elapsed * (Mathf.PI * 2f / _floatPeriod)) - Mathf.Sin(_phase);
            transform.localPosition = _restPosition + Vector3.up * (_floatAmplitude * bob);
        }

        private void OnDisable()
        {
            transform.localPosition = _restPosition;
            transform.localRotation = _restRotation;
        }
    }
}
