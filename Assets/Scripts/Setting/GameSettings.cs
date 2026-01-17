using UnityEngine;

[CreateAssetMenu(fileName = "GameSettings", menuName = "Settings/GameSettings")]
public class GameSettings : ScriptableObject
{
    public float masterVolume = 1f;
    public float effectVolume = 1f;
    public bool isFullScreen = true;
}