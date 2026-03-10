using UnityEngine;

public class AnimatorDebugLogger : MonoBehaviour
{
    [SerializeField] private Animator _targetAnimator;
    [SerializeField] private string _label = "Tyranno";

    private AnimatorStateInfo _prevState;
    private bool _initialized = false;

    private void Update()
    {
        if (_targetAnimator == null) return;

        var currentState = _targetAnimator.GetCurrentAnimatorStateInfo(0);

        if (!_initialized)
        {
            _prevState = currentState;
            _initialized = true;
            Debug.LogError($"[{_label}] Initial State: {GetStateName(currentState)}");
            return;
        }

        if (currentState.fullPathHash != _prevState.fullPathHash)
        {
            Debug.LogError($"[{_label}] Animation Changed: {GetStateName(_prevState)} -> {GetStateName(currentState)}");
            _prevState = currentState;
        }
    }

    private string GetStateName(AnimatorStateInfo stateInfo)
    {
        // fullPathHash로는 이름을 직접 알 수 없으므로 hash + normalizedTime 표시
        return $"Hash:{stateInfo.fullPathHash} (length:{stateInfo.length:F2}s, normalized:{stateInfo.normalizedTime:F2})";
    }
}
