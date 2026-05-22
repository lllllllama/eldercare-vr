using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace PicoElderCare.Rehab
{
    public enum RehabSpatialRayTarget
    {
        TrainingArea,
        VideoPanel
    }

    public class RehabSpatialRayControl : MonoBehaviour
    {
        public RehabSessionManager sessionManager;
        public RehabVideoPanelLayoutController videoLayoutController;
        public Transform leftControllerTransform;
        public Transform rightControllerTransform;
        public Transform hmdTransform;
        public XRNode leftControllerNode = XRNode.LeftHand;
        public XRNode rightControllerNode = XRNode.RightHand;
        public RehabSpatialRayTarget activeTarget = RehabSpatialRayTarget.VideoPanel;
        public bool enableVideoPanelControl = true;
        public bool enableTrainingAreaControl = true;
        public bool enableDirectVideoRayDrag = true;
        public bool createVideoRayCollider = true;
        public bool autoCreateControlCanvas = true;
        public GameObject controlCanvasRoot;
        public TMP_Text statusText;
        public LineRenderer rayPreview;
        public float floorY = 0f;
        public float maxRayDistanceMeters = 4.5f;
        public float videoMovePlaneDistanceMeters = 1.8f;
        public float triggerThreshold = 0.65f;
        public float videoColliderDepthMeters = 0.05f;

        private readonly List<InputDevice> _devices = new List<InputDevice>();
        private bool _placementArmed;
        private bool _waitForTriggerRelease;
        private bool _directVideoDragging;
        private Plane _directVideoDragPlane;
        private Vector3 _directVideoDragOffset;
        private Collider _videoRayCollider;

        public bool PlacementArmed
        {
            get { return _placementArmed; }
        }

        public bool DirectVideoDragActive
        {
            get { return _directVideoDragging; }
        }

        public static RehabSpatialRayControl EnsureRuntime(
            RehabSessionManager session,
            RehabVideoPanelLayoutController videoLayout)
        {
            var controller = FindObjectOfType<RehabSpatialRayControl>(true);
            if (controller == null)
            {
                var host = new GameObject("RehabSpatialRayControl");
                if (session != null)
                {
                    host.transform.SetParent(session.transform, false);
                }

                controller = host.AddComponent<RehabSpatialRayControl>();
            }

            if (session != null)
            {
                controller.sessionManager = session;
            }

            if (videoLayout != null)
            {
                controller.videoLayoutController = videoLayout;
            }

            controller.ResolveReferences();
            controller.EnsureControlCanvas();
            controller.EnsureVideoRayTarget();
            return controller;
        }

        private void Awake()
        {
            ResolveReferences();
            EnsureControlCanvas();
            EnsureVideoRayTarget();
        }

        private void Start()
        {
            ResolveReferences();
            EnsureVideoRayTarget();
            if (videoLayoutController != null)
            {
                videoLayoutController.PlaceInRightFrontOfUserOnce();
            }

            RefreshStatus();
        }

        private void Update()
        {
            if (sessionManager == null || videoLayoutController == null ||
                leftControllerTransform == null || rightControllerTransform == null || hmdTransform == null)
            {
                ResolveReferences();
            }

            EnsureVideoRayTarget();

            var triggerPressed = TryGetPressedControllerRay(out var ray);
            if (_waitForTriggerRelease)
            {
                if (!triggerPressed)
                {
                    _waitForTriggerRelease = false;
                }

                SetRayPreviewVisible(false, ray);
                return;
            }

            if (_directVideoDragging)
            {
                if (!triggerPressed)
                {
                    EndDirectVideoPanelDrag();
                    SetRayPreviewVisible(false, ray);
                    return;
                }

                var directMoved = UpdateDirectVideoPanelDrag(ray);
                SetRayPreviewVisible(directMoved, ray);
                return;
            }

            if (triggerPressed && TryBeginDirectVideoPanelDrag(ray))
            {
                SetRayPreviewVisible(true, ray);
                return;
            }

            if (!_placementArmed || !triggerPressed)
            {
                SetRayPreviewVisible(false, ray);
                return;
            }

            var moved = activeTarget == RehabSpatialRayTarget.TrainingArea &&
                        TryPlaceTrainingAreaFromRay(ray);
            SetRayPreviewVisible(moved, ray);
        }

        public void SelectVideoTarget()
        {
            activeTarget = RehabSpatialRayTarget.VideoPanel;
            _placementArmed = false;
            _waitForTriggerRelease = false;
            RefreshStatus();
        }

        public void SelectTrainingAreaTarget()
        {
            activeTarget = RehabSpatialRayTarget.TrainingArea;
            _placementArmed = true;
            _waitForTriggerRelease = true;
            RefreshStatus();
        }

        public void ScaleVideoUp()
        {
            if (videoLayoutController == null) ResolveReferences();
            if (videoLayoutController != null)
            {
                videoLayoutController.ScaleVideoUp();
            }

            activeTarget = RehabSpatialRayTarget.VideoPanel;
            RefreshStatus();
        }

        public void ScaleVideoDown()
        {
            if (videoLayoutController == null) ResolveReferences();
            if (videoLayoutController != null)
            {
                videoLayoutController.ScaleVideoDown();
            }

            activeTarget = RehabSpatialRayTarget.VideoPanel;
            RefreshStatus();
        }

        public void ResetVideoPlacement()
        {
            if (videoLayoutController == null) ResolveReferences();
            if (videoLayoutController != null)
            {
                videoLayoutController.ResetVideoPlacement();
            }

            activeTarget = RehabSpatialRayTarget.VideoPanel;
            _placementArmed = false;
            RefreshStatus();
        }

        public void ResetTrainingAreaPlacement()
        {
            if (sessionManager == null) ResolveReferences();
            if (sessionManager != null)
            {
                sessionManager.RecenterTrainingArea();
            }

            activeTarget = RehabSpatialRayTarget.TrainingArea;
            _placementArmed = false;
            RefreshStatus();
        }

        public Collider EnsureVideoRayTarget()
        {
            if (!createVideoRayCollider) return null;
            if (videoLayoutController == null) ResolveReferences();
            if (videoLayoutController == null || videoLayoutController.videoQuad == null) return null;

            var target = videoLayoutController.videoQuad.gameObject;
            var box = target.GetComponent<BoxCollider>();
            if (box == null)
            {
                box = target.AddComponent<BoxCollider>();
            }

            box.isTrigger = true;
            box.center = Vector3.zero;
            box.size = new Vector3(1.04f, 1.04f, Mathf.Max(0.005f, videoColliderDepthMeters));
            _videoRayCollider = box;
            return _videoRayCollider;
        }

        public bool TryBeginDirectVideoPanelDrag(Ray ray)
        {
            if (!enableVideoPanelControl || !enableDirectVideoRayDrag) return false;
            if (videoLayoutController == null) ResolveReferences();
            if (videoLayoutController == null || videoLayoutController.panelRoot == null) return false;

            EnsureVideoRayTarget();
            if (!TryHitVideoPanel(ray, out var hitPoint)) return false;

            _placementArmed = false;
            activeTarget = RehabSpatialRayTarget.VideoPanel;
            var normal = GetVideoDragPlaneNormal();
            _directVideoDragPlane = new Plane(normal, videoLayoutController.PanelPosition);
            _directVideoDragOffset = videoLayoutController.PanelPosition - hitPoint;
            _directVideoDragging = true;
            RefreshStatus();
            return true;
        }

        public bool UpdateDirectVideoPanelDrag(Ray ray)
        {
            if (!_directVideoDragging || videoLayoutController == null) return false;
            if (!_directVideoDragPlane.Raycast(ray, out var distance)) return false;
            if (distance < 0f || distance > Mathf.Max(maxRayDistanceMeters, 0.1f)) return false;

            MoveVideoPanelToWorldPoint(ray.GetPoint(distance) + _directVideoDragOffset);
            return true;
        }

        public void EndDirectVideoPanelDrag()
        {
            _directVideoDragging = false;
            RefreshStatus();
        }

        public bool TryPlaceTrainingAreaFromRay(Ray ray)
        {
            if (!enableTrainingAreaControl) return false;

            var floorPlane = new Plane(Vector3.up, new Vector3(0f, floorY, 0f));
            if (!floorPlane.Raycast(ray, out var distance)) return false;
            if (distance < 0f || distance > maxRayDistanceMeters) return false;

            PlaceTrainingAreaAtFloorPoint(ray.GetPoint(distance));
            return true;
        }

        public void PlaceTrainingAreaAtFloorPoint(Vector3 floorPoint)
        {
            var center = new Vector3(floorPoint.x, floorY, floorPoint.z);
            var headPosition = GetHeadPosition(center - Vector3.forward);
            var forward = center - headPosition;
            forward.y = 0f;

            if (sessionManager != null)
            {
                sessionManager.SetTrainingAreaCenter(center, forward, headPosition);
                return;
            }

            var trainingArea = GameObject.Find("TrainingArea");
            if (trainingArea != null)
            {
                trainingArea.transform.position = center;
            }
        }

        public bool TryMoveVideoPanelFromRay(Ray ray)
        {
            if (!enableVideoPanelControl) return false;
            if (videoLayoutController == null) ResolveReferences();
            if (videoLayoutController == null) return false;

            var headPosition = GetHeadPosition(Vector3.zero);
            var forward = GetHeadYawForward();
            var panelPosition = videoLayoutController.PanelPosition;
            if (panelPosition.sqrMagnitude < 0.0001f)
            {
                panelPosition = headPosition + forward * Mathf.Max(0.1f, videoMovePlaneDistanceMeters);
            }

            var movePlane = new Plane(-forward, panelPosition);
            if (!movePlane.Raycast(ray, out var distance)) return false;
            if (distance < 0f || distance > maxRayDistanceMeters) return false;

            MoveVideoPanelToWorldPoint(ray.GetPoint(distance));
            return true;
        }

        public void MoveVideoPanelToWorldPoint(Vector3 worldPoint)
        {
            if (videoLayoutController == null) ResolveReferences();
            if (videoLayoutController == null) return;

            videoLayoutController.MoveVideoToWorldPoint(worldPoint, GetHeadPosition(Vector3.zero));
        }

        public void ResolveReferences()
        {
            if (sessionManager == null)
            {
                sessionManager = FindObjectOfType<RehabSessionManager>(true);
            }

            if (videoLayoutController == null)
            {
                videoLayoutController = FindObjectOfType<RehabVideoPanelLayoutController>(true);
            }

            if (sessionManager != null)
            {
                floorY = sessionManager.trainingFloorY;
            }

            if (leftControllerTransform == null)
            {
                leftControllerTransform = FindTransformByName("Left Controller");
            }

            if (rightControllerTransform == null)
            {
                rightControllerTransform = FindTransformByName("Right Controller");
            }

            if (hmdTransform == null)
            {
                var tracker = FindObjectOfType<HandPoseTracker>(true);
                if (tracker != null)
                {
                    hmdTransform = tracker.hmdTransform;
                }
            }

            if (hmdTransform == null && Camera.main != null)
            {
                hmdTransform = Camera.main.transform;
            }
        }

        public void EnsureControlCanvas()
        {
            if (!autoCreateControlCanvas)
            {
                return;
            }

            if (controlCanvasRoot != null)
            {
                RemoveLegacyVideoMoveButton(controlCanvasRoot.transform);
                RefreshStatus();
                return;
            }

            if (videoLayoutController == null)
            {
                ResolveReferences();
            }

            var parent = videoLayoutController != null && videoLayoutController.panelRoot != null
                ? videoLayoutController.panelRoot
                : transform;
            var existing = parent.Find("RehabSpatialControls");
            if (existing != null)
            {
                controlCanvasRoot = existing.gameObject;
                var status = controlCanvasRoot.transform.Find("Status");
                statusText = status != null
                    ? status.GetComponent<TMP_Text>()
                    : controlCanvasRoot.GetComponentInChildren<TMP_Text>(true);
                RemoveLegacyVideoMoveButton(controlCanvasRoot.transform);
                RefreshStatus();
                return;
            }

            controlCanvasRoot = new GameObject(
                "RehabSpatialControls",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(TrackedDeviceGraphicRaycaster));
            controlCanvasRoot.transform.SetParent(parent, false);
            controlCanvasRoot.transform.localPosition = new Vector3(0f, -0.37f, 0.02f);
            controlCanvasRoot.transform.localRotation = Quaternion.identity;
            controlCanvasRoot.transform.localScale = Vector3.one * 0.0015f;

            var rect = controlCanvasRoot.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(700f, 118f);

            var canvas = controlCanvasRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;

            var scaler = controlCanvasRoot.GetComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 12f;

            var background = controlCanvasRoot.AddComponent<Image>();
            background.color = new Color(0.012f, 0.048f, 0.062f, 0.74f);
            var outline = controlCanvasRoot.AddComponent<Outline>();
            outline.effectColor = new Color(0.33f, 0.94f, 1f, 0.28f);
            outline.effectDistance = new Vector2(2f, -2f);

            CreateDivider(controlCanvasRoot.transform, "TopTrace", new Vector2(0f, 53f), new Vector2(640f, 3f), new Color(0.42f, 0.96f, 1f, 0.5f));
            CreateDivider(controlCanvasRoot.transform, "BottomTrace", new Vector2(0f, -53f), new Vector2(420f, 2f), new Color(0.42f, 0.96f, 1f, 0.24f));

            CreateControlButton(controlCanvasRoot.transform, "MoveSpaceButton", "\u7a7a\u95f4\u4f4d\u7f6e", new Vector2(-228f, 10f), new Vector2(150f, 52f), SelectTrainingAreaTarget);
            CreateControlButton(controlCanvasRoot.transform, "VideoScaleDownButton", "\u89c6\u9891 -", new Vector2(-64f, 10f), new Vector2(132f, 52f), ScaleVideoDown);
            CreateControlButton(controlCanvasRoot.transform, "VideoScaleUpButton", "\u89c6\u9891 +", new Vector2(84f, 10f), new Vector2(132f, 52f), ScaleVideoUp);
            CreateControlButton(controlCanvasRoot.transform, "ResetButton", "\u91cd\u7f6e\u663e\u793a", new Vector2(246f, 10f), new Vector2(150f, 52f), ResetVideoPlacement);

            statusText = CreateText(controlCanvasRoot.transform, "Status", "\u6309\u4f4f\u89c6\u9891\u753b\u9762\u62d6\u52a8\uff0c\u677e\u5f00\u540e\u56fa\u5b9a\uff1b\u7a7a\u95f4\u5708\u9700\u70b9\u51fb\u6309\u94ae\u540e\u79fb\u52a8", 20f, TextAlignmentOptions.Center, new Vector2(0f, -38f), new Vector2(640f, 34f));
            statusText.color = new Color(0.74f, 0.98f, 1f, 0.78f);
        }

        private bool TryHitVideoPanel(Ray ray, out Vector3 hitPoint)
        {
            hitPoint = Vector3.zero;

            if (Physics.Raycast(ray, out var hit, Mathf.Max(0.1f, maxRayDistanceMeters), ~0, QueryTriggerInteraction.Collide) &&
                IsVideoPanelCollider(hit.collider))
            {
                hitPoint = hit.point;
                return true;
            }

            if (videoLayoutController == null || videoLayoutController.videoQuad == null) return false;

            var plane = new Plane(GetVideoDragPlaneNormal(), videoLayoutController.videoQuad.position);
            if (!plane.Raycast(ray, out var distance)) return false;
            if (distance < 0f || distance > Mathf.Max(0.1f, maxRayDistanceMeters)) return false;

            var point = ray.GetPoint(distance);
            if (!IsPointWithinVideoQuad(point))
            {
                return false;
            }

            hitPoint = point;
            return true;
        }

        private bool IsVideoPanelCollider(Collider candidate)
        {
            if (candidate == null || videoLayoutController == null || videoLayoutController.panelRoot == null)
            {
                return false;
            }

            if (_videoRayCollider != null && candidate == _videoRayCollider)
            {
                return true;
            }

            var candidateTransform = candidate.transform;
            return candidateTransform == videoLayoutController.videoQuad ||
                   candidateTransform.IsChildOf(videoLayoutController.videoQuad) ||
                   candidateTransform == videoLayoutController.panelRoot ||
                   candidateTransform.IsChildOf(videoLayoutController.panelRoot);
        }

        private bool IsPointWithinVideoQuad(Vector3 worldPoint)
        {
            if (videoLayoutController == null || videoLayoutController.videoQuad == null) return false;

            var local = videoLayoutController.videoQuad.InverseTransformPoint(worldPoint);
            return Mathf.Abs(local.x) <= 0.55f && Mathf.Abs(local.y) <= 0.55f;
        }

        private Vector3 GetVideoDragPlaneNormal()
        {
            if (videoLayoutController != null && videoLayoutController.panelRoot != null)
            {
                var forward = Vector3.ProjectOnPlane(videoLayoutController.panelRoot.forward, Vector3.up);
                if (forward.sqrMagnitude > 0.0001f)
                {
                    return forward.normalized;
                }
            }

            return -GetHeadYawForward();
        }

        private void RemoveLegacyVideoMoveButton(Transform root)
        {
            if (root == null) return;

            var legacy = root.Find("MoveVideoButton");
            if (legacy == null) return;

            if (Application.isPlaying)
            {
                Destroy(legacy.gameObject);
            }
            else
            {
                DestroyImmediate(legacy.gameObject);
            }
        }

        private bool TryGetPressedControllerRay(out Ray ray)
        {
            ray = new Ray(Vector3.zero, Vector3.forward);
            if (IsTriggerPressed(leftControllerNode) && leftControllerTransform != null)
            {
                ray = new Ray(leftControllerTransform.position, leftControllerTransform.forward);
                return true;
            }

            if (IsTriggerPressed(rightControllerNode) && rightControllerTransform != null)
            {
                ray = new Ray(rightControllerTransform.position, rightControllerTransform.forward);
                return true;
            }

            return false;
        }

        private bool IsTriggerPressed(XRNode node)
        {
            InputDevices.GetDevicesAtXRNode(node, _devices);
            for (var i = 0; i < _devices.Count; i++)
            {
                var device = _devices[i];
                if (device.TryGetFeatureValue(CommonUsages.triggerButton, out var triggerButton) && triggerButton)
                {
                    return true;
                }

                if (device.TryGetFeatureValue(CommonUsages.trigger, out var triggerValue) && triggerValue >= triggerThreshold)
                {
                    return true;
                }
            }

            return false;
        }

        private Vector3 GetHeadPosition(Vector3 fallback)
        {
            return hmdTransform != null ? hmdTransform.position : fallback;
        }

        private Vector3 GetHeadYawForward()
        {
            if (hmdTransform == null) return Vector3.forward;

            var forward = Vector3.ProjectOnPlane(hmdTransform.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }

            return forward.normalized;
        }

        private void RefreshStatus()
        {
            if (statusText == null) return;

            if (!_placementArmed)
            {
                statusText.text = _directVideoDragging
                    ? "\u6b63\u5728\u62d6\u52a8\u89c6\u9891\uff1a\u677e\u5f00\u5c04\u7ebf\u952e\u5373\u56fa\u5b9a\u5728\u5f53\u524d\u4f4d\u7f6e"
                    : "\u663e\u793a\u4e0e\u8bad\u7ec3\u5708\u5df2\u5206\u79bb\uff1a\u6309\u4f4f\u89c6\u9891\u753b\u9762\u62d6\u52a8\uff0c\u7a7a\u95f4\u5708\u4f7f\u7528\u6309\u94ae\u79fb\u52a8";
                return;
            }

            statusText.text = activeTarget == RehabSpatialRayTarget.VideoPanel
                ? "\u89c6\u9891\u4f4d\u7f6e\u5df2\u9009\u4e2d\uff1a\u6263\u52a8\u4efb\u610f\u624b\u67c4\u6263\u673a\u79fb\u52a8\u663e\u793a\uff0c\u4fdd\u6301\u6b63\u7acb"
                : "\u7a7a\u95f4\u4f4d\u7f6e\u5df2\u9009\u4e2d\uff1a\u5c04\u7ebf\u6307\u5411\u5730\u9762\u540e\u6263\u52a8\u6263\u673a\u79fb\u52a8\u8bad\u7ec3\u5708";
        }

        private void SetRayPreviewVisible(bool visible, Ray ray)
        {
            if (rayPreview == null && visible)
            {
                rayPreview = CreateRayPreview();
            }

            if (rayPreview == null) return;

            rayPreview.enabled = visible;
            if (!visible) return;

            rayPreview.SetPosition(0, ray.origin);
            rayPreview.SetPosition(1, ray.origin + ray.direction.normalized * maxRayDistanceMeters);
        }

        private LineRenderer CreateRayPreview()
        {
            var go = new GameObject("RehabSpatialRayPreview");
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.widthMultiplier = 0.012f;
            line.numCapVertices = 4;
            line.sharedMaterial = CreateRuntimeLineMaterial();
            line.startColor = new Color(0.35f, 0.96f, 1f, 0.74f);
            line.endColor = new Color(0.35f, 0.96f, 1f, 0.08f);
            line.enabled = false;
            return line;
        }

        private static Material CreateRuntimeLineMaterial()
        {
            var shader = Shader.Find("Sprites/Default") ??
                         Shader.Find("Universal Render Pipeline/Unlit") ??
                         Shader.Find("Standard");
            if (shader == null)
            {
                return null;
            }

            var material = new Material(shader);
            material.color = new Color(0.35f, 0.96f, 1f, 0.74f);
            return material;
        }

        private static Button CreateControlButton(
            Transform parent,
            string name,
            string text,
            Vector2 anchoredPosition,
            Vector2 size,
            UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var image = go.AddComponent<Image>();
            image.color = new Color(0.05f, 0.26f, 0.35f, 0.92f);
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0.45f, 0.96f, 1f, 0.28f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = new Color(0.08f, 0.42f, 0.52f, 0.96f);
            colors.pressedColor = new Color(0.02f, 0.18f, 0.24f, 0.96f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            if (onClick != null)
            {
                button.onClick.AddListener(onClick);
            }

            var label = CreateText(go.transform, "Label", text, 24f, TextAlignmentOptions.Center, Vector2.zero, size);
            label.fontStyle = FontStyles.Bold;
            label.color = new Color(0.88f, 1f, 1f, 0.96f);
            label.raycastTarget = false;
            return button;
        }

        private static TMP_Text CreateText(
            Transform parent,
            string name,
            string text,
            float fontSize,
            TextAlignmentOptions alignment,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            var label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            var font = FindRehabFontAsset();
            if (font != null)
            {
                label.font = font;
            }

            return label;
        }

        private static Image CreateDivider(
            Transform parent,
            string name,
            Vector2 anchoredPosition,
            Vector2 size,
            Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static TMP_FontAsset FindRehabFontAsset()
        {
            var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            for (var i = 0; i < fonts.Length; i++)
            {
                if (fonts[i] != null && fonts[i].name == "RehabChineseTMP")
                {
                    return fonts[i];
                }
            }

            return null;
        }

        private static Transform FindTransformByName(string objectName)
        {
            var transforms = FindObjectsOfType<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] != null && transforms[i].name == objectName)
                {
                    return transforms[i];
                }
            }

            return null;
        }
    }
}
