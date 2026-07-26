using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class PicoGripOrTriggerInputSource : MonoBehaviour, IGripInputSource
{
    public XRNode controllerNode = XRNode.RightHand;
    [Range(0f, 1f)] public float pressedThreshold = 0.45f;

    private readonly List<InputDevice> _devices = new List<InputDevice>();

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
        InputDevices.GetDevicesAtXRNode(controllerNode, _devices);
        foreach (var device in _devices)
        {
            if (device.TryGetFeatureValue(CommonUsages.grip, out var analogGrip))
            {
                value = Mathf.Max(value, analogGrip);
            }

            if (device.TryGetFeatureValue(CommonUsages.trigger, out var analogTrigger))
            {
                value = Mathf.Max(value, analogTrigger);
            }

            if ((device.TryGetFeatureValue(CommonUsages.gripButton, out var gripButton) && gripButton) ||
                (device.TryGetFeatureValue(CommonUsages.triggerButton, out var triggerButton) && triggerButton))
            {
                value = 1f;
                return true;
            }

            if (value > pressedThreshold)
            {
                return true;
            }
        }

        return false;
    }
}
