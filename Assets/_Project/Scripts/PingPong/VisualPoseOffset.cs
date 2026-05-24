using UnityEngine;

[ExecuteAlways]
public class VisualPoseOffset : MonoBehaviour
{
    public Transform visualRoot;
    public Vector3 localPositionOffset;
    public Vector3 localRotationOffsetEuler;
    public Vector3 localScale = Vector3.one;
    public bool applyOnValidate = true;
    public bool applyOnStart = true;

    private bool _applyingPose;

    private void Reset()
    {
        visualRoot = transform;
        localPositionOffset = transform.localPosition;
        localRotationOffsetEuler = transform.localEulerAngles;
        localScale = transform.localScale;
    }

    private void OnValidate()
    {
        if (applyOnValidate)
        {
            ApplyPose();
        }
    }

    private void LateUpdate()
    {
        if (Application.isPlaying || !applyOnValidate || _applyingPose) return;

        var target = visualRoot != null ? visualRoot : transform;
        if (PoseMatchesFields(target)) return;

        CaptureCurrentPose(target);
    }

    private void Start()
    {
        if (applyOnStart)
        {
            ApplyPose();
        }
    }

    [ContextMenu("Apply Pose Offset")]
    public void ApplyPose()
    {
        var target = visualRoot != null ? visualRoot : transform;
        _applyingPose = true;
        target.localPosition = localPositionOffset;
        target.localRotation = Quaternion.Euler(localRotationOffsetEuler);
        target.localScale = localScale;
        _applyingPose = false;
    }

    [ContextMenu("Capture Current Pose")]
    public void CaptureCurrentPose()
    {
        CaptureCurrentPose(visualRoot != null ? visualRoot : transform);
    }

    private void CaptureCurrentPose(Transform target)
    {
        if (target == null) return;

        localPositionOffset = target.localPosition;
        localRotationOffsetEuler = target.localEulerAngles;
        localScale = target.localScale;
    }

    private bool PoseMatchesFields(Transform target)
    {
        if (target == null) return true;

        return Vector3.SqrMagnitude(target.localPosition - localPositionOffset) < 0.000001f &&
               Quaternion.Angle(target.localRotation, Quaternion.Euler(localRotationOffsetEuler)) < 0.01f &&
               Vector3.SqrMagnitude(target.localScale - localScale) < 0.000001f;
    }
}
