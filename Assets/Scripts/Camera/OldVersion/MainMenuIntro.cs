using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.Video;

public class MainMenuIntro : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private GameObject uiCanvas;
    private double showTime = 3.5;

    [SerializeField] private GameObject meteorEffectPrefab;
    [SerializeField] private GameObject explosionEffectPrefab;
    [SerializeField] private Transform spawnPoint;
    private void Start()
    {
        if (uiCanvas != null) uiCanvas.SetActive(false);
        CheckVideoTime().Forget();
    }

    private async UniTaskVoid CheckVideoTime() 
    {
       
        // 비디오 시간 기준으로 기다리기
        await UniTask.WaitUntil(() => videoPlayer.time >= showTime -1f);


        var meteor = Instantiate(meteorEffectPrefab, spawnPoint.position, spawnPoint.rotation);

        // 목표 지점 계산
        Vector3 targetPos = Camera.main.transform.position
                          + Camera.main.transform.forward * 2f
                          - Camera.main.transform.right * 1f
                          - Camera.main.transform.up * 1f;

        // 메테오 이동
        await meteor.transform.DOMove(targetPos, 1f).AsyncWaitForCompletion();

        // 메테오 제거
        Destroy(meteor);

        // 폭발 이펙트 생성
        var explosion = Instantiate(explosionEffectPrefab, targetPos, Quaternion.identity);

        // UI 등장
        uiCanvas.SetActive(true);

        // 2초 뒤 폭발 이펙트 삭제
        await UniTask.Delay(TimeSpan.FromSeconds(2));
        Destroy(explosion);
    }
}
