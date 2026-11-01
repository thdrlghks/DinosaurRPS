#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Explicit authoring only: retains the three original human FBX motions and
/// builds small skeletal reactions from their frame-zero pose.
/// </summary>
public static class HumanAnimationAuthoring
{
    private const string Folder = "Assets/Graphics/Characters/Men";
    private const string ControllerPath = Folder + "/Human.controller";
    private static readonly string[] WristPaths =
    {
        "joint38/joint39/joint40/joint48/joint49/joint20",
        "joint38/joint39/joint40/joint50/joint51/hand_r:joint20"
    };

    [MenuItem("Tools/Dinosaur RPS/Build Human Animations")]
    private static void BuildFromMenu() => Build();

    public static AnimatorController Build()
    {
        var originalRock = OriginalClip("hm_R");
        var originalPaper = OriginalClip("hm_P");
        var originalScissors = OriginalClip("hm_S");
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/hm_P.fbx");
        if (model == null) throw new InvalidOperationException("The original hm_P human model is missing.");

        var preview = Object.Instantiate(model);
        preview.name = "Human animation authoring preview";
        preview.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            foreach (var animator in preview.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
            originalPaper.SampleAnimation(preview, 0f);
            ApplyHandScaleCompensation(preview);

            // Keep the original animated positions and rotations. Only convert
            // the source's compensated finger scales into Unity hierarchy scales.
            var rock = SaveClip(CorrectedHandClip(originalRock, preview, "HumanRock"));
            var paper = SaveClip(CorrectedHandClip(originalPaper, preview, "HumanPaper"));
            var scissors = SaveClip(CorrectedHandClip(originalScissors, preview, "HumanScissors"));

            // Verified source hierarchy: pelvis 38 -> spine 39 -> chest 40;
            // chest children are head 41 and upper arms 48 / 50.
            Transform pelvis = Bone(preview.transform, "joint38");
            Transform spine = Child(pelvis, "joint39");
            Transform chest = Child(spine, "joint40");
            Transform head = Child(chest, "joint41");
            Transform armA = Child(chest, "joint48");
            Transform armB = Child(chest, "joint50");
            Vector3 up = (chest.position - pelvis.position).normalized;
            Vector3 across = (armB.position - armA.position).normalized;
            Vector3 forward = Vector3.Cross(across, up).normalized;
            if (up.sqrMagnitude < .5f || across.sqrMagnitude < .5f || forward.sqrMagnitude < .5f)
                throw new InvalidOperationException("Cannot determine the human torso's anatomical axes.");

            var idle = NeutralClip(preview, "HumanIdle", 3f, true);
            Rotate(idle, preview.transform, spine, across, t => .8f * Breath(t));
            Rotate(idle, preview.transform, chest, across, t => .6f * Breath(t));
            Rotate(idle, preview.transform, head, across, t => -.5f * Breath(t));

            var win = NeutralClip(preview, "HumanWin", 2f, false);
            Rotate(win, preview.transform, chest, across, t => -5f * Pulse(t));
            Rotate(win, preview.transform, head, across, t => -4f * Pulse(t));
            RaiseArm(win, preview.transform, armA, Child(armA, "joint49"), up, 34f);
            RaiseArm(win, preview.transform, armB, Child(armB, "joint51"), up, 34f);

            var lose = NeutralClip(preview, "HumanLose", 1.25f, false);
            Rotate(lose, preview.transform, spine, across, t => -9f * Recoil(t));
            Rotate(lose, preview.transform, chest, across, t => -7f * Recoil(t));
            Rotate(lose, preview.transform, head, across, t => 8f * Recoil(t));

            var draw = NeutralClip(preview, "HumanDraw", 1.6f, false);
            Rotate(draw, preview.transform, chest, forward, t => 3f * Mathf.Sin(t * Mathf.PI * 2f) * Pulse(t));
            Rotate(draw, preview.transform, head, up, t => 9f * Mathf.Sin(t * Mathf.PI * 2f) * Pulse(t));

            idle = SaveClip(idle);
            win = SaveClip(win);
            lose = SaveClip(lose);
            draw = SaveClip(draw);
            var controller = BuildController(idle, rock, paper, scissors, win, lose, draw);
            AssetDatabase.SaveAssets();
            Debug.Log("[HumanAnimationAuthoring] Human controller ready. Original RPS takes: " +
                      $"Rock {rock.length:F3}s, Paper {paper.length:F3}s, Scissors {scissors.length:F3}s. " +
                      "Idle and result reactions are derived skeletal clips; no model-root motion was added.");
            return controller;
        }
        finally
        {
            Object.DestroyImmediate(preview);
        }
    }

    /// <summary>
    /// The Maya FBX finger joints use InheritType 2 (segment scale compensation).
    /// Their bind matrices all have world scale .4758800654, including wrist and
    /// fingertips. Unity's imported nested local scales incorrectly compound that
    /// factor. Preserve each wrist scale and give its descendants unit local scale.
    /// </summary>
    public static void ApplyHandScaleCompensation(GameObject human)
    {
        if (human == null) throw new ArgumentNullException(nameof(human));
        foreach (string path in WristPaths)
        {
            Transform wrist = human.transform.Find(path);
            if (wrist == null) throw new InvalidOperationException("Missing original human wrist: " + path);
            foreach (var finger in wrist.GetComponentsInChildren<Transform>(true))
                if (finger != wrist) finger.localScale = Vector3.one;
        }
    }

    private static AnimationClip CorrectedHandClip(AnimationClip original, GameObject sampledModel, string name)
    {
        var clip = Object.Instantiate(original);
        clip.name = name;
        clip.hideFlags = HideFlags.None;
        foreach (string wristPath in WristPaths)
        {
            Transform wrist = sampledModel.transform.Find(wristPath);
            if (wrist == null) throw new InvalidOperationException("Missing original human wrist: " + wristPath);
            foreach (var finger in wrist.GetComponentsInChildren<Transform>(true))
            {
                if (finger == wrist) continue;
                string path = AnimationUtility.CalculateTransformPath(finger, sampledModel.transform);
                // Add explicit curves even where import stripped constant scale:
                // Animator write defaults must never restore the compounded pose.
                Constant(clip, path, "m_LocalScale.x", 1f, original.length);
                Constant(clip, path, "m_LocalScale.y", 1f, original.length);
                Constant(clip, path, "m_LocalScale.z", 1f, original.length);
            }
        }
        return clip;
    }

    private static AnimationClip OriginalClip(string file)
    {
        var clips = AssetDatabase.LoadAllAssetsAtPath(Folder + "/" + file + ".fbx")
            .OfType<AnimationClip>()
            .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal) && clip.length > 0f)
            .ToArray();
        if (clips.Length != 1)
            throw new InvalidOperationException($"Expected one original take in {file}.fbx; found {clips.Length}.");
        return clips[0];
    }

    private static Transform Bone(Transform root, string name)
    {
        var bone = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(item => item.name == name);
        if (bone == null) throw new InvalidOperationException("Missing original human bone: " + name);
        return bone;
    }

    private static Transform Child(Transform parent, string name)
    {
        var child = parent.Find(name);
        if (child == null) throw new InvalidOperationException($"Missing original human bone: {parent.name}/{name}");
        return child;
    }

    private static AnimationClip NeutralClip(GameObject sampledModel, string name, float duration, bool loop)
    {
        var clip = new AnimationClip { name = name, frameRate = 24f };
        foreach (var bone in sampledModel.GetComponentsInChildren<Transform>(true))
        {
            // The placed actor's root is intentionally never animated. Local
            // child transforms preserve the exact sampled model and joint pose.
            if (bone == sampledModel.transform) continue;
            string path = AnimationUtility.CalculateTransformPath(bone, sampledModel.transform);
            Constant(clip, path, "m_LocalPosition.x", bone.localPosition.x, duration);
            Constant(clip, path, "m_LocalPosition.y", bone.localPosition.y, duration);
            Constant(clip, path, "m_LocalPosition.z", bone.localPosition.z, duration);
            Constant(clip, path, "m_LocalRotation.x", bone.localRotation.x, duration);
            Constant(clip, path, "m_LocalRotation.y", bone.localRotation.y, duration);
            Constant(clip, path, "m_LocalRotation.z", bone.localRotation.z, duration);
            Constant(clip, path, "m_LocalRotation.w", bone.localRotation.w, duration);
            Constant(clip, path, "m_LocalScale.x", bone.localScale.x, duration);
            Constant(clip, path, "m_LocalScale.y", bone.localScale.y, duration);
            Constant(clip, path, "m_LocalScale.z", bone.localScale.z, duration);
        }

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        settings.loopBlend = false;
        settings.startTime = 0f;
        settings.stopTime = duration;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        return clip;
    }

    private static void Constant(AnimationClip clip, string path, string property, float value, float duration)
    {
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), property),
            AnimationCurve.Constant(0f, duration, value));
    }

    private static float Breath(float t) => (1f - Mathf.Cos(t * Mathf.PI * 2f)) * .5f;
    private static float Pulse(float t) => Mathf.Pow(Mathf.Sin(t * Mathf.PI), 2f);

    private static float Recoil(float t)
    {
        if (t < .2f) return Mathf.SmoothStep(0f, 1f, t / .2f);
        return 1f - Mathf.SmoothStep(0f, 1f, (t - .2f) / .8f);
    }

    private static void RaiseArm(AnimationClip clip, Transform root, Transform upperArm,
        Transform elbow, Vector3 up, float degrees)
    {
        Vector3 axis = Vector3.Cross((elbow.position - upperArm.position).normalized, up);
        if (axis.sqrMagnitude < .001f) return;
        Rotate(clip, root, upperArm, axis.normalized, t => degrees * Pulse(t));
    }

    private static void Rotate(AnimationClip clip, Transform root, Transform bone, Vector3 worldAxis,
        Func<float, float> angle)
    {
        string path = AnimationUtility.CalculateTransformPath(bone, root);
        Vector3 localAxis = bone.InverseTransformDirection(worldAxis).normalized;
        Quaternion neutral = bone.localRotation;
        var curves = new[] { new AnimationCurve(), new AnimationCurve(), new AnimationCurve(), new AnimationCurve() };
        int steps = Mathf.CeilToInt(clip.length * clip.frameRate);
        for (int sample = 0; sample <= steps; sample++)
        {
            float t = sample / (float)steps;
            Quaternion rotation = neutral * Quaternion.AngleAxis(angle(t), localAxis);
            float time = clip.length * t;
            curves[0].AddKey(time, rotation.x);
            curves[1].AddKey(time, rotation.y);
            curves[2].AddKey(time, rotation.z);
            curves[3].AddKey(time, rotation.w);
        }

        string[] components = { "x", "y", "z", "w" };
        for (int component = 0; component < curves.Length; component++)
        {
            for (int key = 0; key < curves[component].length; key++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curves[component], key, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(curves[component], key, AnimationUtility.TangentMode.ClampedAuto);
            }
            AnimationUtility.SetEditorCurve(clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation." + components[component]),
                curves[component]);
        }
        clip.EnsureQuaternionContinuity();
    }

    private static AnimationClip SaveClip(AnimationClip authored)
    {
        string path = Folder + "/" + authored.name + ".anim";
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (existing == null)
        {
            AssetDatabase.CreateAsset(authored, path);
            return authored;
        }
        EditorUtility.CopySerialized(authored, existing);
        EditorUtility.SetDirty(existing);
        Object.DestroyImmediate(authored);
        return existing;
    }

    private static AnimatorController BuildController(AnimationClip idle, AnimationClip rock,
        AnimationClip paper, AnimationClip scissors, AnimationClip win, AnimationClip lose, AnimationClip draw)
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        if (controller.layers.Length != 1)
            throw new InvalidOperationException("Human.controller must have one authoring layer.");
        var machine = controller.layers[0].stateMachine;
        foreach (var transition in machine.anyStateTransitions) machine.RemoveAnyStateTransition(transition);
        foreach (var transition in machine.entryTransitions) machine.RemoveEntryTransition(transition);
        foreach (var child in machine.states)
            foreach (var transition in child.state.transitions) child.state.RemoveTransition(transition);

        controller.parameters = Array.Empty<AnimatorControllerParameter>();
        // ResetAllTriggers also resets the player's four win triggers on the opponent.
        string[] triggers = { "Rock", "Paper", "Scissors", "Win", "Win2", "Win3", "Win4", "Lose", "Draw" };
        foreach (string trigger in triggers) controller.AddParameter(trigger, AnimatorControllerParameterType.Trigger);

        AnimatorState State(string name, Motion motion, float x, float y)
        {
            var state = machine.states.Select(child => child.state).FirstOrDefault(item => item.name == name);
            if (state == null) state = machine.AddState(name, new Vector3(x, y));
            state.motion = motion;
            state.speed = 1f;
            state.writeDefaultValues = true;
            EditorUtility.SetDirty(state);
            return state;
        }

        var idleState = State("Idle", idle, 280f, 30f);
        machine.defaultState = idleState;
        var rockState = State("Rock", rock, 20f, 140f);
        var paperState = State("Paper", paper, 280f, 140f);
        var scissorsState = State("Scissors", scissors, 540f, 140f);
        var winState = State("Win", win, 20f, 270f);
        var loseState = State("Lose", lose, 280f, 270f);
        var drawState = State("Draw", draw, 540f, 270f);
        Trigger(machine, rockState, "Rock");
        Trigger(machine, paperState, "Paper");
        Trigger(machine, scissorsState, "Scissors");
        foreach (string trigger in new[] { "Win", "Win2", "Win3", "Win4" }) Trigger(machine, winState, trigger);
        Trigger(machine, loseState, "Lose");
        Trigger(machine, drawState, "Draw");

        // RPS states deliberately have no exit: the game manager freezes the
        // gesture at .85/.98, reveals the result, and explicitly resets to Idle.
        foreach (var result in new[] { winState, loseState, drawState })
        {
            var transition = result.AddTransition(idleState);
            transition.hasExitTime = true;
            transition.exitTime = 1f;
            transition.hasFixedDuration = true;
            transition.duration = .16f;
        }
        EditorUtility.SetDirty(machine);
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static void Trigger(AnimatorStateMachine machine, AnimatorState target, string parameter)
    {
        var transition = machine.AddAnyStateTransition(target);
        transition.hasExitTime = false;
        transition.hasFixedDuration = true;
        transition.duration = .12f;
        transition.canTransitionToSelf = false;
        transition.AddCondition(AnimatorConditionMode.If, 0f, parameter);
    }
}
#endif
