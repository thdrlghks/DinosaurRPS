using System;
using UnityEngine;


public class UIMove : MonoBehaviour
{
    [SerializeField] private GameObject _title;
    [SerializeField] private float _speed = 1f;
    [SerializeField] private float _distance = 20f;
    
    private Vector3 _startPos;

    private void Start()
    {
        _startPos = transform.localPosition;
    }

    private void Update()
    {
        var newY = _startPos.y + Mathf.Sin(Time.time * _speed) * _distance;
        transform.localPosition = new Vector3(_startPos.x, newY, _startPos.z);
    }
}

