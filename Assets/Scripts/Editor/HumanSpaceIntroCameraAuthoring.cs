#if UNITY_EDITOR
using System;
using System.Linq;
using Core.Enums;
using Managers;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Splines;
using Object = UnityEngine.Object;

/// <summary>Authors the three moving intro shots without rebuilding the arena or actors.</summary>
public static class HumanSpaceIntroCameraAuthoring
{
    const string RootName = "Human Space Intro Camera Paths";

    [MenuItem("Tools/Dinosaur RPS/Restore Human Space Intro Cameras")]
    public static void RepairFinalRound()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/FinalRound.unity");
        Configure(Object.FindFirstObjectByType<TournamentGameManager>());
        EditorSceneManager.SaveScene(scene);
        Debug.Log("HUMAN_SPACE_INTRO_CAMERAS_AUTHORED");
    }

    public static void Configure(TournamentGameManager battle)
    {
        if (battle == null) throw new InvalidOperationException("Final battle manager is missing.");
        var settings = new SerializedObject(battle);
        if (settings.FindProperty("_currentStage").enumValueIndex != (int)TournamentStage.Finals)
            throw new InvalidOperationException("Space intro cameras belong to the final stage.");
        var manager = settings.FindProperty("_camController").objectReferenceValue as CameraManager;
        var player = settings.FindProperty("_playerAnimator").objectReferenceValue as Animator;
        var human = settings.FindProperty("_opponentAnimator").objectReferenceValue as Animator;
        if (manager == null || player == null || human == null || manager.idleCam == null)
            throw new InvalidOperationException("Final battle camera/actor references are missing.");

        var scene = battle.gameObject.scene;
        foreach (var old in scene.GetRootGameObjects().Where(go => go.name == RootName))
            Object.DestroyImmediate(old);
        var root = new GameObject(RootName);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);

        Vector3 p = player.transform.position, h = human.transform.position;
        Vector3 middle = (p + h) * .5f; middle.y = 0;
        MovingShot(root.transform, manager.introPlayerCam, "Scissors - Tyranno", p + new Vector3(0, 1.7f, -.3f), 42,
            p + new Vector3(-6.6f, 1.6f, -2.6f),
            p + new Vector3(-5.8f, 2.6f, -3.2f),
            p + new Vector3(-5, 3.5f, -2.7f));
        MovingShot(root.transform, manager.introEnemyCam, "Rock - Human", h + Vector3.up * 1.35f, 56,
            h + new Vector3(-4.4f, 1.2f, 2.6f),
            h + new Vector3(-3.8f, 1.55f, 2.1f),
            h + new Vector3(-3.1f, 1.85f, 1.4f));
        // Leave the top-centre chant balloon clear of the human's face.
        var humanComposer = manager.introEnemyCam.GetComponent<CinemachineRotationComposer>();
        var humanComposition = humanComposer.Composition;
        humanComposition.ScreenPosition = new Vector2(.14f, 0);
        humanComposer.Composition = humanComposition;
        EditorUtility.SetDirty(humanComposer);
        // Paper is held at the first knot until the chant ends. The last knot
        // matches the idle shot, keeping the following number countdown continuous.
        MovingShot(root.transform, manager.introZoomOutCam, "Paper - Arena Reveal", middle + Vector3.up * 1.1f,
            manager.idleCam.Lens.FieldOfView,
            middle + new Vector3(-7, 3.1f, .6f),
            middle + new Vector3(-9, 3.7f, .7f),
            manager.idleCam.transform.position);

        var cameraSettings = new SerializedObject(manager);
        cameraSettings.FindProperty("_introZoomOutLookOffset").vector3Value = Vector3.zero;
        cameraSettings.FindProperty("_playIntroOnStart").boolValue = false;
        cameraSettings.ApplyModifiedPropertiesWithoutUndo();
        manager.introDuration = 2f;
        manager.zoomOutDuration = 1.5f;
        manager.holdBeforeZoomOut = .5f;
        EditorUtility.SetDirty(manager);
        EditorSceneManager.MarkSceneDirty(scene);
    }

    static void MovingShot(Transform root, CinemachineCamera camera, string name,
        Vector3 focus, float fov, params Vector3[] points)
    {
        if (camera == null) throw new InvalidOperationException("Missing intro camera: " + name);
        var pathObject = new GameObject(name + " Path");
        pathObject.transform.SetParent(root, false);
        var container = pathObject.AddComponent<SplineContainer>();
        var spline = new Spline();
        foreach (var point in points) spline.Add(new BezierKnot(point), TangentMode.AutoSmooth);
        container.Spline = spline;
        var target = new GameObject(name + " Look Target").transform;
        target.SetParent(root, false);
        target.position = focus;

        var dolly = camera.GetComponent<CinemachineSplineDolly>();
        if (dolly == null) dolly = camera.gameObject.AddComponent<CinemachineSplineDolly>();
        dolly.Spline = container;
        dolly.PositionUnits = PathIndexUnit.Normalized;
        dolly.CameraPosition = 0;
        dolly.SplineOffset = Vector3.zero;
        dolly.CameraRotation = CinemachineSplineDolly.RotationMode.Default;
        dolly.Damping = default;
        dolly.AutomaticDolly.Enabled = false;
        camera.Follow = target;
        camera.LookAt = target;
        var composer = camera.GetComponent<CinemachineRotationComposer>();
        if (composer == null) composer = camera.gameObject.AddComponent<CinemachineRotationComposer>();
        composer.TargetOffset = Vector3.zero;
        composer.Damping = Vector2.zero;
        composer.Lookahead = default;
        composer.CenterOnActivate = true;
        var composition = ScreenComposerSettings.Default;
        composition.DeadZone.Enabled = false;
        composer.Composition = composition;
        var curve = camera.GetComponent<CameraMoveSettings>();
        if (curve == null) curve = camera.gameObject.AddComponent<CameraMoveSettings>();
        curve.moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        camera.transform.SetPositionAndRotation(points[0], Quaternion.LookRotation(focus - points[0]));
        var lens = camera.Lens; lens.FieldOfView = fov; lens.FarClipPlane = 500; camera.Lens = lens;
        EditorUtility.SetDirty(camera);
    }
}
#endif
