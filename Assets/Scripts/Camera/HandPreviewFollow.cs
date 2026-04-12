using UnityEngine;

public class HandPreviewFollow : MonoBehaviour
{
    [SerializeField] private Transform _target;
    [SerializeField] private Vector3 _localFollowOffset;
    [SerializeField] private Vector3 _localLookOffset;
    [SerializeField] private bool _followPosition = true;
    [SerializeField] private bool _followRotation = true;
    [SerializeField] private bool _useTargetRotation = true;
    [SerializeField] private bool _bakeOffsetOnStart = true;

    private void Start()
    {
        if (_bakeOffsetOnStart)
        {
            BakeCurrentOffset();
        }
    }

    private void LateUpdate()
    {
        if (_target == null)
        {
            return;
        }

        if (_followPosition)
        {
            transform.position = GetFollowPosition();
        }

        if (_followRotation)
        {
            Vector3 lookTarget = GetLookTargetPosition();
            Vector3 forward = lookTarget - transform.position;

            if (forward.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
            }
        }
    }

    [ContextMenu("Bake Current Offset")]
    public void BakeCurrentOffset()
    {
        if (_target == null)
        {
            return;
        }

        _localFollowOffset = _useTargetRotation
            ? _target.InverseTransformPoint(transform.position)
            : transform.position - _target.position;
    }

    private Vector3 GetFollowPosition()
    {
        if (_useTargetRotation)
        {
            return _target.TransformPoint(_localFollowOffset);
        }

        return _target.position + _localFollowOffset;
    }

    private Vector3 GetLookTargetPosition()
    {
        if (_useTargetRotation)
        {
            return _target.TransformPoint(_localLookOffset);
        }

        return _target.position + _localLookOffset;
    }
}
