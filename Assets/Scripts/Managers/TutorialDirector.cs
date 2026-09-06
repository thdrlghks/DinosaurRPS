using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Managers
{
    public enum TutorialMoment { Intro, Controls, Health, Countdown, Forfeit, VictoryMotion, ChickenDefeat, LockedReward, UnlockedReward, Complete }

    /// <summary>Only the Tutorial scene owns this guided presentation.</summary>
    public sealed class TutorialDirector : MonoBehaviour
    {
        [SerializeField] private Image _paper;
        [SerializeField] private Sprite _unlockedPaper;
        [SerializeField] private RectTransform[] _controls;
        [SerializeField] private RectTransform[] _playerHud;
        [SerializeField] private RectTransform[] _enemyHud;
        [SerializeField] private TutorialSpotlight _shade;
        [SerializeField] private RectTransform _explanation;
        [SerializeField] private TMP_Text _step;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _body;
        [SerializeField] private Button _continueButton;
        [SerializeField] private TMP_Text _continueLabel;
        [SerializeField] private Image _number;
        [SerializeField] private Sprite[] _numbers;
        [SerializeField] private Image _reward;

        private Sprite _lockedPaper;
        private bool _advance;
        private bool _guideShown;
        private readonly Vector3[] _corners = new Vector3[4];

        public bool PaperUnlocked { get; private set; }
        public bool CanContinue { get; private set; }
        public int Forfeits { get; private set; }
        public TutorialMoment Moment { get; private set; }
        public int CountdownNumber { get; private set; }

        private void Awake()
        {
            _lockedPaper = _paper.sprite;
            _paper.gameObject.SetActive(false);
            _shade.gameObject.SetActive(false);
            _explanation.gameObject.SetActive(false);
            _number.gameObject.SetActive(false);
            _reward.gameObject.SetActive(false);
            _continueButton.onClick.AddListener(Continue);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) Continue();
        }

        private void LateUpdate()
        {
            // Compute windows from the existing UI, including its key labels.
            // This keeps them aligned when resolution or canvas scaling changes.
            if (Moment == TutorialMoment.Controls)
                _shade.SetWindows(Window(_controls));
            else if (Moment == TutorialMoment.Health)
                _shade.SetWindows(Window(_playerHud), Window(_enemyHud));
            else if (Moment == TutorialMoment.Complete)
                _shade.SetWindows(Window(new[] { _paper.rectTransform }));
        }

        public void Continue()
        {
            if (CanContinue && Time.timeScale > 0f) _advance = true;
        }

        public async UniTask ShowThreeAndGuide(CancellationToken token)
        {
            SetNumber(3);
            if (_guideShown) return;
            await UniTask.Delay(300, cancellationToken: token);
            _number.rectTransform.anchoredPosition = new Vector2(0, 300);
            _number.rectTransform.sizeDelta = new Vector2(100, 130);

            Moment = TutorialMoment.Controls;
            _shade.gameObject.SetActive(true);
            _shade.SetWindows(Window(_controls));
            Explain("1 / 2  ·  손 선택", "Q는 바위, E는 가위!",
                "카운트가 끝나기 전에 키를 눌러 손을 선택하세요.\n선택하지 않으면 이번 라운드는 실격패!\n보자기 W는 닭에게 이긴 뒤 사용할 수 있어요.", new Vector2(200, 40));
            await WaitForContinue("[Space]  다음", token);

            Moment = TutorialMoment.Health;
            _shade.SetWindows(Window(_playerHud), Window(_enemyHud));
            Explain("2 / 2  ·  대결 상황", "체력과 승리 포인트",
                "왼쪽은 내 체력, 오른쪽은 상대의 체력이에요.\n라운드에서 이기면 위의 승리 포인트가 채워져요.\n이제 Q 또는 E로 손을 골라 보세요!", new Vector2(0, -70));
            await WaitForContinue("[Space]  대결 시작", token);

            _guideShown = true;
            _explanation.gameObject.SetActive(false);
            _shade.gameObject.SetActive(false);
            Moment = TutorialMoment.Countdown;
        }

        public async UniTask RunNumberCountdown(CancellationToken token)
        {
            Moment = TutorialMoment.Countdown;
            _number.rectTransform.anchoredPosition = new Vector2(0, 80);
            _number.rectTransform.sizeDelta = new Vector2(150, 195);
            for (int number = 3; number >= 1; number--)
            {
                SetNumber(number);
                await UniTask.Delay(1200, cancellationToken: token);
            }
            _number.gameObject.SetActive(false);
            CountdownNumber = 0;
        }

        public async UniTask ShowForfeit(CancellationToken token)
        {
            Moment = TutorialMoment.Forfeit;
            Forfeits++;
            // Leave the character visible while its existing Lose clip plays.
            Explain("이번 라운드 패배", "실격패",
                "시간 안에 손을 선택하지 않았어요.\nQ 또는 E를 눌러 다시 도전해 보세요.", new Vector2(300, 50));
            _continueButton.gameObject.SetActive(false);
            await UniTask.Delay(2200, cancellationToken: token);
            await WaitForContinue("[Space]  다시 도전", token);
            _explanation.gameObject.SetActive(false);
            Moment = TutorialMoment.Intro;
        }

        public void MarkVictoryMotion() => Moment = TutorialMoment.VictoryMotion;
        public void MarkChickenDefeat() => Moment = TutorialMoment.ChickenDefeat;

        public async UniTask UnlockPaper(CancellationToken token)
        {
            Moment = TutorialMoment.LockedReward;
            _shade.SetWindows();
            _shade.gameObject.SetActive(true);
            _reward.gameObject.SetActive(true);
            _reward.sprite = _lockedPaper;
            RectTransform reward = _reward.rectTransform;
            reward.anchoredPosition = new Vector2(0, 40);
            reward.sizeDelta = new Vector2(340, 340);
            Explain("새로운 손", "보자기를 얻었다!", "잠금 해제 중…", new Vector2(0, 260));
            _body.rectTransform.anchoredPosition = new Vector2(0, -510);
            _continueButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -610);
            _continueButton.gameObject.SetActive(false);
            Vector2 origin = reward.anchoredPosition;
            try
            {
                for (float elapsed = 0; elapsed < 2f; elapsed += Time.deltaTime)
                {
                    reward.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(elapsed * 44f) * 11f);
                    reward.anchoredPosition = origin + Vector2.right * Mathf.Sin(elapsed * 61f) * 7f;
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }
            }
            finally
            {
                if (reward != null) { reward.anchoredPosition = origin; reward.localRotation = Quaternion.identity; }
            }

            Moment = TutorialMoment.UnlockedReward;
            _reward.sprite = _unlockedPaper;
            _paper.sprite = _unlockedPaper;
            PaperUnlocked = true;
            _body.text = "잠금 해제!  이제 W로 보자기를 낼 수 있어요.";
            await UniTask.Delay(1000, cancellationToken: token);

            // Put the acquired hand back in its existing W slot.
            Rect target = Window(new[] { _paper.rectTransform }, includeChildren: false, padding: 0);
            for (float elapsed = 0; elapsed < .65f; elapsed += Time.deltaTime)
            {
                float t = Mathf.SmoothStep(0, 1, elapsed / .65f);
                reward.anchoredPosition = Vector2.Lerp(origin, target.center, t);
                reward.sizeDelta = Vector2.Lerp(new Vector2(340, 340), target.size, t);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
            _paper.gameObject.SetActive(true);
            _reward.gameObject.SetActive(false);
            _shade.SetWindows(Window(new[] { _paper.rectTransform }));
            Moment = TutorialMoment.Complete;
            await WaitForContinue("[Space]  다음으로", token);
        }

        private void SetNumber(int number)
        {
            CountdownNumber = number;
            _number.sprite = _numbers[3 - number];
            _number.gameObject.SetActive(true);
        }

        private void Explain(string step, string title, string body, Vector2 position)
        {
            _explanation.anchoredPosition = position;
            _step.text = step;
            _title.text = title;
            _body.text = body;
            _body.rectTransform.anchoredPosition = new Vector2(0, -40);
            _continueButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -190);
            _explanation.gameObject.SetActive(true);
        }

        private async UniTask WaitForContinue(string label, CancellationToken token)
        {
            _advance = false;
            CanContinue = false;
            _continueLabel.text = label;
            _continueButton.gameObject.SetActive(true);
            await UniTask.Delay(250, cancellationToken: token);
            CanContinue = true;
            try { await UniTask.WaitUntil(() => _advance, cancellationToken: token); }
            finally { CanContinue = false; }
        }

        private Rect Window(RectTransform[] targets, bool includeChildren = true, float padding = 18)
        {
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            foreach (RectTransform target in targets)
            {
                Encapsulate(target, ref min, ref max);
                if (!includeChildren) continue;
                foreach (Graphic graphic in target.GetComponentsInChildren<Graphic>())
                    if (graphic.gameObject.activeInHierarchy) Encapsulate(graphic.rectTransform, ref min, ref max);
            }
            return Rect.MinMaxRect(min.x - padding, min.y - padding, max.x + padding, max.y + padding);
        }

        private void Encapsulate(RectTransform target, ref Vector2 min, ref Vector2 max)
        {
            target.GetWorldCorners(_corners);
            Canvas canvas = target.GetComponentInParent<Canvas>();
            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            foreach (Vector3 corner in _corners)
            {
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(camera, corner);
                Canvas overlayCanvas = _shade.canvas;
                Camera overlayCamera = overlayCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : overlayCanvas.worldCamera;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_shade.rectTransform, screen, overlayCamera, out Vector2 local);
                min = Vector2.Min(min, local); max = Vector2.Max(max, local);
            }
        }

        private void OnDestroy() { CanContinue = false; }
    }
}
