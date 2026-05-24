using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class VisualHandGripAnimator : MonoBehaviour
{
    public XRNode controllerNode = XRNode.LeftHand;
    public Transform handVisual;
    public bool autoFindFingerBones = true;
    public float curlDegrees = 35f;
    public float closedPoseSpeed = 12f;
    public Vector3 closedLocalPositionOffset = Vector3.zero;
    public Vector3 closedLocalRotationOffsetEuler = new Vector3(-8f, 0f, 0f);
    public Vector3 closedLocalScale = Vector3.one * 0.98f;

    private readonly List<InputDevice> _devices = new List<InputDevice>();
    private readonly List<FingerBonePose> _fingerBones = new List<FingerBonePose>();

    private Transform _cachedHandVisual;
    private Vector3 _openLocalPosition;
    private Quaternion _openLocalRotation;
    private Vector3 _openLocalScale;
    private bool _poseCached;
    private float _gripBlend;

    private struct FingerBonePose
    {
        public Transform transform;
        public Quaternion openLocalRotation;
        public Vector3 curlEulerMultiplier;
    }

    private void OnEnable()
    {
        RebuildPoseCache();
        ApplyPose(0f);
    }

    private void OnDisable()
    {
        ApplyPose(0f);
        _gripBlend = 0f;
    }

    private void Update()
    {
        if (_cachedHandVisual != ResolveHandVisual())
        {
            RebuildPoseCache();
        }

        var targetGrip = ReadGripValue();
        _gripBlend = Mathf.MoveTowards(_gripBlend, targetGrip, Mathf.Max(0.01f, closedPoseSpeed) * Time.deltaTime);
    }

    private void LateUpdate()
    {
        if (!_poseCached)
        {
            RebuildPoseCache();
        }

        ApplyPose(_gripBlend);
    }

    public void RebuildPoseCache()
    {
        _fingerBones.Clear();
        _poseCached = false;

        var visual = ResolveHandVisual();
        if (visual == null) return;

        _cachedHandVisual = visual;
        _openLocalPosition = visual.localPosition;
        _openLocalRotation = visual.localRotation;
        _openLocalScale = visual.localScale;

        if (autoFindFingerBones)
        {
            foreach (var child in visual.GetComponentsInChildren<Transform>(true))
            {
                if (child == null || child == visual) continue;
                if (!TryGetFingerCurlMultiplier(child.name, out var curlEulerMultiplier)) continue;

                _fingerBones.Add(new FingerBonePose
                {
                    transform = child,
                    openLocalRotation = child.localRotation,
                    curlEulerMultiplier = curlEulerMultiplier
                });
            }
        }

        _poseCached = true;
    }

    private Transform ResolveHandVisual()
    {
        if (handVisual != null) return handVisual;

        var namedVisual = transform.Find("HandVisual");
        if (namedVisual != null)
        {
            handVisual = namedVisual;
        }

        return handVisual;
    }

    private static bool TryGetFingerCurlMultiplier(string transformName, out Vector3 curlEulerMultiplier)
    {
        curlEulerMultiplier = Vector3.zero;
        if (string.IsNullOrEmpty(transformName)) return false;

        var lowerName = transformName.ToLowerInvariant();
        if (lowerName.Contains("thumb"))
        {
            curlEulerMultiplier = new Vector3(0.65f, -0.2f, -0.55f);
            return true;
        }

        if (lowerName.Contains("index") ||
            lowerName.Contains("middle") ||
            lowerName.Contains("ring") ||
            lowerName.Contains("pinky") ||
            lowerName.Contains("little") ||
            lowerName.Contains("finger"))
        {
            curlEulerMultiplier = new Vector3(1f, 0f, 0f);
            return true;
        }

        return false;
    }

    private void ApplyPose(float blend)
    {
        if (!_poseCached || _cachedHandVisual == null) return;

        var hasFingerBones = autoFindFingerBones && _fingerBones.Count > 0;
        var visualBlend = hasFingerBones ? blend * 0.2f : blend;
        var closedRotationOffset = Quaternion.Euler(closedLocalRotationOffsetEuler);

        _cachedHandVisual.localPosition = _openLocalPosition + closedLocalPositionOffset * visualBlend;
        _cachedHandVisual.localRotation = _openLocalRotation * Quaternion.Slerp(Quaternion.identity, closedRotationOffset, visualBlend);
        _cachedHandVisual.localScale = Vector3.Lerp(_openLocalScale, Vector3.Scale(_openLocalScale, closedLocalScale), visualBlend);

        if (!hasFingerBones) return;

        var curlAmount = curlDegrees * blend;
        for (var i = 0; i < _fingerBones.Count; i++)
        {
            var finger = _fingerBones[i];
            if (finger.transform == null) continue;

            var curl = Quaternion.Euler(finger.curlEulerMultiplier * curlAmount);
            finger.transform.localRotation = finger.openLocalRotation * curl;
        }
    }

    private float ReadGripValue()
    {
        _devices.Clear();
        InputDevices.GetDevicesAtXRNode(controllerNode, _devices);
        var strongestGrip = 0f;

        foreach (var device in _devices)
        {
            if (device.TryGetFeatureValue(CommonUsages.grip, out var gripValue))
            {
                strongestGrip = Mathf.Max(strongestGrip, gripValue);
            }

            if (device.TryGetFeatureValue(CommonUsages.gripButton, out var gripButton) && gripButton)
            {
                strongestGrip = 1f;
            }
        }

        return Mathf.Clamp01(strongestGrip);
    }
}
