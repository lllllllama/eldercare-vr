using UnityEngine;

namespace PicoElderCare.Rehab.Tracking.Pico
{
    public sealed class PicoBodyTrackingDebugRenderer : MonoBehaviour
    {
        private struct JointConnection
        {
            public RehabJoint from;
            public RehabJoint to;

            public JointConnection(RehabJoint from, RehabJoint to)
            {
                this.from = from;
                this.to = to;
            }
        }

        private static readonly RehabJoint[] VisibleJoints =
        {
            RehabJoint.Head, RehabJoint.Neck, RehabJoint.Chest, RehabJoint.SpineUpper,
            RehabJoint.SpineLower, RehabJoint.Hips,
            RehabJoint.LeftShoulder, RehabJoint.LeftUpperArm, RehabJoint.LeftElbow,
            RehabJoint.LeftWrist, RehabJoint.LeftHand,
            RehabJoint.RightShoulder, RehabJoint.RightUpperArm, RehabJoint.RightElbow,
            RehabJoint.RightWrist, RehabJoint.RightHand,
            RehabJoint.LeftHip, RehabJoint.LeftKnee, RehabJoint.LeftAnkle, RehabJoint.LeftFoot,
            RehabJoint.RightHip, RehabJoint.RightKnee, RehabJoint.RightAnkle, RehabJoint.RightFoot
        };

        private static readonly JointConnection[] Connections =
        {
            new JointConnection(RehabJoint.Hips, RehabJoint.SpineLower),
            new JointConnection(RehabJoint.SpineLower, RehabJoint.SpineUpper),
            new JointConnection(RehabJoint.SpineUpper, RehabJoint.Chest),
            new JointConnection(RehabJoint.Chest, RehabJoint.Neck),
            new JointConnection(RehabJoint.Neck, RehabJoint.Head),
            new JointConnection(RehabJoint.Chest, RehabJoint.LeftShoulder),
            new JointConnection(RehabJoint.LeftShoulder, RehabJoint.LeftUpperArm),
            new JointConnection(RehabJoint.LeftUpperArm, RehabJoint.LeftElbow),
            new JointConnection(RehabJoint.LeftElbow, RehabJoint.LeftWrist),
            new JointConnection(RehabJoint.LeftWrist, RehabJoint.LeftHand),
            new JointConnection(RehabJoint.Chest, RehabJoint.RightShoulder),
            new JointConnection(RehabJoint.RightShoulder, RehabJoint.RightUpperArm),
            new JointConnection(RehabJoint.RightUpperArm, RehabJoint.RightElbow),
            new JointConnection(RehabJoint.RightElbow, RehabJoint.RightWrist),
            new JointConnection(RehabJoint.RightWrist, RehabJoint.RightHand),
            new JointConnection(RehabJoint.Hips, RehabJoint.LeftHip),
            new JointConnection(RehabJoint.LeftHip, RehabJoint.LeftKnee),
            new JointConnection(RehabJoint.LeftKnee, RehabJoint.LeftAnkle),
            new JointConnection(RehabJoint.LeftAnkle, RehabJoint.LeftFoot),
            new JointConnection(RehabJoint.Hips, RehabJoint.RightHip),
            new JointConnection(RehabJoint.RightHip, RehabJoint.RightKnee),
            new JointConnection(RehabJoint.RightKnee, RehabJoint.RightAnkle),
            new JointConnection(RehabJoint.RightAnkle, RehabJoint.RightFoot)
        };

        [SerializeField] private bool debugSkeletonEnabled;
        [SerializeField] private PicoBodyTrackingProvider provider;
        [SerializeField] private Transform debugRoot;
        [SerializeField] private Material debugMaterial;
        [SerializeField] private float jointDiameterMeters = 0.055f;
        [SerializeField] private float lineWidthMeters = 0.018f;
        [SerializeField] private Color validColor = new Color(0.15f, 1f, 0.45f, 0.9f);
        [SerializeField] private Color limitedColor = new Color(1f, 0.65f, 0.05f, 0.95f);

        private readonly RehabBodySample _sample = new RehabBodySample();
        private readonly Transform[] _jointMarkers = new Transform[(int)RehabJoint.Count];
        private readonly LineRenderer[] _lines = new LineRenderer[Connections.Length];
        private Material _runtimeMaterial;
        private bool _ownsDebugRoot;
        private bool _initialized;

        public bool DebugSkeletonEnabled
        {
            get { return debugSkeletonEnabled; }
            set { debugSkeletonEnabled = value; }
        }

        public PicoBodyTrackingProvider Provider
        {
            get { return provider; }
            set { provider = value; }
        }

        private void Update()
        {
            if (!debugSkeletonEnabled)
            {
                if (debugRoot != null && debugRoot.gameObject.activeSelf)
                {
                    debugRoot.gameObject.SetActive(false);
                }

                return;
            }

            EnsureInitialized();
            if (debugRoot != null && !debugRoot.gameObject.activeSelf)
            {
                debugRoot.gameObject.SetActive(true);
            }

            if (provider == null)
            {
                provider = FindObjectOfType<PicoBodyTrackingProvider>(true);
            }

            _sample.Clear();
            if (provider != null)
            {
                provider.TryGetSample(_sample);
            }

            UpdateJointVisuals();
            UpdateConnectionVisuals();
        }

        private void OnDestroy()
        {
            if (_runtimeMaterial != null)
            {
                DestroyUnityObject(_runtimeMaterial);
            }

            if (_ownsDebugRoot && debugRoot != null)
            {
                DestroyUnityObject(debugRoot.gameObject);
            }
        }

        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            if (debugRoot == null)
            {
                var rootObject = new GameObject("PicoBodyTrackingDebugSkeleton");
                debugRoot = rootObject.transform;
                _ownsDebugRoot = true;
            }

            _runtimeMaterial = debugMaterial != null
                ? new Material(debugMaterial)
                : new Material(Shader.Find("Sprites/Default"));

            for (var i = 0; i < VisibleJoints.Length; i++)
            {
                var joint = VisibleJoints[i];
                var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = joint.ToString();
                marker.transform.SetParent(debugRoot, false);
                marker.transform.localScale = Vector3.one * jointDiameterMeters;
                var collider = marker.GetComponent<Collider>();
                if (collider != null)
                {
                    DestroyUnityObject(collider);
                }

                var renderer = marker.GetComponent<Renderer>();
                renderer.sharedMaterial = _runtimeMaterial;
                marker.SetActive(false);
                _jointMarkers[(int)joint] = marker.transform;
            }

            for (var i = 0; i < Connections.Length; i++)
            {
                var lineObject = new GameObject("BodyJointLine_" + i);
                lineObject.transform.SetParent(debugRoot, false);
                var line = lineObject.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.positionCount = 2;
                line.startWidth = lineWidthMeters;
                line.endWidth = lineWidthMeters;
                line.sharedMaterial = _runtimeMaterial;
                line.enabled = false;
                _lines[i] = line;
            }

            _initialized = true;
        }

        private void UpdateJointVisuals()
        {
            for (var i = 0; i < VisibleJoints.Length; i++)
            {
                var joint = VisibleJoints[i];
                var marker = _jointMarkers[(int)joint];
                var pose = default(RehabJointPose);
                var visible = marker != null &&
                              _sample.TryGetJoint(joint, out pose) &&
                              provider != null;
                if (marker == null)
                {
                    continue;
                }

                marker.gameObject.SetActive(visible);
                if (visible)
                {
                    marker.position = provider.ConvertSamplePositionToWorld(pose.position);
                }
            }

            if (_runtimeMaterial != null)
            {
                _runtimeMaterial.color = _sample.trackingState == RehabTrackingState.Limited
                    ? limitedColor
                    : validColor;
            }
        }

        private void UpdateConnectionVisuals()
        {
            for (var i = 0; i < Connections.Length; i++)
            {
                var from = _jointMarkers[(int)Connections[i].from];
                var to = _jointMarkers[(int)Connections[i].to];
                var visible = from != null && to != null &&
                              from.gameObject.activeSelf && to.gameObject.activeSelf;
                _lines[i].enabled = visible;
                if (visible)
                {
                    _lines[i].SetPosition(0, from.position);
                    _lines[i].SetPosition(1, to.position);
                }
            }
        }

        private static void DestroyUnityObject(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
