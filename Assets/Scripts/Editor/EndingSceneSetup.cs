#if UNITY_EDITOR
using System.IO;
using System.Linq;
using Managers;
using TMPro;
using UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using Object = UnityEngine.Object;

/// <summary>Authors the two replaceable ending clips and assigns recovery typography to active cutscenes.</summary>
public static class EndingSceneSetup
{
    const string Folder = "Assets/Graphics/Videos/Endings/";
    const string FontPath = "Assets/Graphics/Fonts/FBStdMedium SDF.asset";

    [MenuItem("Tools/Dinosaur RPS/Build Endings And Video Recovery")]
    public static void Build()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null) throw new System.InvalidOperationException("The existing Korean UI font is missing.");
        BuildEnding("GoodEnding", "Assets/Scenes/EndingScenes/VictoryAnimation.unity", true, font);
        BuildEnding("BadEnding", "Assets/Scenes/EndingScenes/BadScenes/BadAnimation.unity", false, font);
        string scriptGuid = AssetDatabase.AssetPathToGUID("Assets/Scripts/VideoSceneTransition.cs");
        foreach (var entry in EditorBuildSettings.scenes.Where(s => s.enabled && File.Exists(s.path)))
        {
            // Avoid loading/re-saving battle scenes that do not contain cutscene playback.
            if (!File.ReadAllText(entry.path).Contains(scriptGuid)) continue;
            var scene = EditorSceneManager.OpenScene(entry.path);
            foreach (var transition in Object.FindObjectsByType<VideoSceneTransition>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                RecoverySettings(transition, font);
            EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("ENDING_SCENES_AND_RECOVERY_AUTHORED");
    }

    static void BuildEnding(string clipName, string destination, bool victory, TMP_FontAsset font)
    {
        string clipPath = Folder + clipName + ".mp4";
        string posterPath = Folder + clipName + "FirstFrame.png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(posterPath);
        importer.textureType = TextureImporterType.Default;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
        var clip = AssetDatabase.LoadAssetAtPath<VideoClip>(clipPath);
        var poster = AssetDatabase.LoadAssetAtPath<Texture2D>(posterPath);
        if (clip == null || poster == null) throw new System.InvalidOperationException("Missing ending media: " + clipName);

        var scene = EditorSceneManager.OpenScene("Assets/Scenes/10End2.unity");
        var transition = Object.FindFirstObjectByType<VideoSceneTransition>();
        var settings = new SerializedObject(transition);
        var player = settings.FindProperty("_videoPlayer").objectReferenceValue as VideoPlayer;
        var firstFrame = player.GetComponent<VideoFirstFrameDisplay>();
        var displaySettings = new SerializedObject(firstFrame);
        var screen = displaySettings.FindProperty("_screen").objectReferenceValue as RawImage;
        var next = settings.FindProperty("_skipButton").objectReferenceValue as Image;
        string rtPath = Folder + clipName + ".renderTexture";
        var target = AssetDatabase.LoadAssetAtPath<RenderTexture>(rtPath);
        if (target == null)
        {
            target = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32) { name = clipName + " Screen" };
            AssetDatabase.CreateAsset(target, rtPath);
        }
        player.gameObject.name = victory ? "Good Ending Video" : "Bad Ending Video";
        player.clip = clip;
        player.source = VideoSource.VideoClip;
        player.targetTexture = target;
        player.renderMode = VideoRenderMode.RenderTexture;
        player.aspectRatio = VideoAspectRatio.FitInside;
        player.isLooping = false;
        player.playOnAwake = false;
        settings.FindProperty("_nextSceneName").stringValue = "00MainMenu";
        settings.FindProperty("_handleCompletionKeyboard").boolValue = false;
        settings.ApplyModifiedPropertiesWithoutUndo();
        RecoverySettings(transition, font);
        displaySettings.FindProperty("_firstFrame").objectReferenceValue = poster;
        displaySettings.FindProperty("_firstFrameClip").objectReferenceValue = clip;
        displaySettings.ApplyModifiedPropertiesWithoutUndo();
        screen.texture = poster;
        var aspect = screen.GetComponent<AspectRatioFitter>();
        if (aspect == null) aspect = screen.gameObject.AddComponent<AspectRatioFitter>();
        aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        aspect.aspectRatio = 16f / 9f;
        var camera = Camera.main;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;

        var menu = player.gameObject.AddComponent<EndingMenuView>();
        var menuSettings = new SerializedObject(menu);
        menuSettings.FindProperty("_transition").objectReferenceValue = transition;
        menuSettings.FindProperty("_font").objectReferenceValue = font;
        menuSettings.FindProperty("_isVictory").boolValue = victory;
        menuSettings.FindProperty("_legacyNextButton").objectReferenceValue = next.gameObject;
        menuSettings.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.SaveScene(scene, destination);
    }

    static void RecoverySettings(VideoSceneTransition transition, TMP_FontAsset font)
    {
        var settings = new SerializedObject(transition);
        settings.FindProperty("_recoveryFont").objectReferenceValue = font;
        settings.FindProperty("_prepareTimeout").floatValue = 15f;
        settings.FindProperty("_playbackStallTimeout").floatValue = 15f;
        settings.FindProperty("_mainMenuSceneName").stringValue = "00MainMenu";
        settings.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
