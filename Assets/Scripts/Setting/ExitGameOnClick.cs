using UnityEngine;

public class ExitGameOnClick : MonoBehaviour
{   
    public void OnClickExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
