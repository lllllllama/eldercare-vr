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
        public bool autoCreateControlCanvas = true;
        public GameObject controlCanvasRoot;
        public TMP_Text statusText;
        public LineRenderer rayPreview;
        public float floorY = 0f;
        public float maxRayDistanceMeters = 4.5f;
        public float videoMovePlaneDistanceMeters = 1.8f;
        public float triggerThreshold = 0.65f;

        private readonly List<InputDevice> _devices = new List<InputDevice>();
        private bool _placementArmed;
        private bool _waitForTriggerRelease;

        public bool PlacementArmed
        {
            get { return _placementArmed; }
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
            return controller;
        }

        private void Awake()
        {
            ResolveReferences();
            EnsureControlCanvas();
        }

        private void Start()
        {
            ResolveReferences();
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

            if (!_placementArmed || !triggerPressed)
            {
                SetRayPreviewVisible(false, ray);
                return;
            }

            var moved = activeTarget == RehabSpatialRayTarget.VideoPanel
                ? TryMoveVideoPanelFromRay(ray)
                : TryPlaceTrainingAreaFromRay(ray);
            SetRayPreviewVisible(moved, ray);
        }

        public void SelectVideoTarget()
        {
            activeTarget = RehabSpatialRayTarget.VideoPanel;
            _placementArmed = true;
            _waitForTriggerRelease = true;
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
            if (!autoCreateControlCanvas || controlCanvasRoot != null) return;

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
                statusText = controlCanvasRoot.GetComponentInChildren<TMP_Text>(true);
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
            controlCanvasRoot.transform.localPosition = new Vector3(0f, -0.33f, 0.02f);
            controlCanvasRoot.transform.localRotation = Quaternion.identity;
            controlCanvasRoot.transform.localScale = Vector3.one * 0.0015f;

            var rect = controlCanvasRoot.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(820f, 126f);

            var canvas = controlCanvasRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;

            var scaler = controlCanvasRoot.GetComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 12f;

            var background = controlCanvasRoot.AddComponent<Image>();
            background.color = new Color(0.015f, 0.055f, 0.07f, 0.88f);
            var outline = controlCanvasRoot.AddComponent<Outline>();
            outline.effectColor = new Color(0.33f, 0.94f, 1f, 0.36f);
            outline.effectDistance = new Vector2(2f, -2f);

            CreateDivider(controlCanvasRoot.transform, "TopTrace", new Vector2(0f, 57f), new Vector2(760f, 3f), new Color(0.42f, 0.96f, 1f, 0.56f));
            CreateDivider(controlCanvasRoot.transform, "BottomTrace", new Vector2(0f, -57f), new Vector2(520f, 2f), new Color(0.42f, 0.96f, 1f, 0.28f));

            CreateControlButton(controlCanvasRoot.transform, "MoveVideoButton", "\u89c6\u9891\u4f4d\u7f6e", new Vector2(-306f, 12f), new Vector2(142f, 54f), SelectVideoTarget);
            CreateControlButton(controlCanvasRoot.transform, "MoveSpaceButton", "\u7a7a\u95f4\u4f4d\u7f6e", new Vector2(-154f, 12f), new Vector2(142f, 54f), SelectTrainingAreaTarget);
            CreateControlButton(controlCanvasRoot.transform, "VideoScaleDownButton", "\u89c6\u9891 -", new Vector2(0f, 12f), new Vector2(128f, 54f), ScaleVideoDown);
            CreateControlButton(controlCanvasRoot.transform, "VideoScaleUpButton", "\u89c6\u9891 +", new Vector2(142f, 12f), new Vector2(128f, 54f), ScaleVideoUp);
            CreateControlButton(controlCanvasRoot.transform, "ResetButton", "\u91cd\u7f6e\u663e\u793a", new Vector2(300f, 12f), new Vector2(146f, 54f), ResetVideoPlacement);

            statusText = CreateText(controlCanvasRoot.transform, "Status", "\u9009\u62e9\u76ee\u6807\u540e\uff0c\u6263\u52a8\u4efb\u610f\u624b\u67c4\u6263\u673a\u8fdb\u884c\u79fb\u52a8", 22f, TextAlignmentOptions.Center, new Vector2(0f, -40f), new Vector2(760f, 34f));
            statusText.color = new Color(0.74f, 0.98f, 1f, 0.78f);
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
                statusText.text = "\u663e\u793a\u4e0e\u8bad\u7ec3\u5708\u5df2\u5206\u79bb\uff0c\u9009\u62e9\u76ee\u6807\u540e\u53ef\u7528\u5c04\u7ebf\u79fb\u52a8";
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
