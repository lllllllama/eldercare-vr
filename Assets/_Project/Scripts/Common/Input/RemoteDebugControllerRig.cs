using UnityEngine;
using UnityEngine.XR;

[DefaultExecutionOrder(-80)]
public class RemoteDebugControllerRig : MonoBehaviour
{
    public RemoteDebugInputState inputState;
    public Transform hmdTransform;
    public Transform leftControllerTransform;
    public Transform rightControllerTransform;
    public bool createMissingObjects = true;
    public Vector3 defaultHeadPosition = new Vector3(0f, 1.6f, 0f);
    public Vector3 leftControllerLocalPosition = new Vector3(-0.28f, -0.18f, 0.52f);
    public Vector3 rightControllerLocalPosition = new Vector3(0.28f, -0.18f, 0.52f);

    public RemoteGripInputSource LeftGripSource { get; private set; }
    public RemoteGripInputSource RightGripSource { get; private set; }

    private void Awake()
    {
        EnsureRig();
    }

    private void OnEnable()
    {
        EnsureRig();
    }

    public void EnsureRig()
    {
        if (!createMissingObjects) return;

        if (inputState == null)
        {
            inputState = GetComponent<RemoteDebugInputState>();
            if (inputState == null)
            {
                inputState = gameObject.AddComponent<RemoteDebugInputState>();
            }
        }

        hmdTransform = EnsureChildTransform(hmdTransform, "RemoteDebugHMD", defaultHeadPosition, Quaternion.identity);
        leftControllerTransform = EnsureChildTransform(leftControllerTransform, "RemoteDebugLeftController", defaultHeadPosition + leftControllerLocalPosition, Quaternion.identity);
        rightControllerTransform = EnsureChildTransform(rightControllerTransform, "RemoteDebugRightController", defaultHeadPosition + rightControllerLocalPosition, Quaternion.identity);

        LeftGripSource = EnsureGripSource(leftControllerTransform, XRNode.LeftHand);
        RightGripSource = EnsureGripSource(rightControllerTransform, XRNode.RightHand);
    }

    public void SetGrip(bool rightHand, float value, bool buttonPressed = false)
    {
        EnsureRig();
        if (inputState != null)
        {
            inputState.SetGrip(rightHand, value, buttonPressed);
        }
    }

    private Transform EnsureChildTransform(Transform current, string childName, Vector3 worldPosition, Quaternion worldRotation)
    {
        if (current != null) return current;

        var child = transform.Find(childName);
        if (child == null)
        {
            var childObject = new GameObject(childName);
            child = childObject.transform;
            child.SetParent(transform, true);
        }

        child.position = worldPosition;
        child.rotation = worldRotation;
        return child;
    }

    private RemoteGripInputSource EnsureGripSource(Transform controllerTransform, XRNode node)
    {
        if (controllerTransform == null) return null;

        var source = controllerTransform.GetComponent<RemoteGripInputSource>();
        if (source == null)
        {
            source = controllerTransform.gameObject.AddComponent<RemoteGripInputSource>();
        }

        source.inputState = inputState;
        source.controllerNode = node;
        return source;
    }
}
