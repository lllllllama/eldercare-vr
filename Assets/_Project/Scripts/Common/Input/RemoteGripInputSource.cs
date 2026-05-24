using UnityEngine;
using UnityEngine.XR;

public class RemoteGripInputSource : MonoBehaviour, IGripInputSource
{
    public RemoteDebugInputState inputState;
    public XRNode controllerNode = XRNode.LeftHand;

    public bool IsGripPressed
    {
        get
        {
            ResolveInputState();
            if (inputState == null) return false;
            return IsRightHand ? inputState.IsRightGripPressed : inputState.IsLeftGripPressed;
        }
    }

    public float GripValue
    {
        get
        {
            ResolveInputState();
            if (inputState == null) return 0f;
            return IsRightHand ? inputState.rightGrip : inputState.leftGrip;
        }
    }

    private bool IsRightHand => controllerNode == XRNode.RightHand;

    private void Awake()
    {
        ResolveInputState();
    }

    private void OnEnable()
    {
        ResolveInputState();
    }

    private void ResolveInputState()
    {
        if (inputState == null)
        {
            inputState = FindObjectOfType<RemoteDebugInputState>(true);
        }
    }
}
