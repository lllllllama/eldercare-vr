using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PicoElderCare.Rehab.Tracking.Pico
{
    public sealed class PicoBodyTrackingStatusPanel : MonoBehaviour
    {
        private const string CanvasObjectName = "PicoBodyTrackingStatusCanvas";
        private const string TextObjectName = "DiagnosticsText";
        private const int TextCapacity = 1024;

        [SerializeField] private bool statusPanelEnabled = true;
        [SerializeField] private PicoBodyTrackingProvider provider;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private float statusDistance = 1.2f;
        [SerializeField] private float statusVerticalOffset = -0.18f;
        [SerializeField] private float statusFontSize = 72f;
        [SerializeField] private Vector2 statusPanelSize = new Vector2(900f, 420f);
        [SerializeField] private Vector3 statusPanelScale = new Vector3(0.0015f, 0.0015f, 0.0015f);
        [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.8f);
        [SerializeField] private Color textColor = Color.white;

        private readonly StringBuilder _textBuilder = new StringBuilder(TextCapacity);
        private Canvas _statusCanvas;
        private Image _background;
        private TextMeshProUGUI _statusText;
        private bool _ownsCanvas;
        private bool _initialized;
        private bool _cameraLookupAttempted;
        private bool _cameraErrorLogged;

        public bool StatusPanelEnabled
        {
            get { return statusPanelEnabled; }
            set
            {
                statusPanelEnabled = value;
                ApplyVisibility();
            }
        }

        public PicoBodyTrackingProvider Provider
        {
            get { return provider; }
            set { provider = value; }
        }

        public Camera TargetCamera
        {
            get { return targetCamera; }
            set
            {
                targetCamera = value;
                _cameraLookupAttempted = value != null;
                if (_initialized && targetCamera != null)
                {
                    ApplyCanvasLayout();
                }
            }
        }

        public float StatusDistance
        {
            get { return statusDistance; }
            set
            {
                statusDistance = value;
                ApplyCanvasLayout();
            }
        }

        public float StatusVerticalOffset
        {
            get { return statusVerticalOffset; }
            set
            {
                statusVerticalOffset = value;
                ApplyCanvasLayout();
            }
        }

        public float StatusFontSize
        {
            get { return statusFontSize; }
            set
            {
                statusFontSize = Mathf.Max(1f, value);
                ApplyVisualSettings();
            }
        }

        public Vector2 StatusPanelSize
        {
            get { return statusPanelSize; }
            set
            {
                statusPanelSize = value;
                ApplyCanvasLayout();
            }
        }

        public Vector3 StatusPanelScale
        {
            get { return statusPanelScale; }
            set
            {
                statusPanelScale = value;
                ApplyCanvasLayout();
            }
        }

        public Color BackgroundColor
        {
            get { return backgroundColor; }
            set
            {
                backgroundColor = value;
                ApplyVisualSettings();
            }
        }

        public Color TextColor
        {
            get { return textColor; }
            set
            {
                textColor = value;
                ApplyVisualSettings();
            }
        }

        public Canvas StatusCanvas
        {
            get { return _statusCanvas; }
        }

        public TextMeshProUGUI StatusText
        {
            get { return _statusText; }
        }

        private void OnEnable()
        {
            _cameraLookupAttempted = targetCamera != null;
            if (Application.isPlaying && statusPanelEnabled)
            {
                RefreshNow();
            }
        }

        private void OnDisable()
        {
            if (_statusCanvas != null)
            {
                _statusCanvas.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (_ownsCanvas && _statusCanvas != null)
            {
                DestroyUnityObject(_statusCanvas.gameObject);
            }
        }

        private void OnValidate()
        {
            statusFontSize = Mathf.Max(1f, statusFontSize);
            statusPanelSize.x = Mathf.Max(1f, statusPanelSize.x);
            statusPanelSize.y = Mathf.Max(1f, statusPanelSize.y);
            ApplyCanvasLayout();
            ApplyVisualSettings();
            ApplyVisibility();
        }

        private void Update()
        {
            RefreshNow();
        }

        public void RefreshNow()
        {
            if (!statusPanelEnabled)
            {
                ApplyVisibility();
                return;
            }

            if (!EnsureInitialized())
            {
                return;
            }

            ApplyVisibility();
            UpdateStatusText();
        }

        private bool EnsureInitialized()
        {
            if (_initialized && _statusCanvas != null && _statusText != null)
            {
                return true;
            }

            if (!TryResolveTargetCamera())
            {
                return false;
            }

            var existingCanvasTransform = targetCamera.transform.Find(CanvasObjectName);
            if (existingCanvasTransform != null)
            {
                _statusCanvas = existingCanvasTransform.GetComponent<Canvas>();
            }

            if (_statusCanvas == null)
            {
                var canvasObject = new GameObject(
                    CanvasObjectName,
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(CanvasRenderer),
                    typeof(Image));
                _statusCanvas = canvasObject.GetComponent<Canvas>();
                _ownsCanvas = true;
            }

            _statusCanvas.renderMode = RenderMode.WorldSpace;
            _statusCanvas.worldCamera = targetCamera;
            _statusCanvas.overrideSorting = true;
            _statusCanvas.sortingOrder = 1000;

            var scaler = _statusCanvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = _statusCanvas.gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.dynamicPixelsPerUnit = 10f;

            _background = _statusCanvas.GetComponent<Image>();
            if (_background == null)
            {
                _background = _statusCanvas.gameObject.AddComponent<Image>();
            }

            _background.raycastTarget = false;

            var textTransform = _statusCanvas.transform.Find(TextObjectName);
            if (textTransform != null)
            {
                _statusText = textTransform.GetComponent<TextMeshProUGUI>();
            }

            if (_statusText == null)
            {
                var textObject = new GameObject(
                    TextObjectName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(TextMeshProUGUI));
                textObject.transform.SetParent(_statusCanvas.transform, false);
                _statusText = textObject.GetComponent<TextMeshProUGUI>();
            }

            var textRect = _statusText.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            _statusText.enableAutoSizing = false;
            _statusText.alignment = TextAlignmentOptions.MidlineLeft;
            _statusText.enableWordWrapping = true;
            _statusText.overflowMode = TextOverflowModes.Overflow;
            _statusText.richText = false;
            _statusText.raycastTarget = false;
            _statusText.lineSpacing = -55f;
            _statusText.margin = new Vector4(28f, 20f, 28f, 20f);
            _statusText.outlineColor = Color.black;
            _statusText.outlineWidth = 0.2f;

            _initialized = true;
            ApplyCanvasLayout();
            ApplyVisualSettings();
            return true;
        }

        private bool TryResolveTargetCamera()
        {
            if (targetCamera != null)
            {
                return true;
            }

            if (!_cameraLookupAttempted)
            {
                _cameraLookupAttempted = true;
                targetCamera = Camera.main;
            }

            if (targetCamera != null)
            {
                return true;
            }

            if (!_cameraErrorLogged)
            {
                _cameraErrorLogged = true;
                Debug.LogError(
                    "PicoBodyTrackingStatusPanel could not find a Main Camera. Assign Target Camera in the Inspector.",
                    this);
            }

            return false;
        }

        private void ApplyCanvasLayout()
        {
            if (!_initialized || _statusCanvas == null || targetCamera == null)
            {
                return;
            }

            var canvasTransform = _statusCanvas.transform;
            if (canvasTransform.parent != targetCamera.transform)
            {
                canvasTransform.SetParent(targetCamera.transform, false);
            }

            canvasTransform.localPosition = new Vector3(0f, statusVerticalOffset, statusDistance);
            canvasTransform.localRotation = Quaternion.identity;
            canvasTransform.localScale = statusPanelScale;

            var canvasRect = _statusCanvas.transform as RectTransform;
            if (canvasRect != null)
            {
                canvasRect.sizeDelta = statusPanelSize;
            }

            _statusCanvas.worldCamera = targetCamera;
        }

        private void ApplyVisualSettings()
        {
            if (_background != null)
            {
                _background.color = backgroundColor;
            }

            if (_statusText != null)
            {
                _statusText.fontSize = statusFontSize;
                _statusText.enableAutoSizing = false;
                _statusText.color = textColor;
                _statusText.outlineColor = Color.black;
                _statusText.outlineWidth = 0.2f;
            }
        }

        private void ApplyVisibility()
        {
            var shouldBeVisible = statusPanelEnabled && isActiveAndEnabled;
            if (_statusCanvas != null && _statusCanvas.gameObject.activeSelf != shouldBeVisible)
            {
                _statusCanvas.gameObject.SetActive(shouldBeVisible);
            }
        }

        private void UpdateStatusText()
        {
            if (_statusText == null)
            {
                return;
            }

            var diagnostics = provider != null ? provider.Diagnostics : null;
            _textBuilder.Clear();
            _textBuilder.Append("Tracking State: ");
            AppendTrackingState(_textBuilder, provider != null
                ? provider.TrackingState
                : RehabTrackingState.Unavailable);
            _textBuilder.Append("\nValid Joint Count: ");
            AppendInt(_textBuilder, diagnostics != null ? diagnostics.validJointCount : 0);
            _textBuilder.Append("\nSupport Result: ");
            AppendInt(_textBuilder, diagnostics != null ? diagnostics.supportResult : 0);
            _textBuilder.Append("\nStart Result: ");
            AppendInt(_textBuilder, diagnostics != null ? diagnostics.startResult : 0);
            _textBuilder.Append("\nState Result: ");
            AppendInt(_textBuilder, diagnostics != null ? diagnostics.stateResult : 0);
            _textBuilder.Append("\nData Result: ");
            AppendInt(_textBuilder, diagnostics != null ? diagnostics.dataResult : 0);
            _textBuilder.Append("\nSuccessful Sample Count: ");
            AppendInt(_textBuilder, diagnostics != null ? diagnostics.successfulSampleCount : 0);
            _textBuilder.Append("\nFailed Sample Count: ");
            AppendInt(_textBuilder, diagnostics != null ? diagnostics.failedSampleCount : 0);
            _textBuilder.Append("\nLast Error: ");
            if (diagnostics == null || string.IsNullOrEmpty(diagnostics.lastError))
            {
                _textBuilder.Append("None");
            }
            else
            {
                _textBuilder.Append(diagnostics.lastError);
            }

            _statusText.SetText(_textBuilder);
        }

        private static void AppendInt(StringBuilder builder, int value)
        {
            if (value == 0)
            {
                builder.Append('0');
                return;
            }

            uint remaining;
            if (value < 0)
            {
                builder.Append('-');
                remaining = unchecked((uint)(-(long)value));
            }
            else
            {
                remaining = (uint)value;
            }

            var divisor = 1u;
            while (remaining / divisor >= 10u)
            {
                divisor *= 10u;
            }

            while (divisor > 0u)
            {
                builder.Append((char)('0' + remaining / divisor));
                remaining %= divisor;
                divisor /= 10u;
            }
        }

        private static void AppendTrackingState(StringBuilder builder, RehabTrackingState state)
        {
            switch (state)
            {
                case RehabTrackingState.Unsupported: builder.Append("Unsupported"); break;
                case RehabTrackingState.Starting: builder.Append("Starting"); break;
                case RehabTrackingState.WaitingForDevice: builder.Append("WaitingForDevice"); break;
                case RehabTrackingState.WaitingForCalibration: builder.Append("WaitingForCalibration"); break;
                case RehabTrackingState.Valid: builder.Append("Valid"); break;
                case RehabTrackingState.Limited: builder.Append("Limited"); break;
                case RehabTrackingState.Lost: builder.Append("Lost"); break;
                case RehabTrackingState.Error: builder.Append("Error"); break;
                default: builder.Append("Unavailable"); break;
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
