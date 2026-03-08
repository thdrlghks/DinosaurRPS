using UnityEngine;

public class CameraMoveSettings : MonoBehaviour
{
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
}