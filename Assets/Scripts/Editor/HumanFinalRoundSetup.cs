#if UNITY_EDITOR
using System;
using System.Linq;
using Core.Enums;
using Managers;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Explicit authoring command; never changes scenes on import or in play mode.</summary>
public static class HumanFinalRoundSetup
{
    const string HumanFolder = "Assets/Graphics/Characters/Men";
    const string PosePath = "Assets/Scripts/Camera/HumanHandCamPoses.asset";

    [MenuItem("Tools/Dinosaur RPS/Build Human Space Final Round")]
    public static void Build()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/QuarterFinals.unity");
        var manager = Object.FindFirstObjectByType<TournamentGameManager>();
        var settings = new SerializedObject(manager);
        var director = manager.GetComponent<TutorialDirector>();
        if (director != null)
        {
            var rewardSettings = new SerializedObject(director);
            Ref<Image>(settings, "_rpsPaperImage").sprite = Ref<Sprite>(rewardSettings, "_unlockedPaper");
            Object.DestroyImmediate(director);
        }
        foreach (var root in scene.GetRootGameObjects())
            if (root.name == "Quarterfinal Paper Reward") Object.DestroyImmediate(root);

        settings.FindProperty("_currentStage").enumValueIndex = (int)TournamentStage.Finals;
        settings.FindProperty("_opponentLoseTrigger").stringValue = "Lose";
        settings.FindProperty("_opponentDrawTrigger").stringValue = "Draw";
        settings.FindProperty("_handPoseFreezeNormalizedTime").floatValue = .98f;
        Ref<Image>(settings, "_rpsPaperImage").gameObject.SetActive(true);
        ConfigureHuman(scene, settings);
        ConfigurePortraits(scene, settings);
        settings.ApplyModifiedPropertiesWithoutUndo();

        // The Objects root also contains actors/markers, so only remove scenery children.
        var objects = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Objects");
        if (objects != null)
        {
            foreach (var cube in objects.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Cube").ToArray())
                Object.DestroyImmediate(cube.gameObject);
            foreach (Transform child in objects.transform.Cast<Transform>().ToArray())
                if (new[] { "Trees", "Grounds", "Bushes", "Rocks", "FightFloor", "banner" }.Contains(child.name))
                    Object.DestroyImmediate(child.gameObject);
        }
        GroundPlayer(settings);
        foreach (var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            light.enabled = false;
        SpaceArenaAuthoring.Build();
        ConfigureCameras(settings);
        settings.ApplyModifiedPropertiesWithoutUndo();
        HumanSpaceIntroCameraAuthoring.Configure(manager);
        foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            camera.farClipPlane = 500f;
            camera.clearFlags = CameraClearFlags.Skybox;
        }
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/FinalRound.unity");
        AssetDatabase.SaveAssets();
        Debug.Log("HUMAN_SPACE_FINAL_AUTHORED");
    }

    static void ConfigureHuman(Scene scene, SerializedObject settings)
    {
        var old = Ref<Animator>(settings, "_opponentAnimator");
        var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(HumanFolder + "/hm_R.fbx"), scene);
        PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        go.name = "Human";
        go.transform.SetPositionAndRotation(new Vector3(old.transform.position.x, 0, old.transform.position.z), Quaternion.identity);
        Clip("R").SampleAnimation(go, 0);
        HumanAnimationAuthoring.ApplyHandScaleCompensation(go);
        go.transform.localScale = Vector3.one * (2.35f / BoundsOf(go).size.y);
        go.transform.position += Vector3.up * (.025f - BoundsOf(go).min.y);
        var animator = go.GetComponent<Animator>();
        if (animator == null) animator = go.AddComponent<Animator>();
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.runtimeAnimatorController = HumanAnimationAuthoring.Build();
        var skin = BattleMaterial("HumanBattleSkin", "hm_body");
        var leaf = BattleMaterial("HumanBattleLeaf", "hm_leaf");
        foreach (var renderer in go.GetComponentsInChildren<Renderer>())
            renderer.sharedMaterials = renderer.sharedMaterials.Select(material => material.name.ToLowerInvariant().Contains("leaf") ? leaf : skin).ToArray();
        foreach (var renderer in go.GetComponentsInChildren<SkinnedMeshRenderer>()) renderer.updateWhenOffscreen = true;
        settings.FindProperty("_opponentAnimator").objectReferenceValue = animator;

        foreach (var marker in old.GetComponentsInChildren<Transform>(true).Where(t => t.name == "CHand").ToArray())
            marker.SetParent(go.transform, true);
        Object.DestroyImmediate(old.gameObject);

        var poses = AssetDatabase.LoadAssetAtPath<HandCamPoseLibrary>(PosePath);
        if (poses == null)
        {
            poses = Object.Instantiate(Ref<HandCamPoseLibrary>(settings, "_handCamPoses"));
            poses.name = "HumanHandCamPoses";
            AssetDatabase.CreateAsset(poses, PosePath);
        }
        else EditorUtility.CopySerialized(Ref<HandCamPoseLibrary>(settings, "_handCamPoses"), poses);
        poses.name = "HumanHandCamPoses";
        foreach (var pair in new[] { ("R", HandType.Rock), ("P", HandType.Paper), ("S", HandType.Scissors) })
        {
            var clip = Clip(pair.Item1);
            clip.SampleAnimation(go, clip.length * .98f);
            HumanAnimationAuthoring.ApplyHandScaleCompensation(go);
            // The original generic rig uses numbered joints. Select hand vertices by
            // skin weights so framing follows the actual animated mesh, not its rest bounds.
            var mesh = go.GetComponentsInChildren<SkinnedMeshRenderer>().OrderByDescending(r => r.sharedMesh.vertexCount).First();
            var hand = FindGestureHand(mesh);
            var b = HandBounds(mesh, hand);
            // Slightly above the back of the hand keeps all five fingers readable
            // inside the existing circular comparison view.
            Vector3 normal = new Vector3(-1, .2f, -.2f).normalized;
            Vector3 cameraPosition = b.center + normal * 2.5f;
            poses.SetPose(false, pair.Item2, new HandCamPoseLibrary.HandCamPose {
                configured = true, position = cameraPosition,
                eulerAngles = Quaternion.LookRotation(b.center - cameraPosition).eulerAngles,
                fieldOfView = Mathf.Clamp(2 * Mathf.Rad2Deg * Mathf.Atan(Mathf.Max(b.size.y, b.size.z) * .8f / 2.5f), 10, 42)
            });
        }
        Clip("R").SampleAnimation(go, 0);
        HumanAnimationAuthoring.ApplyHandScaleCompensation(go);
        PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
        foreach (var renderer in go.GetComponentsInChildren<Renderer>())
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        EditorUtility.SetDirty(poses);
        settings.FindProperty("_handCamPoses").objectReferenceValue = poses;
    }

    static Transform FindGestureHand(SkinnedMeshRenderer renderer)
    {
        // The source FBX animates this wrist; the hand_r-prefixed arm stays neutral.
        var wrist = renderer.transform.root.Find("joint38/joint39/joint40/joint48/joint49/joint20");
        if (wrist == null) throw new InvalidOperationException("Original human gesture wrist was not found.");
        return wrist;
    }

    static Bounds HandBounds(SkinnedMeshRenderer renderer, Transform wrist)
    {
        var indices = renderer.bones.Select((bone, i) => (bone, i))
            .Where(p => p.bone != null && (p.bone == wrist || p.bone.IsChildOf(wrist))).Select(p => p.i).ToHashSet();
        var vertices = SkinnedWorldVertices(renderer); var weights = renderer.sharedMesh.boneWeights;
        var bounds = new Bounds(wrist.position, Vector3.zero);
        for (int i = 0; i < vertices.Length; i++)
        {
            var w = weights[i];
            float influence = (indices.Contains(w.boneIndex0) ? w.weight0 : 0) + (indices.Contains(w.boneIndex1) ? w.weight1 : 0)
                + (indices.Contains(w.boneIndex2) ? w.weight2 : 0) + (indices.Contains(w.boneIndex3) ? w.weight3 : 0);
            if (influence > .45f) bounds.Encapsulate(vertices[i]);
        }
        if (bounds.size.sqrMagnitude < .0001f)
        {
            foreach (var finger in wrist.GetComponentsInChildren<Transform>()) bounds.Encapsulate(finger.position);
            bounds.Expand(.12f);
        }
        return bounds;
    }

    static void ConfigurePortraits(Scene scene, SerializedObject settings)
    {
        const string path = "Assets/Graphics/UI/PreGame/Human.png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        var factory = new SpriteDataProviderFactories(); factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        var rects = provider.GetSpriteRects().ToList();
        foreach (var entry in new[] { ("HumanIntro", new Rect(0,0,1920,1080)), ("HumanPortrait", new Rect(1160,350,510,610)) })
        {
            var rect = rects.FirstOrDefault(r => r.name == entry.Item1);
            if (rect == null) { rect = new SpriteRect { name = entry.Item1, spriteID = GUID.Generate() }; rects.Add(rect); }
            rect.rect = entry.Item2; rect.pivot = Vector2.one * .5f; rect.alignment = SpriteAlignment.Center;
        }
        provider.SetSpriteRects(rects.ToArray());
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
        provider.Apply();
        importer.SaveAndReimport();
        var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
        var intro = Ref<GameObject>(settings, "_opponentCanvas");
        intro.name = "HumanStart";
        intro.GetComponent<Image>().sprite = sprites.Single(s => s.name == "HumanIntro");
        var portrait = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Image>(true)).Single(i => i.name == "EnemyPortrait");
        portrait.sprite = sprites.Single(s => s.name == "HumanPortrait");
        portrait.preserveAspect = true;
    }

    static void ConfigureCameras(SerializedObject settings)
    {
        var manager = Ref<CameraManager>(settings, "_camController");
        Vector3 player = Ref<Animator>(settings, "_playerAnimator").transform.position;
        Vector3 human = Ref<Animator>(settings, "_opponentAnimator").transform.position;
        Vector3 middle = (player + human) * .5f; middle.y = 0;
        // Remove the forest sign tracking and author simple, readable arena shots.
        foreach (var cam in Object.FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            foreach (var body in cam.GetComponents<CinemachineComponentBase>())
                if (!(body is CinemachineBasicMultiChannelPerlin)) Object.DestroyImmediate(body);
            cam.Follow = null; cam.LookAt = null;
            var lens = cam.Lens; lens.FarClipPlane = 500; cam.Lens = lens;
        }
        Shot(manager.idleCam, middle + new Vector3(-11f, 4, .8f), middle + Vector3.up * 1.1f, 48);
        Shot(manager.sideCam, middle + new Vector3(-12, 5.7f, 0), middle + Vector3.up, 45);
        Shot(manager.frontCam, middle + new Vector3(-9, 3, 0), middle + Vector3.up * 1.1f, 46);
        Shot(manager.introPlayerCam, player + new Vector3(-5, 2.4f, -2.5f), player + Vector3.up * 1.3f, 42);
        Shot(manager.introEnemyCam, human + new Vector3(-4.6f, 2.4f, 2.2f), human + Vector3.up * 1.3f, 40);
        Shot(manager.introZoomOutCam, middle + new Vector3(-12, 6.3f, .7f), middle + Vector3.up * .9f, 44);
        Shot(manager.enemyWinCam, human + new Vector3(-5, 2.5f, 2), human + Vector3.up * 1.2f, 42);
        Shot(manager.playerWinCam, player + new Vector3(-7, 4, -2), player + Vector3.up * 1.8f, 48);
        foreach (var binding in manager.playerWinAnimationCameras ?? Array.Empty<CameraManager.AnimationCameraBinding>())
        {
            if (binding == null) continue;
            Shot(binding.camera, player + new Vector3(-7, 4, -2), player + Vector3.up * 1.8f, 48);
            binding.moveAlongSpline = false;
        }
        manager.enemyWinAnimationCameras = new[] { new CameraManager.AnimationCameraBinding { triggerName = "Win", camera = manager.enemyWinCam, moveAlongSpline = false } };
        manager.resultCamPosition = middle + new Vector3(-9, 3.2f, 0);
        manager.resultCamRotation = Quaternion.LookRotation(middle + Vector3.up * 1.2f - manager.resultCamPosition).eulerAngles;
        EditorUtility.SetDirty(manager);
    }

    static void Shot(CinemachineCamera camera, Vector3 position, Vector3 target, float fov)
    {
        if (camera == null) return;
        camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(target - position));
        var lens = camera.Lens; lens.FieldOfView = fov; camera.Lens = lens;
    }
    static Material BattleMaterial(string name, string sourceName)
    {
        string path = HumanFolder + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(AssetDatabase.LoadAssetAtPath<Material>(HumanFolder + "/" + sourceName + ".mat")) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        material.DisableKeyword("_METALLICSPECGLOSSMAP");
        material.DisableKeyword("_PARALLAXMAP");
        material.SetTexture("_MetallicGlossMap", null);
        material.SetTexture("_ParallaxMap", null);
        material.SetFloat("_Parallax", 0);
        material.SetFloat("_Metallic", 0);
        material.SetFloat("_Smoothness", .24f);
        UnityEditor.Rendering.Universal.ShaderGUI.LitGUI.SetMaterialKeywords(material);
        EditorUtility.SetDirty(material);
        return material;
    }
    static void GroundPlayer(SerializedObject settings)
    {
        var player = Ref<Animator>(settings, "_playerAnimator");
        var pedestal = player.transform.Find("Cube");
        if (pedestal != null) Object.DestroyImmediate(pedestal.gameObject);
        var idle = player.runtimeAnimatorController.animationClips.FirstOrDefault(c => c.name.ToLowerInvariant().Contains("idle"));
        if (idle != null) idle.SampleAnimation(player.gameObject, 0);
        float lowest = float.PositiveInfinity;
        foreach (var renderer in player.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            foreach (var vertex in SkinnedWorldVertices(renderer)) lowest = Mathf.Min(lowest, vertex.y);
        }
        Vector3 offset = Vector3.up * (.025f - lowest);
        player.transform.position += offset;
        settings.FindProperty("_tyrannoWinDanceY").floatValue = player.transform.position.y + 1.3f;
        PrefabUtility.RecordPrefabInstancePropertyModifications(player.transform);
        var poses = Ref<HandCamPoseLibrary>(settings, "_handCamPoses");
        foreach (var hand in new[] { HandType.Rock, HandType.Paper, HandType.Scissors })
        {
            var pose = poses.GetPose(true, hand); pose.position += offset; poses.SetPose(true, hand, pose);
        }
        EditorUtility.SetDirty(poses);
    }
    // Evaluate the original skin in world space. This avoids BakeMesh's inherited
    // import-scale ambiguity on these FBXs (the actor roots are scaled by 11/28).
    public static Vector3[] SkinnedWorldVertices(SkinnedMeshRenderer renderer)
    {
        var mesh = renderer.sharedMesh;
        var source = mesh.vertices; var weights = mesh.boneWeights; var bindposes = mesh.bindposes;
        var matrices = renderer.bones.Select((bone, index) => bone.localToWorldMatrix * bindposes[index]).ToArray();
        var result = new Vector3[source.Length];
        for (int i = 0; i < source.Length; i++)
        {
            var w = weights[i]; var v = source[i];
            result[i] = matrices[w.boneIndex0].MultiplyPoint3x4(v) * w.weight0
                + matrices[w.boneIndex1].MultiplyPoint3x4(v) * w.weight1
                + matrices[w.boneIndex2].MultiplyPoint3x4(v) * w.weight2
                + matrices[w.boneIndex3].MultiplyPoint3x4(v) * w.weight3;
        }
        return result;
    }
    static T Ref<T>(SerializedObject obj, string field) where T : Object => obj.FindProperty(field).objectReferenceValue as T;
    static AnimationClip Clip(string hand) => AssetDatabase.LoadAllAssetsAtPath(HumanFolder + "/hm_" + hand + ".fbx").OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview__"));
    static Bounds BoundsOf(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
        return bounds;
    }
}
#endif
