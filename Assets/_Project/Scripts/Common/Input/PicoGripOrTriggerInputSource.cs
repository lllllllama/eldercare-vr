using UnityEngine;
using UnityEngine.XR;

public class PicoGripOrTriggerInputSource : MonoBehaviour, IGripInputSource
{
    public XRNode controllerNode = XRNode.RightHand;
    [Range(0f, 1f)] public float pressThreshold = 0.55f;
    [Range(0f, 1f)] public float releaseThreshold = 0.35f;

    private InputDevice _device;
    private XRNode _deviceNode;
    private bool _wasPressed;

    public bool IsGripPressed
    {
        get { return ReadPressed(out _); }
    }

    public float GripValue
    {
        get
        {
            ReadPressed(out var value);
            return value;
        }
    }

    private bool ReadPressed(out float value)
    {
        value = 0f;
        if (!TryResolveDevice())
        {
            _wasPressed = false;
            return false;
        }

        if (_device.TryGetFeatureValue(CommonUsages.grip, out var analogGrip))
        {
            value = Mathf.Max(value, analogGrip);
        }

        if (_device.TryGetFeatureValue(CommonUsages.trigger, out var analogTrigger))
        {
            value = Mathf.Max(value, analogTrigger);
        }

        if ((_device.TryGetFeatureValue(CommonUsages.gripButton, out var gripButton) && gripButton) ||
            (_device.TryGetFeatureValue(CommonUsages.triggerButton, out var triggerButton) && triggerButton))
        {
            value = 1f;
            _wasPressed = true;
            return true;
        }

        // 双阈值迟滞：按下用高阈值、松开用低阈值，避免模拟量在单一阈值附近
        // 抖动一帧就被判成“松手”，导致老年玩家半路提前放箭。
        var threshold = _wasPressed ? releaseThreshold : pressThreshold;
        _wasPressed = value > threshold;
        return _wasPressed;
    }

    private bool TryResolveDevice()
    {
        if (_device.isValid && _deviceNode == controllerNode) return true;

        _device = InputDevices.GetDeviceAtXRNode(controllerNode);
        _deviceNode = controllerNode;
        return _device.isValid;
    }
}
