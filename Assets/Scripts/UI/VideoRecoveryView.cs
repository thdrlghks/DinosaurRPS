using System.Linq;
using Managers;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    /// <summary>Created only on video failure, below the existing settings overlay.</summary>
    public sealed class VideoRecoveryView : MonoBehaviour
    {
        private GameObject _canvasRoot;
        private GameObject _ownedEventSystem;
        private CanvasGroup _group;
        private TMP_Text _message;
        private Button _retry, _continue, _menu;
        private bool _canContinue, _canMenu;
        private TMP_FontAsset _font;

        public void Initialize(VideoSceneTransition owner, TMP_FontAsset font)
        {
            _font = font != null ? font : Resources.FindObjectsOfTypeAll<TMP_FontAsset>()
                .FirstOrDefault(candidate => candidate.name == "FBStdMedium SDF");
            if (_font == null) _font = TMP_Settings.defaultFontAsset;
            var root = Rect("Video Recovery Canvas", transform, Vector2.zero, Vector2.zero);
            _canvasRoot = root.gameObject;
            var canvas = _canvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 250;
            var scaler = _canvasRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = .5f;
            _canvasRoot.AddComponent<GraphicRaycaster>();
            _group = _canvasRoot.AddComponent<CanvasGroup>();

            var shade = Rect("Background", root, Vector2.zero, Vector2.zero);
            shade.anchorMin = Vector2.zero;
            shade.anchorMax = Vector2.one;
            shade.offsetMin = shade.offsetMax = Vector2.zero;
            shade.gameObject.AddComponent<Image>().color = new Color(0.015f, .025f, .05f, .92f);
            var panel = Rect("Recovery Panel", root, Vector2.zero, new Vector2(1080, 470));
            panel.gameObject.AddComponent<Image>().color = new Color(.08f, .105f, .16f, .98f);
            Label("Title", panel, "영상 재생에 문제가 생겼어요", 44, new Vector2(0, 140), new Vector2(1000, 75));
            _message = Label("Message", panel, "", 30, new Vector2(0, 40), new Vector2(980, 120));
            _retry = MakeButton(panel, "Retry", "다시 시도 [R]", -330, owner.RetryVideo);
            _continue = MakeButton(panel, "Continue", "계속 [Enter]", 0, owner.ContinueAfterError);
            _menu = MakeButton(panel, "Main Menu", "메인 메뉴 [M]", 330, owner.ReturnToMainMenu);
            Label("Keyboard Hint", panel, "마우스로 선택하거나 표시된 키를 눌러 주세요.  Space 키로도 계속할 수 있어요.",
                22, new Vector2(0, -195), new Vector2(1020, 40));
            if (EventSystem.current == null)
            {
                _ownedEventSystem = new GameObject("Video Recovery Event System", typeof(EventSystem), typeof(StandaloneInputModule));
                _ownedEventSystem.transform.SetParent(transform, false);
            }
            Hide();
        }

        public void Show(string message, bool canContinue, bool canMenu)
        {
            _canContinue = canContinue;
            _canMenu = canMenu;
            _message.text = message;
            _canvasRoot.SetActive(true);
        }

        public void Hide()
        {
            if (_canvasRoot != null) _canvasRoot.SetActive(false);
        }

        public void SetInteraction(bool allowed, bool blocksRaycasts)
        {
            if (_group == null) return;
            _group.interactable = allowed;
            _group.blocksRaycasts = blocksRaycasts;
            _retry.interactable = allowed;
            _continue.interactable = allowed && _canContinue;
            _menu.interactable = allowed && _canMenu;
        }

        private Button MakeButton(Transform parent, string name, string text, float x, UnityAction action)
        {
            var rect = Rect(name, parent, new Vector2(x, -100), new Vector2(300, 76));
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(.2f, .32f, .46f, 1f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var colors = button.colors;
            colors.highlightedColor = new Color(.8f, .9f, 1f);
            colors.pressedColor = new Color(.55f, .7f, .85f);
            colors.disabledColor = new Color(.35f, .35f, .35f);
            button.colors = colors;
            button.onClick.AddListener(action);
            Label(name + " Label", rect, text, 28, Vector2.zero, new Vector2(290, 68));
            return button;
        }

        private TMP_Text Label(string name, Transform parent, string value, float size, Vector2 position, Vector2 dimensions)
        {
            var rect = Rect(name, parent, position, dimensions);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = _font;
            text.text = value;
            text.fontSize = size;
            text.color = new Color(.94f, .96f, 1f);
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private void OnDestroy()
        {
            if (_canvasRoot != null) Destroy(_canvasRoot);
            if (_ownedEventSystem != null) Destroy(_ownedEventSystem);
        }
    }
}
