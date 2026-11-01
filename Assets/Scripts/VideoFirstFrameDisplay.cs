using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Managers
{
    /// <summary>Keeps an opaque placeholder until this playback attempt renders a frame.</summary>
    [RequireComponent(typeof(VideoPlayer))]
    [DefaultExecutionOrder(-200)]
    public sealed class VideoFirstFrameDisplay : MonoBehaviour
    {
        [SerializeField] private RawImage _screen;
        [SerializeField] private Texture2D _firstFrame;
        [Tooltip("Optional clip identity for this poster. A replacement clip uses black until its own first frame.")]
        [SerializeField] private VideoClip _firstFrameClip;

        private VideoPlayer _player;
        private bool _subscribed;
        private bool _previousFrameEvents;
        private int _readyFrame = -1;
        private int _generation;
        private VideoPlayer.FrameReadyEventHandler _frameHandler;

        public bool IsConfigured => _screen != null && GetPlayer() != null;
        public bool HasRenderedFrame { get; private set; }

        private VideoPlayer GetPlayer()
        {
            if (_player == null) _player = GetComponent<VideoPlayer>();
            return _player;
        }
        private void Awake() => GetPlayer();
        private void OnEnable() => ResetForPlayback();

        public bool ResetForPlayback()
        {
            HoldPlaceholder();
            if (!IsConfigured || !isActiveAndEnabled) return false;
            int generation = _generation;
            _previousFrameEvents = _player.sendFrameReadyEvents;
            _frameHandler = (player, frameIndex) =>
            {
                if (generation != _generation || player != _player || !player.isPrepared ||
                    !player.isPlaying || frameIndex < 0) return;
                if (_readyFrame < 0) _readyFrame = Time.frameCount;
            };
            _player.frameReady += _frameHandler;
            _player.sendFrameReadyEvents = true;
            _subscribed = true;
            return true;
        }

        public void HoldPlaceholder()
        {
            ++_generation;
            StopListening();
            _readyFrame = -1;
            HasRenderedFrame = false;
            var player = GetPlayer();
            if (_screen == null) return;
            bool posterMatches = _firstFrameClip == null ||
                (player != null && player.source == VideoSource.VideoClip && player.clip == _firstFrameClip);
            _screen.texture = posterMatches && _firstFrame != null ? _firstFrame : Texture2D.blackTexture;
        }

        private void LateUpdate()
        {
            if (_readyFrame < 0 || Time.frameCount <= _readyFrame || _player == null || _screen == null) return;
            Texture output = _player.targetTexture != null ? _player.targetTexture : _player.texture;
            if (output == null) return;
            // Wait one render cycle after frameReady; prepareCompleted alone has no pixels yet.
            _screen.texture = output;
            HasRenderedFrame = true;
            _readyFrame = -1;
            StopListening();
        }

        private void OnDisable()
        {
            ++_generation;
            _readyFrame = -1;
            StopListening();
        }
        private void StopListening()
        {
            if (!_subscribed) return;
            if (_player != null)
            {
                _player.frameReady -= _frameHandler;
                _player.sendFrameReadyEvents = _previousFrameEvents;
            }
            _frameHandler = null;
            _subscribed = false;
        }
    }
}
