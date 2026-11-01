using Managers;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    /// <summary>Finishes the ending on its last frame and offers an explicit return to the menu.</summary>
    public sealed class EndingMenuView : MonoBehaviour
    {
        [SerializeField] private VideoSceneTransition _transition;
        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private bool _isVictory;
        [SerializeField] private GameObject _legacyNextButton;
        private CanvasGroup _group;
        private Button _menuButton;
        private GameObject _ownedEventSystem;
        private bool _awaitingRelease = true;
        private bool _leaving;
        private float _fade;

        public Button MenuButton => _menuButton;
        public bool CanReturnToMenu => isActiveAndEnabled && _group != null && _fade >= 1f
            && !_awaitingRelease && !_leaving && !GamePause.BlocksGameplayInput
            && _transition != null && _transition.IsPlaybackComplete && _transition.CanReturnToMainMenu;

        private void Update()
        {
            if (_transition == null || !_transition.IsPlaybackComplete || _transition.HasPlaybackFailed)
            {
                if (_group != null) _group.gameObject.SetActive(false);
                _fade = 0f;
                _awaitingRelease = true;
                _leaving = false;
                return;
            }
            if (_group == null) Build();
            _group.gameObject.SetActive(true);
            if (_legacyNextButton != null) _legacyNextButton.SetActive(false);
            if (!GamePause.IsPaused) _fade = Mathf.Min(1f, _fade + Time.unscaledDeltaTime / .35f);
            _group.alpha = _fade;
            if (_awaitingRelease && !GamePause.BlocksGameplayInput && InputsReleased()) _awaitingRelease = false;
            _group.blocksRaycasts = !GamePause.IsPaused;
            _group.interactable = CanReturnToMenu;
            _menuButton.interactable = CanReturnToMenu;
            if (CanReturnToMenu && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)
                || Input.GetKeyDown(KeyCode.Space))) ReturnToMainMenu();
        }

        public void ReturnToMainMenu()
        {
            if (!CanReturnToMenu || Input.GetKeyDown(KeyCode.Escape)) return;
            _leaving = true;
            _group.interactable = false;
            _menuButton.interactable = false;
            _transition.ReturnToMainMenu();
        }

        private static bool InputsReleased() => !Input.GetKey(KeyCode.Return) && !Input.GetKey(KeyCode.KeypadEnter)
            && !Input.GetKey(KeyCode.Space) && !Input.GetMouseButton(0);

        private void Build()
        {
            var root = new GameObject("Ending Menu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster), typeof(CanvasGroup));
            root.transform.SetParent(transform, false);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 220;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            _group = root.GetComponent<CanvasGroup>();
            _group.alpha = 0;

            var panel = Rect("Ending Footer", root.transform, new Vector2(.5f, 0), new Vector2(.5f, 0),
                new Vector2(0, 24), new Vector2(1660, 230));
            var background = panel.gameObject.AddComponent<Image>();
            background.color = new Color(.035f, .045f, .06f, .94f);
            var accent = _isVictory ? new Color(1f, .78f, .32f) : new Color(.95f, .48f, .4f);
            var stripe = Rect("Accent", panel, new Vector2(.5f, 1), new Vector2(.5f, 1),
                Vector2.zero, new Vector2(1660, 4));
            stripe.gameObject.AddComponent<Image>().color = accent;
            Label(panel, "Outcome", _isVictory ? "우승을 축하합니다!" : "이번 도전은 여기까지",
                new Vector2(54, -36), new Vector2(1000, 74), 48, accent);
            Label(panel, "Message", _isVictory ? "마지막 승부까지 함께해 주셔서 감사합니다." : "다음 도전에서 다시 만나요.",
                new Vector2(58, -120), new Vector2(980, 54), 30, new Color(.87f, .89f, .93f));
            var buttonRect = Rect("Main Menu Button", panel, new Vector2(1, .5f), new Vector2(1, .5f),
                new Vector2(-48, 0), new Vector2(450, 96));
            var image = buttonRect.gameObject.AddComponent<Image>();
            image.color = new Color(.88f, .78f, .57f);
            _menuButton = buttonRect.gameObject.AddComponent<Button>();
            _menuButton.targetGraphic = image;
            var colors = _menuButton.colors;
            colors.highlightedColor = new Color(1, .94f, .77f);
            colors.pressedColor = new Color(.7f, .62f, .45f);
            _menuButton.colors = colors;
            _menuButton.onClick.AddListener(ReturnToMainMenu);
            var label = Rect("Label", buttonRect, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            label.anchorMax = Vector2.one; label.offsetMin = label.offsetMax = Vector2.zero;
            var text = label.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = _font; text.text = "메인 메뉴"; text.fontSize = 38;
            text.alignment = TextAlignmentOptions.Center; text.color = new Color(.16f, .11f, .06f); text.raycastTarget = false;
            Label(panel, "Input Hint", "Enter / Space", new Vector2(1235, -177), new Vector2(340, 34),
                22, new Color(.72f, .75f, .8f));
            if (EventSystem.current == null)
                _ownedEventSystem = new GameObject("Ending EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private void Label(Transform parent, string name, string value, Vector2 position, Vector2 size, float fontSize, Color color)
        {
            var rect = Rect(name, parent, new Vector2(0, 1), new Vector2(0, 1), position, size);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = _font; text.text = value; text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.MidlineLeft; text.color = color; text.raycastTarget = false;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot; rect.anchoredPosition = position; rect.sizeDelta = size;
            return rect;
        }

        private void OnDestroy()
        {
            if (_menuButton != null) _menuButton.onClick.RemoveListener(ReturnToMainMenu);
            if (_ownedEventSystem != null) Destroy(_ownedEventSystem);
        }
    }
}
