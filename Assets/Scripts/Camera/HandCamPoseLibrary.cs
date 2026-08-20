using Core.Enums;
using UnityEngine;

/// <summary>
/// 손 클로즈업(돋보기) 카메라의 6개 좌표 세트를 담는 에셋.
/// 티라노(가위/바위/보) + 닭(가위/바위/보) 각각의 위치 · 회전 · FOV(확대율)를 저장한다.
///
/// 씬 컴포넌트가 아니라 ScriptableObject 에셋에 저장하는 이유:
/// Play 모드에서 실제 게임 화면을 보며 캡처한 값이 Play 종료 후에도 유지돼야 하기 때문.
/// (씬 오브젝트의 필드는 Play 종료 시 되돌아가지만, 에셋은 유지된다.)
/// </summary>
[CreateAssetMenu(fileName = "HandCamPoseLibrary", menuName = "DinosaurRPS/Hand Cam Pose Library")]
public class HandCamPoseLibrary : ScriptableObject
{
    [System.Serializable]
    public struct HandCamPose
    {
        [Tooltip("캡처 버튼으로 값이 저장됐는지 여부. false면 런타임에서 폴백 좌표를 쓴다.")]
        public bool configured;
        public Vector3 position;
        public Vector3 eulerAngles;
        [Range(1f, 120f)] public float fieldOfView;

        public static HandCamPose Default => new HandCamPose
        {
            configured = false,
            position = Vector3.zero,
            eulerAngles = new Vector3(0f, 90f, 0f),
            fieldOfView = 30f
        };
    }

    [Header("티라노 (Player, 왼쪽 돋보기)")]
    public HandCamPose tyrannoRock = HandCamPose.Default;
    public HandCamPose tyrannoPaper = HandCamPose.Default;
    public HandCamPose tyrannoScissors = HandCamPose.Default;

    [Header("닭 (Opponent, 오른쪽 돋보기)")]
    public HandCamPose chickenRock = HandCamPose.Default;
    public HandCamPose chickenPaper = HandCamPose.Default;
    public HandCamPose chickenScissors = HandCamPose.Default;

    /// <summary>
    /// 해당 손모양의 pose를 반환하되, 아직 캡처(configured)되지 않았으면 false.
    /// 미설정 상태에서 원점(0,0,0)으로 카메라가 튀는 것을 막기 위해 런타임은 이걸 쓴다.
    /// </summary>
    public bool TryGetPose(bool isTyranno, HandType hand, out HandCamPose pose)
    {
        pose = GetPose(isTyranno, hand);
        return pose.configured;
    }

    public HandCamPose GetPose(bool isTyranno, HandType hand)
    {
        if (isTyranno)
        {
            return hand switch
            {
                HandType.Rock => tyrannoRock,
                HandType.Paper => tyrannoPaper,
                HandType.Scissors => tyrannoScissors,
                _ => tyrannoRock
            };
        }

        return hand switch
        {
            HandType.Rock => chickenRock,
            HandType.Paper => chickenPaper,
            HandType.Scissors => chickenScissors,
            _ => chickenRock
        };
    }

#if UNITY_EDITOR
    /// <summary>에디터 캡처 버튼 전용. 런타임에서는 호출하지 않는다.</summary>
    public void SetPose(bool isTyranno, HandType hand, HandCamPose pose)
    {
        if (isTyranno)
        {
            switch (hand)
            {
                case HandType.Rock: tyrannoRock = pose; break;
                case HandType.Paper: tyrannoPaper = pose; break;
                case HandType.Scissors: tyrannoScissors = pose; break;
            }
        }
        else
        {
            switch (hand)
            {
                case HandType.Rock: chickenRock = pose; break;
                case HandType.Paper: chickenPaper = pose; break;
                case HandType.Scissors: chickenScissors = pose; break;
            }
        }
    }
#endif
}
