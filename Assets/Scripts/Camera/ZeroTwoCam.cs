using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class ZeroTwoCam : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Transform followCam;

    private Vector3 _offset;

    void Start()
    {
        /*zeroTwoTarget = GameObject.Find("main").transform;
        followCam = GameObject.Find("ZeroTwoCam").transform;*/

        _offset = followCam.transform.position - player.position;
    }

    void LateUpdate()
    {
        followCam.transform.position = player.position - player.forward * 5f + Vector3.up * 7f;
        followCam.transform.LookAt(player.position + Vector3.up * 4f);
    }
}
