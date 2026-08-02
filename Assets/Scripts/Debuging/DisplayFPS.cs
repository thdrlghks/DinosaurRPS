using UnityEngine;

namespace Debuging
{
    /// <summary>
    /// 개발용 FPS 오버레이.
    /// 릴리스 빌드에서는 스스로를 제거해 OnGUI 비용을 남기지 않는다.
    /// </summary>
    public class DisplayFPS : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD

        private static DisplayFPS _instance;

        private float _deltaTime;
        private GUIStyle _style;
        private Rect _rect;
        private int _cachedScreenHeight = -1;

        private void Awake()
        {
            // 이 컴포넌트는 여러 씬에 각각 놓여 있다. 가드가 없으면
            // DontDestroyOnLoad 때문에 씬을 넘어갈 때마다 하나씩 쌓인다.
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            // HitStop이 timeScale을 0으로 만들기 때문에 unscaled를 써야 값이 죽지 않는다.
            _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;
        }

        private void OnGUI()
        {
            // OnGUI는 프레임당 여러 번(Layout/Repaint 등) 불린다. 실제로 그리는 건 Repaint뿐.
            if (Event.current.type != EventType.Repaint) return;

            // 매 호출마다 GUIStyle을 새로 만들면 계속 GC를 긁는다. 해상도가 바뀔 때만 갱신.
            if (_style == null || Screen.height != _cachedScreenHeight)
            {
                _cachedScreenHeight = Screen.height;
                int fontSize = _cachedScreenHeight * 2 / 100;

                _style = new GUIStyle { alignment = TextAnchor.UpperLeft, fontSize = fontSize };
                _style.normal.textColor = Color.white;
                _rect = new Rect(10f, 10f, Screen.width, fontSize);
            }

            float msec = _deltaTime * 1000f;
            float fps = _deltaTime > 0f ? 1f / _deltaTime : 0f;

            GUI.Label(_rect, string.Format("{0:0.0} ms ({1:0.0} fps)", msec, fps), _style);
        }

#else

        private void Awake()
        {
            // 릴리스 빌드: 오버레이도 Update/OnGUI 비용도 남기지 않는다.
            Destroy(gameObject);
        }

#endif
    }
}
