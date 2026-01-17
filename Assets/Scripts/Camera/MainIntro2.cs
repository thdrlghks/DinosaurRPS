using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.Video;
public class MainIntro2 : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private GameObject uiCanvas;
    private double showTime = 3.5;

    private void Start()
    {
        if (uiCanvas != null) uiCanvas.SetActive(false);
        CheckVideoTime().Forget();
    }

    private async UniTaskVoid CheckVideoTime()
    {

        await UniTask.WaitUntil(() => videoPlayer.time >= showTime);

        uiCanvas.SetActive(true);
    }
}
