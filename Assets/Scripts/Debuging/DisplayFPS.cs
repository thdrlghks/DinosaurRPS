using UnityEngine;

namespace Debuging
{
    public class DisplayFPS : MonoBehaviour
    {
        float deltaTime = 0.0f;

        void Update()
        {
            deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
            DontDestroyOnLoad(gameObject);
        }

        void OnGUI()
        {
            int w = Screen.width, h = Screen.height;
            GUIStyle style = new GUIStyle();
            style.alignment = TextAnchor.UpperLeft;
            style.fontSize = h * 2 / 100; 
            style.normal.textColor = new Color(1.0f, 1.0f, 1.0f, 1.0f); 

            var msec = deltaTime * 1000.0f;
            var fps = 1.0f / deltaTime;

            string text = string.Format("{0:0.0} ms ({1:0.0} fps)", msec, fps);
            GUI.Label(new Rect(10, 10, w, h * 2 / 100), text, style);
        }
    }
}