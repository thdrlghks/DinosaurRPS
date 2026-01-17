using DG.Tweening;
using UnityEngine;

public class DeadTestCode : MonoBehaviour
{
    
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private GameObject _enemyPrefab;
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartUppercutEffect();
        }
    }
    
    private void StartUppercutEffect()
    {
        Vector3[] path = new Vector3[]
        {
            new Vector3(-2.34f, 2.02f, -2.34f),    
            new Vector3(-2.51f, 10.33f, -5.41f),  
            new Vector3(-2.5f, 0.99f, -7.92f)       
        };
        
        _enemyPrefab.transform.rotation = Quaternion.Euler(0f, 1.65f, 0f);
        
        _enemyPrefab.transform.DOPath(path, 2f, PathType.CatmullRom)
            .SetEase(Ease.OutQuad);
        
        _enemyPrefab.transform.DORotate(new Vector3(360f * 3f, 1.65f, 0f), 2f, RotateMode.FastBeyond360)
            .SetEase(Ease.OutQuad);
        
        _enemyPrefab.transform.DORotate(new Vector3(20.86f, 181.54f, 179.44f), 0.3f, RotateMode.Fast)
            .SetDelay(1.7f)
            .SetEase(Ease.OutBounce);
    }
    
}
