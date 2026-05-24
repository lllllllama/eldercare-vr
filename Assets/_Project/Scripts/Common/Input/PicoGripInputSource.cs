using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class PicoGripInputSource : MonoBehaviour, IGripInputSource
{
    public XRNode controllerNode = XRNode.LeftHand;
    [Range(0f, 1f)] public float gripPressedThreshold = 0.55f;

    private readonly List<InputDevice> _devices = new List<InputDevice>();

    public bool IsGripPressed
    {
        get { return ReadGripPressed(out _); }
    }

    public float GripValue
    {
        get
        {
            ReadGripPressed(out var gripValue);
            return gripValue;
        }
    }

    private bool ReadGripPressed(out float gripValue)
    {
        gripValue = 0f;
        InputDevices.GetDevicesAtXRNode(controllerNode, _devices);
        foreach (var device in _devices)
        {
            if (device.TryGetFeatureValue(CommonUsages.grip, out var analogGrip))
            {
                gripValue = Mathf.Max(gripValue, analogGrip);
            }

            if (device.TryGetFeatureValue(CommonUsages.gripButton, out var gripButton) && gripButton)
            {
                gripValue = 1f;
                return true;
            }

            if (gripValue > gripPressedThreshold)
            {
                return true;
            }
        }

        return false;
    }
}
