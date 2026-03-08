using UnityEngine;

public class FollowCAM : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Transform followCam;

    private Vector3 _offset;

    public void Start()
    {
        _offset = followCam.transform.position - target.position;
    }

    void LateUpdate()
    {
        followCam.transform.position = target.position + _offset;
    }
}
