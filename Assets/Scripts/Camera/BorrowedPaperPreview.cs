using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// An independent chicken Paper feed for Tyranno's magnified hand reveal.
/// The opponent's live camera and the selection HUD are never redirected.
/// </summary>
public sealed class BorrowedPaperPreview : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField] private Camera _camera;

    private static readonly int PaperState = Animator.StringToHash("Base Layer.Paper");
    private RenderTexture _texture;
    private RawImage _target;
    private Texture _originalTexture;
    private Rect _originalUv;

    public Animator Begin(RawImage target)
    {
        End();
        if (target == null || _animator == null || _camera == null) return null;

        if (_texture == null)
        {
            var source = target.texture as RenderTexture;
            _texture = source != null
                ? new RenderTexture(source.descriptor)
                : new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32);
            _texture.name = "Tyranno Borrowed Chicken Paper";
            _texture.wrapMode = TextureWrapMode.Clamp;
            _texture.Create();
        }

        _target = target;
        _originalTexture = target.texture;
        _originalUv = target.uvRect;
        gameObject.SetActive(true);
        _animator.speed = 1f;
        _animator.Play(PaperState, 0, 0f);
        _animator.Update(0f);
        _camera.targetTexture = _texture;
        _camera.enabled = true;
        // Fill the feed before exposing it, including on repeated Paper rounds.
        _camera.Render();
        target.texture = _texture;
        target.uvRect = new Rect(_originalUv.x + _originalUv.width, _originalUv.y,
            -_originalUv.width, _originalUv.height);
        return _animator;
    }

    public void End()
    {
        if (_target != null)
        {
            _target.texture = _originalTexture;
            _target.uvRect = _originalUv;
        }
        _target = null;
        _originalTexture = null;
        if (_camera != null) _camera.enabled = false;
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_target != null)
        {
            _target.texture = _originalTexture;
            _target.uvRect = _originalUv;
        }
        if (_texture == null) return;
        _texture.Release();
        Destroy(_texture);
    }
}
