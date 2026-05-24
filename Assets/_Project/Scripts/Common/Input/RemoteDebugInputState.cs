using UnityEngine;

public class RemoteDebugInputState : MonoBehaviour
{
    [Range(0f, 1f)] public float leftGrip;
    [Range(0f, 1f)] public float rightGrip;
    public bool leftGripButton;
    public bool rightGripButton;
    [Range(0f, 1f)] public float gripPressedThreshold = 0.55f;

    public bool IsLeftGripPressed => leftGripButton || leftGrip > gripPressedThreshold;
    public bool IsRightGripPressed => rightGripButton || rightGrip > gripPressedThreshold;

    public void SetLeftGrip(float value, bool buttonPressed = false)
    {
        leftGrip = Mathf.Clamp01(value);
        leftGripButton = buttonPressed || leftGrip > gripPressedThreshold;
    }

    public void SetRightGrip(float value, bool buttonPressed = false)
    {
        rightGrip = Mathf.Clamp01(value);
        rightGripButton = buttonPressed || rightGrip > gripPressedThreshold;
    }

    public void SetGrip(bool rightHand, float value, bool buttonPressed = false)
    {
        if (rightHand)
        {
            SetRightGrip(value, buttonPressed);
            return;
        }

        SetLeftGrip(value, buttonPressed);
    }

    public void ReleaseAll()
    {
        leftGrip = 0f;
        rightGrip = 0f;
        leftGripButton = false;
        rightGripButton = false;
    }
}
