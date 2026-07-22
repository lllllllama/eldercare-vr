using UnityEngine;

namespace PicoElderCare.Rehab
{
    public class RehabVideoPanelLayoutController : MonoBehaviour
    {
        public Transform panelRoot;
        public Transform videoQuad;
        public Transform headTransform;
        public Transform promptCanvas;
        public Transform trainingAreaRoot;

        public float panelDistance = 2.15f;
        public float videoRightOffset = 0.9f;
        public float heightOffset = 0.08f;
        public float videoWidth = 1.22f;
        public float videoHeight = 0.69f;
        public float videoScale = 1f;
        public float minVideoScale = 0.85f;
        public float maxVideoScale = 1.15f;
        public float videoScaleStep = 0.1f;
        public float minPanelDistance = 0.95f;
        public float maxPanelDistance = 3.25f;
        public float minHeightFromHead = -0.55f;
        public float maxHeightFromHead = 0.45f;
        public bool preferPromptCanvasLayout = false;
        public bool followTrainingAreaRoot = false;
        public bool preserveUserPlacement = true;
        public bool faceUser = true;
        public bool lockManualMoveHeight = true;
        public bool createClosedVideoFrame = true;
        public float frameThickness = 0.012f;
        public float framePadding = 0.035f;
        public Color frameColor = new Color(0.33f, 0.94f, 1f, 0.78f);

        private RehabSessionManager _sessionManager;
        private bool _hasUserPlacement;
        private Material _closedFrameMaterial;
        private Mesh _closedFrameMesh;

        public Vector3 PanelPosition
        {
            get { return panelRoot != null ? panelRoot.position : transform.position; }
        }

        private void Reset()
        {
            ResolveReferences();
            ApplyVideoSize();
        }

        private void Awake()
        {
            ResolveReferences();
            ApplyVideoSize();
        }

        public void PlaceInRightFrontOfUserOnce()
        {
            ResolveReferences();
            ApplyVideoSize();

            if (panelRoot == null) return;

            var headPosition = headTransform != null
                ? headTransform.position
                : new Vector3(0f, 1.5f, 0f);

            if (preserveUserPlacement && _hasUserPlacement)
            {
                if (faceUser)
                {
                    FaceHeadYaw(headPosition);
                }

                return;
            }

            var forward = GetHeadYawForward();
            var right = Vector3.Cross(Vector3.up, forward).normalized;
            if (right.sqrMagnitude < 0.0001f)
            {
                right = Vector3.right;
            }

            var baseCenter = headPosition + forward * Mathf.Max(0.1f, panelDistance);
            if (preferPromptCanvasLayout && promptCanvas != null && !IsPromptCanvasBoundToTrainingArea())
            {
                baseCenter = promptCanvas.position;
            }
            else if (followTrainingAreaRoot && trainingAreaRoot != null)
            {
                var trainingToHead = headPosition - trainingAreaRoot.position;
                trainingToHead.y = 0f;
                if (trainingToHead.sqrMagnitude > 0.0001f)
                {
                    baseCenter = trainingAreaRoot.position - trainingToHead.normalized * 0.2f + Vector3.up * headPosition.y;
                }
            }

            var videoPosition = baseCenter + right * videoRightOffset + Vector3.up * heightOffset;
            panelRoot.position = videoPosition;

            if (faceUser)
            {
                FaceHeadYaw(headPosition);
            }
        }

        public void ResetVideoPlacement()
        {
            _hasUserPlacement = false;
            PlaceInRightFrontOfUserOnce();
        }

        public void ApplyExternalPanelPose(Vector3 position, Quaternion rotation, bool preserveAsUserPlacement)
        {
            ResolveReferences();
            ApplyVideoSize();

            if (panelRoot == null) return;

            panelRoot.position = position;
            panelRoot.rotation = rotation;
            _hasUserPlacement = preserveAsUserPlacement;
        }

        public void MoveVideoToWorldPoint(Vector3 targetPosition, Vector3 headPosition)
        {
            ResolveReferences();
            if (panelRoot == null) return;

            if (lockManualMoveHeight)
            {
                targetPosition.y = panelRoot.position.y;
            }

            var constrained = ConstrainPanelPosition(targetPosition, headPosition);
            panelRoot.position = constrained;
            _hasUserPlacement = true;

            if (faceUser)
            {
                FaceHeadYaw(headPosition);
            }
        }

        public void AdjustVideoScale(float delta)
        {
            SetVideoScale(videoScale + delta);
        }

        public void ScaleVideoUp()
        {
            AdjustVideoScale(Mathf.Abs(videoScaleStep));
        }

        public void ScaleVideoDown()
        {
            AdjustVideoScale(-Mathf.Abs(videoScaleStep));
        }

        public void SetVideoScale(float scale)
        {
            videoScale = Mathf.Clamp(scale, Mathf.Max(0.01f, minVideoScale), Mathf.Max(minVideoScale, maxVideoScale));
            ApplyVideoSize();
        }

        public void ApplyVideoSize()
        {
            videoScale = Mathf.Clamp(videoScale, Mathf.Max(0.01f, minVideoScale), Mathf.Max(minVideoScale, maxVideoScale));
            if (videoQuad != null)
            {
                videoQuad.localScale = new Vector3(videoWidth * videoScale, videoHeight * videoScale, 1f);
            }

            ApplyClosedVideoFrame();
        }

        public void ApplyClosedVideoFrame()
        {
            ResolveReferences();
            if (videoQuad == null || videoQuad.parent == null) return;

            var frameParent = videoQuad.parent;
            var frameRoot = frameParent.Find("VideoClosedFrame");
            if (!createClosedVideoFrame)
            {
                if (frameRoot != null)
                {
                    frameRoot.gameObject.SetActive(false);
                }

                return;
            }

            if (frameRoot == null)
            {
                var frameObject = new GameObject("VideoClosedFrame");
                frameObject.transform.SetParent(frameParent, false);
                frameRoot = frameObject.transform;
            }

            frameRoot.gameObject.SetActive(true);
            frameRoot.localPosition = videoQuad.localPosition;
            frameRoot.localRotation = videoQuad.localRotation;
            frameRoot.localScale = Vector3.one;
            frameRoot.gameObject.layer = videoQuad.gameObject.layer;

            var width = Mathf.Abs(videoQuad.localScale.x);
            var height = Mathf.Abs(videoQuad.localScale.y);
            var thickness = Mathf.Max(0.001f, frameThickness);
            var padding = Mathf.Max(0f, framePadding);
            var horizontalLength = width + 2f * (padding + thickness);
            var verticalLength = height + 2f * padding;
            var horizontalY = height * 0.5f + padding + thickness * 0.5f;
            var verticalX = width * 0.5f + padding + thickness * 0.5f;

            ConfigureFrameBar(frameRoot, "FrameTop", new Vector3(0f, horizontalY, 0f), new Vector3(horizontalLength, thickness, thickness));
            ConfigureFrameBar(frameRoot, "FrameBottom", new Vector3(0f, -horizontalY, 0f), new Vector3(horizontalLength, thickness, thickness));
            ConfigureFrameBar(frameRoot, "FrameLeft", new Vector3(-verticalX, 0f, 0f), new Vector3(thickness, verticalLength, thickness));
            ConfigureFrameBar(frameRoot, "FrameRight", new Vector3(verticalX, 0f, 0f), new Vector3(thickness, verticalLength, thickness));
        }

        private void ConfigureFrameBar(Transform frameRoot, string barName, Vector3 localPosition, Vector3 localScale)
        {
            var bar = frameRoot.Find(barName);
            if (bar == null)
            {
                var barObject = new GameObject(barName, typeof(MeshFilter), typeof(MeshRenderer));
                barObject.transform.SetParent(frameRoot, false);
                bar = barObject.transform;
            }

            bar.localPosition = localPosition;
            bar.localRotation = Quaternion.identity;
            bar.localScale = localScale;
            bar.gameObject.layer = videoQuad.gameObject.layer;

            var colliders = bar.GetComponents<Collider>();
            for (var i = 0; i < colliders.Length; i++)
            {
                if (Application.isPlaying)
                {
                    Destroy(colliders[i]);
                }
                else
                {
                    DestroyImmediate(colliders[i]);
                }
            }

            var meshFilter = bar.GetComponent<MeshFilter>();
            if (meshFilter.sharedMesh == null)
            {
                meshFilter.sharedMesh = GetClosedFrameMesh();
            }

            var meshRenderer = bar.GetComponent<MeshRenderer>();
            meshRenderer.enabled = true;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.sharedMaterial = GetClosedFrameMaterial();
        }

        private Mesh GetClosedFrameMesh()
        {
            if (_closedFrameMesh != null) return _closedFrameMesh;

            _closedFrameMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            if (_closedFrameMesh != null) return _closedFrameMesh;

            _closedFrameMesh = new Mesh
            {
                name = "RehabVideoClosedFrameCube",
                hideFlags = HideFlags.HideAndDontSave,
                vertices = new[]
                {
                    new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f),
                    new Vector3(0.5f, 0.5f, -0.5f), new Vector3(-0.5f, 0.5f, -0.5f),
                    new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, 0.5f),
                    new Vector3(0.5f, 0.5f, 0.5f), new Vector3(-0.5f, 0.5f, 0.5f)
                },
                triangles = new[]
                {
                    0, 2, 1, 0, 3, 2,
                    4, 5, 6, 4, 6, 7,
                    0, 1, 5, 0, 5, 4,
                    2, 3, 7, 2, 7, 6,
                    0, 4, 7, 0, 7, 3,
                    1, 2, 6, 1, 6, 5
                }
            };
            _closedFrameMesh.RecalculateNormals();
            _closedFrameMesh.RecalculateBounds();
            return _closedFrameMesh;
        }

        private Material GetClosedFrameMaterial()
        {
            if (_closedFrameMaterial == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                             Shader.Find("Unlit/Color") ??
                             Shader.Find("Standard");
                if (shader == null) return null;

                _closedFrameMaterial = new Material(shader)
                {
                    name = "RehabVideoClosedFrameMaterial",
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            _closedFrameMaterial.color = frameColor;
            if (_closedFrameMaterial.HasProperty("_BaseColor"))
            {
                _closedFrameMaterial.SetColor("_BaseColor", frameColor);
            }

            return _closedFrameMaterial;
        }

        private Vector3 ConstrainPanelPosition(Vector3 targetPosition, Vector3 headPosition)
        {
            var horizontal = targetPosition - headPosition;
            horizontal.y = 0f;
            if (horizontal.sqrMagnitude < 0.0001f)
            {
                horizontal = GetHeadYawForward() * Mathf.Max(0.1f, panelDistance);
            }

            var distance = Mathf.Clamp(
                horizontal.magnitude,
                Mathf.Max(0.1f, minPanelDistance),
                Mathf.Max(minPanelDistance, maxPanelDistance));
            horizontal = horizontal.normalized * distance;

            targetPosition.x = headPosition.x + horizontal.x;
            targetPosition.z = headPosition.z + horizontal.z;
            targetPosition.y = Mathf.Clamp(
                targetPosition.y,
                headPosition.y + minHeightFromHead,
                headPosition.y + maxHeightFromHead);

            return targetPosition;
        }

        private Vector3 GetHeadYawForward()
        {
            if (headTransform == null)
            {
                return Vector3.forward;
            }

            var forward = Vector3.ProjectOnPlane(headTransform.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }

            return forward.normalized;
        }

        private void FaceHeadYaw(Vector3 headPosition)
        {
            if (panelRoot == null) return;

            var awayFromUser = panelRoot.position - headPosition;
            awayFromUser.y = 0f;
            if (awayFromUser.sqrMagnitude < 0.0001f)
            {
                awayFromUser = GetHeadYawForward();
            }

            panelRoot.rotation = Quaternion.LookRotation(awayFromUser.normalized, Vector3.up);
        }

        private bool IsPromptCanvasBoundToTrainingArea()
        {
            return _sessionManager != null && _sessionManager.placePromptCanvasWithTrainingArea;
        }

        private void ResolveReferences()
        {
            if (panelRoot == null)
            {
                panelRoot = transform;
            }

            if (videoQuad == null)
            {
                var quad = transform.Find("VideoQuad");
                if (quad != null)
                {
                    videoQuad = quad;
                }
            }

            if (headTransform == null)
            {
                var tracker = FindObjectOfType<HandPoseTracker>(true);
                if (tracker != null)
                {
                    headTransform = tracker.hmdTransform;
                }
            }

            if (headTransform == null && Camera.main != null)
            {
                headTransform = Camera.main.transform;
            }

            if (promptCanvas == null)
            {
                var sessionManager = FindObjectOfType<RehabSessionManager>(true);
                if (sessionManager != null)
                {
                    _sessionManager = sessionManager;
                    promptCanvas = sessionManager.promptCanvas;
                    trainingAreaRoot = trainingAreaRoot != null ? trainingAreaRoot : sessionManager.trainingAreaRoot;
                }
            }
            else if (_sessionManager == null)
            {
                _sessionManager = FindObjectOfType<RehabSessionManager>(true);
            }
        }
    }
}
