using UnityEngine;

public class SwitchCAM : MonoBehaviour
{
    public Camera[] cameras;

    // 0 = mainCam, 1 = zeroTwo, 2 = Enemy, 3 = SPIN

    public void SwitchCam(int index)
    {
        for (int i = 0; i < cameras.Length; i++)
        {
            cameras[i].enabled = (i == index);
        }
    }

}
