using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.Video;

public class MainMenuIntro : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private GameObject uiCanvas;
    private double showTime = 3.5;
    [SerializeField, Min(1f)] private float menuWaitTimeout = 15f;

    [SerializeField] private GameObject meteorEffectPrefab;
    [SerializeField] private GameObject explosionEffectPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform meteorTarget;
    [SerializeField] private float term = 0.5f;

    private CancellationTokenSource _introCancellation;
    private VideoPlayer _subscribedPlayer;
    private bool _videoFailed;
    private Tween _meteorTween;
    private GameObject _meteor, _explosion;

    private void Start()
    {
        if (uiCanvas != null) uiCanvas.SetActive(false);
        _introCancellation = new CancellationTokenSource();
        if (videoPlayer != null)
        {
            _subscribedPlayer = videoPlayer;
            _subscribedPlayer.errorReceived += OnVideoError;
        }
        CheckVideoTime(_introCancellation.Token).Forget();
    }

    private async UniTask CheckVideoTime(CancellationToken cancellationToken)
    {
        try
        {
            float waited = 0f;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (_videoFailed || videoPlayer == null ||
                    (videoPlayer.source == VideoSource.VideoClip && videoPlayer.clip == null) ||
                    (videoPlayer.source == VideoSource.Url && string.IsNullOrWhiteSpace(videoPlayer.url)))
                {
                    ShowMenu();
                    return;
                }
                if (!GamePause.IsPaused)
                {
                    // The original meteor starts at 2.5 s and travels until 3.5 s.
                    if (videoPlayer.time >= showTime - 1f) break;
                    waited += Time.unscaledDeltaTime;
                    if (waited >= Mathf.Max(1f, menuWaitTimeout))
                    {
                        Debug.LogWarning("[MainMenuIntro] Video did not reach the menu cue; showing the menu.", this);
                        ShowMenu();
                        return;
                    }
                }
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            if (meteorEffectPrefab == null || explosionEffectPrefab == null || spawnPoint == null || meteorTarget == null)
            {
                ShowMenu();
                return;
            }
            Vector3 targetPos = meteorTarget.position;
            _meteor = Instantiate(meteorEffectPrefab, spawnPoint.position, spawnPoint.rotation);
            var moveTween = _meteor.transform.DOMove(targetPos, 1f);
            _meteorTween = moveTween;
            await UniTask.WaitUntil(() => !moveTween.IsActive() || moveTween.IsComplete(),
                cancellationToken: cancellationToken);
            _meteorTween = null;
            cancellationToken.ThrowIfCancellationRequested();
            Destroy(_meteor);
            _meteor = null;

            _explosion = Instantiate(explosionEffectPrefab, targetPos, Quaternion.identity);
            await UniTask.Delay(TimeSpan.FromSeconds(Mathf.Max(0f, term)), cancellationToken: cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            ShowMenu();
            await UniTask.Delay(TimeSpan.FromSeconds(2), cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Leaving the scene cancels every wait without touching the next scene's UI.
        }
        catch (Exception exception)
        {
            if (cancellationToken.IsCancellationRequested) return;
            Debug.LogWarning("[MainMenuIntro] Intro could not finish; showing the menu. " + exception.Message, this);
            ShowMenu();
        }
        finally
        {
            UnsubscribeVideo();
            CleanupEffects();
        }
    }

    private void OnVideoError(VideoPlayer player, string message)
    {
        if (player != videoPlayer || !isActiveAndEnabled) return;
        _videoFailed = true;
        Debug.LogWarning("[MainMenuIntro] Video error: " + message, this);
    }

    private void ShowMenu()
    {
        if (isActiveAndEnabled && uiCanvas != null) uiCanvas.SetActive(true);
    }

    private void UnsubscribeVideo()
    {
        if (_subscribedPlayer != null) _subscribedPlayer.errorReceived -= OnVideoError;
        _subscribedPlayer = null;
    }

    private void CleanupEffects()
    {
        // Clear the handle first so repeated cleanup is harmless.
        var tween = _meteorTween;
        _meteorTween = null;
        tween?.Kill();
        if (_meteor != null) Destroy(_meteor);
        if (_explosion != null) Destroy(_explosion);
        _meteor = _explosion = null;
    }

    private void OnDisable()
    {
        _introCancellation?.Cancel();
        UnsubscribeVideo();
        CleanupEffects();
    }

    private void OnDestroy()
    {
        _introCancellation?.Dispose();
        _introCancellation = null;
    }
}
