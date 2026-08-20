#if UNITY_EDITOR
using Core.Enums;
using Managers;
using UnityEditor;
using UnityEngine;

/// <summary>
/// TournamentGameManager 인스펙터 하단에 손 클로즈업 카메라 6종 좌표 "캡처" 버튼을 추가한다.
///
/// 사용법:
///  1) _handCamPoses 에 HandCamPoseLibrary 에셋을 할당한다.
///  2) Play 모드로 들어가 원하는 손모양(가위/바위/보)이 나오는 라운드를 진행한다.
///  3) 손 포즈가 멈춘 순간(돋보기에 손이 보일 때) Unity 상단 Pause 버튼으로 일시정지한다.
///  4) Scene 뷰에서 해당 hand-cam(티라노/닭) 오브젝트를 원하는 구도로 옮기고 FOV를 맞춘다.
///  5) 아래 해당 캐릭터·손모양 버튼을 눌러 현재 위치·회전·FOV를 에셋에 저장한다.
///
/// SO 에셋에 저장하므로 Play 종료 후에도 값이 유지된다.
/// </summary>
[CustomEditor(typeof(TournamentGameManager))]
public class TournamentGameManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("손 클로즈업 카메라 좌표 캡처", EditorStyles.boldLabel);

        var lib = serializedObject.FindProperty("_handCamPoses").objectReferenceValue as HandCamPoseLibrary;
        var tyrannoCam = serializedObject.FindProperty("_tyrannoHandCam").objectReferenceValue as Transform;
        var chickenCam = serializedObject.FindProperty("_chickenHandCam").objectReferenceValue as Transform;

        if (lib == null)
        {
            EditorGUILayout.HelpBox("먼저 _handCamPoses 에 HandCamPoseLibrary 에셋을 할당하세요.\n" +
                                    "(Project 창 우클릭 → Create → DinosaurRPS → Hand Cam Pose Library)",
                                    MessageType.Warning);
            return;
        }

        EditorGUILayout.HelpBox(
            "Play 중 손 포즈가 멈춘 순간 Pause → Scene 뷰에서 hand-cam을 원하는 구도로 맞춘 뒤 " +
            "아래 버튼으로 캡처하세요. 값은 에셋에 저장되어 Play 종료 후에도 유지됩니다.",
            MessageType.Info);

        EditorGUILayout.LabelField("티라노 (왼쪽 돋보기)");
        DrawCaptureRow(lib, tyrannoCam, isTyranno: true);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("닭 (오른쪽 돋보기)");
        DrawCaptureRow(lib, chickenCam, isTyranno: false);
    }

    private void DrawCaptureRow(HandCamPoseLibrary lib, Transform cam, bool isTyranno)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("가위 저장")) Capture(lib, cam, isTyranno, HandType.Scissors);
            if (GUILayout.Button("바위 저장")) Capture(lib, cam, isTyranno, HandType.Rock);
            if (GUILayout.Button("보 저장")) Capture(lib, cam, isTyranno, HandType.Paper);
        }
    }

    private void Capture(HandCamPoseLibrary lib, Transform camTransform, bool isTyranno, HandType hand)
    {
        string who = isTyranno ? "티라노" : "닭";

        if (camTransform == null)
        {
            Debug.LogWarning($"[HandCamCapture] {who} hand-cam Transform이 인스펙터에 비어 있습니다.");
            return;
        }

        // Camera는 hand-cam 오브젝트 자신이 아니라 자식에 붙어 있다.
        var cam = camTransform.GetComponentInChildren<Camera>(true);
        if (cam == null)
            Debug.LogWarning($"[HandCamCapture] {who} hand-cam 자식에서 Camera를 못 찾았습니다. FOV는 30으로 저장됩니다.");

        // 실제로 화면을 그리는 자식 Camera의 월드 위치·회전을 저장한다.
        // (부모 Transform을 저장하면 자식 카메라를 옮겨 잡은 구도가 반영되지 않는다.)
        Transform poseSource = cam != null ? cam.transform : camTransform;

        var pose = new HandCamPoseLibrary.HandCamPose
        {
            configured = true,
            position = poseSource.position,
            eulerAngles = poseSource.rotation.eulerAngles,
            fieldOfView = cam != null ? cam.fieldOfView : 30f
        };

        Undo.RecordObject(lib, "Capture Hand Cam Pose");
        lib.SetPose(isTyranno, hand, pose);
        EditorUtility.SetDirty(lib);
        AssetDatabase.SaveAssets();

        Debug.Log($"[HandCamCapture] {who} {hand} 저장 → pos={pose.position}, " +
                  $"rot={pose.eulerAngles}, fov={pose.fieldOfView:0.0}", lib);
    }
}
#endif
