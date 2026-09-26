using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Managers
{
    /// <summary>Shows the clip's first frame until the video has rendered its own frame.</summary>
    [RequireComponent(typeof(VideoPlayer))]
    [DefaultExecutionOrder(-200)]
    public sealed class VideoFirstFrameDisplay : MonoBehaviour
    {
        [SerializeField] private RawImage _screen;
        [SerializeField] private Texture2D _firstFrame;

        private VideoPlayer _player;
        private bool _subscribed;
        private bool _previousFrameEvents;
        private int _readyFrame = -1;

        private void Awake() => _player = GetComponent<VideoPlayer>();

        private void OnEnable()
        {
            if (_screen == null || _firstFrame == null)
            {
                Debug.LogError("Assign the video screen and the clip's first-frame texture.", this);
                return;
            }

            // This texture is also saved on the scene's RawImage, so even its first draw is opaque.
            _screen.texture = _firstFrame;
            _readyFrame = -1;
            _previousFrameEvents = _player.sendFrameReadyEvents;
            _player.frameReady += OnFrameReady;
            _player.sendFrameReadyEvents = true;
            _subscribed = true;
        }

        private void OnFrameReady(VideoPlayer player, long frameIndex)
        {
            if (_readyFrame < 0) _readyFrame = Time.frameCount;
        }

        private void LateUpdate()
        {
            if (_readyFrame < 0 || Time.frameCount <= _readyFrame) return;
            Texture output = _player.targetTexture != null ? _player.targetTexture : _player.texture;
            if (output == null) return;

            // prepareCompleted is too early: wait for frameReady and one render cycle
            // so the RenderTexture has pixels before replacing the still image.
            _screen.texture = output;
            _readyFrame = -1;
            StopListening();
        }

        private void OnDisable() => StopListening();

        private void StopListening()
        {
            if (!_subscribed || _player == null) return;
            _player.frameReady -= OnFrameReady;
            _player.sendFrameReadyEvents = _previousFrameEvents;
            _subscribed = false;
        }
    }
}
