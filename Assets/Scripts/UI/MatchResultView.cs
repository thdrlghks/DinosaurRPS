using System;
using System.Threading;
using Core.Data;
using Core.Enums;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    /// <summary>Displays one completed match on the existing parchment artwork.</summary>
    public sealed class MatchResultView : MonoBehaviour
    {
        private const float ArtworkWidth = 2360f;
        private const float ArtworkHeight = 1632f;
        private const float PaperWidth = 2202f;
        private const float PaperHeight = 1278f;
        private static readonly Color Ink = new Color32(83, 52, 16, 255);
        private static readonly Color WinInk = new Color32(36, 105, 46, 255);
        private static readonly Color LoseInk = new Color32(153, 45, 27, 255);
        private RectTransform _canvasRect;
        private RectTransform _artwork;
        private GameObject _ownedEventSystem;
        private Button _nextButton;
        private TMP_FontAsset _font;
        private bool _canContinue;
        private bool _continued;
        private bool _showing;

        public bool IsShowing => _showing;
        public bool CanContinue => _canContinue;

        public async UniTask ShowAsync(
            MatchRecord record,
            Texture2D background,
            TMP_FontAsset font,
            Sprite[] playerHandSprites,
            Sprite[] opponentHandSprites,
            CancellationToken token)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            if (background == null) throw new ArgumentNullException(nameof(background));
            if (font == null) throw new ArgumentNullException(nameof(font));
            if (_showing) throw new InvalidOperationException("The match result is already open.");

            _showing = true;
            _continued = false;
            _canContinue = false;
            _font = font;
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, this.GetCancellationTokenOnDestroy());
            try
            {
                Build(record, new MatchResultSummary(record), background, playerHandSprites, opponentHandSprites);
                FitArtwork();
                Canvas.ForceUpdateCanvases();
                // A key held while the result appears must be released before it can advance.
                await UniTask.Delay(250, ignoreTimeScale: true, cancellationToken: linked.Token);
                await UniTask.WaitUntil(AdvanceInputsReleased, cancellationToken: linked.Token);
                _canContinue = true;
                _nextButton.interactable = true;
                await UniTask.WaitUntil(() => _continued, cancellationToken: linked.Token);
            }
            finally
            {
                _canContinue = false;
                _showing = false;
                if (_canvasRect != null) Destroy(_canvasRect.gameObject);
                if (_ownedEventSystem != null) Destroy(_ownedEventSystem);
                _canvasRect = null;
                _artwork = null;
                _nextButton = null;
                _ownedEventSystem = null;
            }
        }

        private void Update()
        {
            if (_canContinue && (Input.GetKeyDown(KeyCode.Return) ||
                                 Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space)))
                Continue();
        }

        private void LateUpdate() => FitArtwork();

        private static bool AdvanceInputsReleased() =>
            !Input.GetKey(KeyCode.Return) && !Input.GetKey(KeyCode.KeypadEnter) &&
            !Input.GetKey(KeyCode.Space) && !Input.GetMouseButton(0);

        public void Continue()
        {
            if (!_canContinue) return;
            _canContinue = false;
            _continued = true;
            if (_nextButton != null) _nextButton.interactable = false;
        }

        private void Build(MatchRecord record, MatchResultSummary summary, Texture2D background,
            Sprite[] playerSprites, Sprite[] opponentSprites)
        {
            _canvasRect = NewRect("Match Result Canvas", transform);
            var canvas = _canvasRect.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 200;
            var scaler = _canvasRect.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            _canvasRect.gameObject.AddComponent<GraphicRaycaster>();

            var blocker = NewRect("Background Input Blocker", _canvasRect);
            Stretch(blocker);
            var blockerImage = blocker.gameObject.AddComponent<Image>();
            blockerImage.color = new Color(0.08f, 0.06f, 0.035f, 0.96f);
            blockerImage.raycastTarget = true;

            _artwork = NewRect("Original Result Artwork", _canvasRect);
            _artwork.anchorMin = _artwork.anchorMax = _artwork.pivot = new Vector2(0.5f, 0.5f);
            _artwork.sizeDelta = new Vector2(ArtworkWidth, ArtworkHeight);
            var image = _artwork.gameObject.AddComponent<RawImage>();
            image.texture = background;
            image.color = Color.white;
            image.raycastTarget = false;

            Text("Match Title", _artwork, 180, 210, 820, 80, StageName(record.Stage) + " 경기 결과", 62,
                TextAlignmentOptions.Center);
            Text("Match Outcome", _artwork, 1040, 210, 1110, 80,
                record.FinalResult.HasValue ? ResultName(record.FinalResult.Value) + "!" : "대결 기록", 64,
                TextAlignmentOptions.Center, record.FinalResult.HasValue ? ResultColor(record.FinalResult.Value) : Ink);

            BuildRounds(record, summary, playerSprites, opponentSprites);
            // The artwork's three drawings are ordered scissors, paper, rock.
            BuildHandRate(summary, HandType.Scissors, 1045);
            BuildHandRate(summary, HandType.Paper, 1424);
            BuildHandRate(summary, HandType.Rock, 1805);
            BuildSummary(summary);
            BuildNextButton();

            if (EventSystem.current == null)
            {
                _ownedEventSystem = new GameObject("Match Result EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }
        }

        private void BuildRounds(MatchRecord record, MatchResultSummary summary,
            Sprite[] playerSprites, Sprite[] opponentSprites)
        {
            Text("Round Header", _artwork, 215, 335, 135, 65, "라운드", 38, TextAlignmentOptions.Center);
            Text("Player Header", _artwork, 365, 335, 140, 65, "나", 42, TextAlignmentOptions.Center);
            Text("Opponent Header", _artwork, 535, 335, 140, 65, "상대", 42, TextAlignmentOptions.Center);
            Text("Outcome Header", _artwork, 715, 335, 230, 65, "결과", 42, TextAlignmentOptions.Center);

            var scrollRect = Rect("Round List", _artwork, 205, 415, 770, 775);
            var scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
            var scrollHit = scrollRect.gameObject.AddComponent<Image>();
            scrollHit.color = Color.clear;
            scrollHit.raycastTarget = true;
            var viewport = NewRect("Viewport", scrollRect);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = NewRect("Round Records", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            const float rowHeight = 112f;
            content.sizeDelta = new Vector2(0, Mathf.Max(775, record.Rounds.Count * rowHeight));
            content.anchoredPosition = Vector2.zero;
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = record.Rounds.Count * rowHeight > 775;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 45f;
            scroll.inertia = true;

            for (int i = 0; i < record.Rounds.Count; i++)
            {
                RoundRecord round = record.Rounds[i];
                float y = i * rowHeight;
                Text("Round " + round.RoundNumber, content, 10, y + 16, 135, 72,
                    round.RoundNumber.ToString(), 43, TextAlignmentOptions.Center);
                HandIcon("Player Hand", content, 170, y + 2, playerSprites, round.PlayerHand);
                HandIcon("Opponent Hand", content, 340, y + 2, opponentSprites, round.OpponentHand);
                Text("Round Result", content, 510, y + 16, 230, 72, ResultName(round.Result), 48,
                    TextAlignmentOptions.Center, ResultColor(round.Result));
            }
            if (record.Rounds.Count == 0)
                Text("Empty Round List", content, 20, 120, 720, 100,
                    "기록된 라운드가 없어요.", 43, TextAlignmentOptions.Center);
            if (scroll.vertical)
                Text("Scroll Hint", _artwork, 235, 1198, 700, 38, "위아래로 스크롤해서 더 보기", 29,
                    TextAlignmentOptions.Center);
            Text("Match Totals", _artwork, 220, 1240, 730, 70,
                $"{summary.Wins}승  {summary.Draws}무  {summary.Loses}패", 60, TextAlignmentOptions.Center);
        }

        private void BuildHandRate(MatchResultSummary summary, HandType hand, float x)
        {
            var performance = summary.GetHand(hand);
            string rate = "승률 " + (performance.Attempts == 0 ? "—" : performance.WinRate.ToString("0.#") + "%");
            Text(hand + " Win Rate", _artwork, x, 570, 340, 57, rate, 51, TextAlignmentOptions.Center);
            Text(hand + " Sample Count", _artwork, x, 625, 340, 30,
                $"{performance.Wins}승 / {performance.Attempts}회 사용", 28, TextAlignmentOptions.Center);
        }

        private void BuildSummary(MatchResultSummary summary)
        {
            Text("Summary Heading", _artwork, 1090, 718, 1010, 70, "나의 승부 성향", 56,
                TextAlignmentOptions.Center);
            Text("Personality", _artwork, 1090, 812, 1010, 110, summary.PersonalityText, 51,
                TextAlignmentOptions.Center);
            string mostUsed = summary.MostUsedHandText;
            if (summary.TotalRounds > 0 && summary.MostUsedHands.Count > 0)
            {
                float share = 100f * summary.GetHand(summary.MostUsedHands[0]).Attempts / summary.TotalRounds;
                string qualifier = summary.MostUsedHands.Count > 1 ? "각각" : "전체의";
                mostUsed += $" · {qualifier} {share:0.#}%";
            }
            string best = summary.BestHandText;
            if (summary.BestHand.HasValue)
            {
                var performance = summary.GetHand(summary.BestHand.Value);
                best += $" · {performance.Wins}승 / {performance.Attempts}회";
            }
            SummaryRow("가장 많이 낸 패", mostUsed, 949, 85);
            SummaryRow("가장 잘 통한 패", best, 1056, 85);
            SummaryRow("최고 연승", summary.MaxConsecutiveWins + "연승", 1163, 85);
        }

        private void SummaryRow(string label, string value, float y, float height)
        {
            Text(label + " Label", _artwork, 1090, y, 350, height, label, 42, TextAlignmentOptions.MidlineLeft);
            Text(label + " Value", _artwork, 1460, y, 640, height, value, 45, TextAlignmentOptions.MidlineLeft);
        }

        private void BuildNextButton()
        {
            Text("Continue Hint", _artwork, 1220, 1358, 625, 65,
                "Enter / Space로도 다음", 34, TextAlignmentOptions.MidlineRight);
            var rect = Rect("Next Button", _artwork, 1880, 1347, 255, 98);
            var hit = rect.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            _nextButton = rect.gameObject.AddComponent<Button>();
            _nextButton.targetGraphic = hit;
            _nextButton.transition = Selectable.Transition.None;
            _nextButton.navigation = new Navigation { mode = Navigation.Mode.None };
            _nextButton.interactable = false;
            _nextButton.onClick.AddListener(Continue);
            Text("Next Label", rect, 0, 12, 125, 65, "다음", 50, TextAlignmentOptions.Center);
            var arrowRect = Rect("Next Arrow", rect, 138, 14, 105, 62);
            var arrow = arrowRect.gameObject.AddComponent<MatchResultArrow>();
            arrow.color = Ink;
            arrow.raycastTarget = false;
        }

        private void HandIcon(string name, Transform parent, float x, float y, Sprite[] sprites, HandType hand)
        {
            int index = (int)hand;
            Sprite sprite = sprites != null && index >= 0 && index < sprites.Length ? sprites[index] : null;
            if (sprite == null)
            {
                Text(name, parent, x, y + 15, 120, 72, HandName(hand), 38, TextAlignmentOptions.Center);
                return;
            }
            var rect = Rect(name, parent, x, y, 110, 104);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = false;
        }

        private TextMeshProUGUI Text(string name, Transform parent, float x, float y, float width, float height,
            string value, float size, TextAlignmentOptions alignment, Color? color = null)
        {
            var rect = Rect(name, parent, x, y, width, height);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = _font;
            text.text = value;
            text.color = color ?? Ink;
            text.alignment = alignment;
            text.fontSize = size;
            text.enableAutoSizing = true;
            text.fontSizeMin = size * 0.72f;
            text.fontSizeMax = size;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            text.margin = Vector4.zero;
            return text;
        }

        private void FitArtwork()
        {
            if (_canvasRect == null || _artwork == null) return;
            var available = _canvasRect.rect.size - new Vector2(28, 28);
            float scale = Mathf.Max(0.01f, Mathf.Min(available.x / PaperWidth, available.y / PaperHeight));
            _artwork.localScale = Vector3.one * scale;
            // Alpha bounds are (90,178)-(2292,1456): center the paper, preserving its torn edges.
            _artwork.anchoredPosition = new Vector2(-11f, 1f) * scale;
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static RectTransform Rect(string name, Transform parent, float x, float y, float width, float height)
        {
            var rect = NewRect(name, parent);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static string HandName(HandType hand) => hand switch
        {
            HandType.Rock => "바위",
            HandType.Paper => "보",
            _ => "가위"
        };
        private static string ResultName(GameResult result) => result switch
        {
            GameResult.Win => "승리",
            GameResult.Lose => "패배",
            _ => "무승부"
        };
        private static Color ResultColor(GameResult result) => result switch
        {
            GameResult.Win => WinInk,
            GameResult.Lose => LoseInk,
            _ => Ink
        };
        private static string StageName(TournamentStage stage) => stage switch
        {
            TournamentStage.Qualifiers => "튜토리얼",
            TournamentStage.QuarterFinals => "8강",
            TournamentStage.SemiFinals => "4강",
            TournamentStage.Finals => "결승",
            _ => "최종"
        };
    }

    [RequireComponent(typeof(CanvasRenderer))]
    internal sealed class MatchResultArrow : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = GetPixelAdjustedRect();
            Vector2[] points =
            {
                new Vector2(0, .32f), new Vector2(.58f, .32f), new Vector2(.58f, 0),
                new Vector2(1, .5f), new Vector2(.58f, 1), new Vector2(.58f, .68f), new Vector2(0, .68f)
            };
            foreach (var point in points)
                vh.AddVert(new Vector3(rect.x + point.x * rect.width, rect.y + point.y * rect.height), color, Vector2.zero);
            vh.AddTriangle(0, 1, 5);
            vh.AddTriangle(0, 5, 6);
            vh.AddTriangle(2, 3, 4);
        }
    }
}
