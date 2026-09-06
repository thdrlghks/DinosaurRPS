#if UNITY_EDITOR
using System.Linq;
using Managers;
using TMPro;
using UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Explicit scene authoring utility. No automatic scene mutation on import.
public static class GuidedTutorialSetup
{
    private static TMP_FontAsset font;
    private static SerializedObject director;

    [MenuItem("Tools/Dinosaur RPS/Configure Guided Tutorial")]
    public static void Configure()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Tutorial.unity");
        var manager = Object.FindFirstObjectByType<TournamentGameManager>();
        if (manager.GetComponent<TutorialDirector>() != null) return;
        var settings = new SerializedObject(manager);
        director = new SerializedObject(manager.gameObject.AddComponent<TutorialDirector>());
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Graphics/Fonts/FBStdMedium SDF.asset");
        var ui = Get<UIManager>(settings, "_uiManager");
        var uiSettings = new SerializedObject(ui);
        var paper = Get<Image>(settings, "_rpsPaperImage");
        Set("_paper", paper);
        Set("_unlockedPaper", Get<Sprite>(uiSettings, "_paperSprite"));
        paper.gameObject.SetActive(false);
        SetArray(director, "_controls", Get<Image>(settings, "_rpsRockImage").rectTransform,
            Get<Image>(settings, "_rpsScissorsImage").rectTransform);

        // Preserve the existing HUD transforms and use its own artwork for pips.
        var hud = Get<Canvas>(settings, "_gameHealthCanvas");
        Sprite pipSprite = ImportExistingPipArt();
        var playerPip = MakePip(hud.transform, "Tutorial Player Point", new Vector2(-300, 445), pipSprite, out var playerFill);
        var enemyPip = MakePip(hud.transform, "Tutorial Enemy Point", new Vector2(320, 445), pipSprite, out var enemyFill);
        SetArray(uiSettings, "_playerScoreEmpty", playerPip.gameObject);
        SetArray(uiSettings, "_playerScoreFilled", playerFill.gameObject);
        SetArray(uiSettings, "_opponentScoreEmpty", enemyPip.gameObject);
        SetArray(uiSettings, "_opponentScoreFilled", enemyFill.gameObject);
        var hudRects = hud.GetComponentsInChildren<RectTransform>(true);
        RectTransform Find(string name) => hudRects.First(r => r.name == name);
        SetArray(director, "_playerHud", Find("PlayerHealthEdge"), Find("PlayerPortrait"), playerPip.rectTransform);
        SetArray(director, "_enemyHud", Find("EnemeyHealthEdge"), Find("EnemyPortrait"), enemyPip.rectTransform);
        uiSettings.ApplyModifiedPropertiesWithoutUndo();

        var root = new GameObject("Tutorial Guidance", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.layer = 5;
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 70;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;

        var number = Picture(root.transform, "3 2 1", new Vector2(0, 80), new Vector2(150, 195));
        Set("_number", number);
        SetArray(director, "_numbers", ExistingSprite("Count 3"), ExistingSprite("Count 2"), ExistingSprite("Count 1"));
        number.gameObject.SetActive(false);

        var maskRect = Rect(root.transform, "Black Spotlight Filter", Vector2.zero, Vector2.zero);
        maskRect.anchorMin = Vector2.zero; maskRect.anchorMax = Vector2.one;
        var mask = maskRect.gameObject.AddComponent<TutorialSpotlight>();
        mask.color = new Color(0, 0, 0, .78f);
        mask.raycastTarget = true;
        Set("_shade", mask);
        mask.gameObject.SetActive(false);

        var explanation = Rect(root.transform, "Explanation", Vector2.zero, new Vector2(1140, 460));
        Set("_explanation", explanation);
        Set("_step", Text(explanation, "Step", 28, new Vector2(0, 160), new Vector2(1080, 45), new Color32(237, 214, 148, 255)));
        Set("_title", Text(explanation, "Title", 48, new Vector2(0, 88), new Vector2(1080, 80), Color.white));
        Set("_body", Text(explanation, "Body", 31, new Vector2(0, -40), new Vector2(1100, 150), Color.white));
        var nextRect = Rect(explanation, "Continue", new Vector2(0, -190), new Vector2(460, 64));
        var nextImage = nextRect.gameObject.AddComponent<Image>();
        nextImage.color = new Color(0, 0, 0, .35f);
        var next = nextRect.gameObject.AddComponent<Button>();
        next.targetGraphic = nextImage;
        Set("_continueButton", next);
        Set("_continueLabel", Text(nextRect, "Label", 29, Vector2.zero, new Vector2(440, 60), Color.white));
        explanation.gameObject.SetActive(false);

        var reward = Picture(root.transform, "Existing Paper Unlock Icon", new Vector2(0, 40), new Vector2(340, 340));
        reward.sprite = paper.sprite;
        Set("_reward", reward);
        reward.gameObject.SetActive(false);
        foreach (var otherCanvas in scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Canvas>(true)))
        {
            if (otherCanvas.name != "SettingCanvas") continue;
            otherCanvas.sortingOrder = 120;
            PrefabUtility.RecordPrefabInstancePropertyModifications(otherCanvas);
        }
        director.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("GUIDED_TUTORIAL_CONFIGURED");
    }

    private static Sprite ExistingSprite(string name) => AssetDatabase.LoadAllAssetsAtPath(
        "Assets/Graphics/UI/PreGame/BattleImage/" + name + ".png").OfType<Sprite>().First();

    private static Sprite ImportExistingPipArt()
    {
        const string path = "Assets/Graphics/UI/Gameplay/CharacterPortraits/Skillicon_paper_edge.png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.textureShape = TextureImporterShape.Texture2D;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
#pragma warning disable CS0618
        importer.spritesheet = new[] { new SpriteMetaData { name = "ExistingSkillCircle", rect = new Rect(721, 246, 505, 503), pivot = Vector2.one * .5f, alignment = (int)SpriteAlignment.Center } };
#pragma warning restore CS0618
        importer.SaveAndReimport();
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().First();
    }

    private static Image MakePip(Transform parent, string name, Vector2 position, Sprite sprite, out Image filled)
    {
        var empty = Picture(parent, name, position, new Vector2(82, 82));
        empty.sprite = sprite;
        empty.color = new Color(.2f, .2f, .2f, 1f);
        filled = Picture(parent, name + " Won", position, new Vector2(82, 82));
        filled.sprite = sprite;
        filled.gameObject.SetActive(false);
        return empty;
    }

    private static T Get<T>(SerializedObject obj, string field) where T : Object => obj.FindProperty(field).objectReferenceValue as T;
    private static void Set(string field, Object value) => director.FindProperty(field).objectReferenceValue = value;
    private static void SetArray(SerializedObject obj, string field, params Object[] values)
    {
        var array = obj.FindProperty(field); array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }
    private static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.layer = 5;
        var rect = (RectTransform)go.transform; rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
        rect.anchoredPosition = position; rect.sizeDelta = size;
        return rect;
    }
    private static Image Picture(Transform parent, string name, Vector2 position, Vector2 size)
    {
        var image = Rect(parent, name, position, size).gameObject.AddComponent<Image>();
        image.preserveAspect = true; image.raycastTarget = false; return image;
    }
    private static TMP_Text Text(Transform parent, string name, int size, Vector2 position, Vector2 dimensions, Color color)
    {
        var text = Rect(parent, name, position, dimensions).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font; text.fontSize = size; text.color = color; text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        var shadow = text.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(0, 0, 0, .9f); shadow.effectDistance = new Vector2(2, -2);
        return text;
    }
}
#endif
