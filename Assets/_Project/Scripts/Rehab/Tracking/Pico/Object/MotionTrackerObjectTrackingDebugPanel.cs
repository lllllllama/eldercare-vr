using System.Text;
using PicoElderCare.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PicoElderCare.Rehab.Tracking.Pico.ObjectTracking
{
    /// <summary>
    /// Development-only view used by MotionTracker_ObjectTracking_Test. It is
    /// deliberately a client of IWristTrackerSetupService, not a second tracker stack.
    /// </summary>
    public sealed class MotionTrackerObjectTrackingDebugPanel : MonoBehaviour
    {
        private const float RefreshIntervalSeconds = 0.2f;
        private readonly StringBuilder _builder = new StringBuilder(2048);
        private IWristTrackerSetupService _service;
        private TMP_Text _statusText;
        private TMP_FontAsset _font;
        private float _nextRefreshTime;

        public TMP_FontAsset uiFont;

        private void Awake()
        {
            _service = WristTrackingRuntime.EnsureInstance();
            BuildIfNeeded();
        }

        private void OnEnable()
        {
            if (_service == null) _service = WristTrackingRuntime.EnsureInstance();
            if (_service != null)
            {
                _service.AdvancedDiagnosticsVisible = true;
                _service.StartDiagnostics();
            }
        }

        private void OnDisable()
        {
            if (_service != null) _service.StopDiagnostics();
        }

        private void Update()
        {
            if (_service == null || Time.unscaledTime < _nextRefreshTime) return;
            _nextRefreshTime = Time.unscaledTime + RefreshIntervalSeconds;
            RefreshText();
        }

        private void BuildIfNeeded()
        {
            var existing = transform.Find("DebugFrame/Status");
            if (existing != null)
            {
                _statusText = existing.GetComponent<TMP_Text>();
                return;
            }

            var sourceText = FindObjectOfType<TMP_Text>(true);
            _font = uiFont != null ? uiFont : (sourceText != null ? sourceText.font : null);
            var rootRect = transform as RectTransform;
            if (rootRect != null) rootRect.sizeDelta = new Vector2(1000f, 640f);

            var frame = CreatePanel(transform, "DebugFrame", new Vector2(960f, 600f), Vector2.zero, ElderCareMenuDesignTokens.Wood, 38f);
            ElderCareMenuPanelBuilder.ConfigureNativeStroke(frame, ElderCareMenuDesignTokens.WoodDark, 2f);
            var paper = CreatePanel(frame.transform, "Paper", new Vector2(930f, 570f), Vector2.zero, ElderCareMenuDesignTokens.RiceLight, 30f);
            ElderCareMenuPanelBuilder.ConfigureNativeStroke(paper, ElderCareMenuDesignTokens.GoldStroke, 1.5f);
            CreateText(frame.transform, "Title", "Object Tracking 真机诊断", new Vector2(820f, 54f), new Vector2(0f, 252f), 36f, FontStyles.Bold, TextAlignmentOptions.Center, ElderCareMenuDesignTokens.TextPrimary);
            CreateText(frame.transform, "Hint", "开发专用场景 · 与入口设置共用同一套绑定和校准", new Vector2(820f, 34f), new Vector2(0f, 211f), 20f, FontStyles.Normal, TextAlignmentOptions.Center, ElderCareMenuDesignTokens.TextSecondary);
            _statusText = CreateText(frame.transform, "Status", string.Empty, new Vector2(850f, 420f), new Vector2(0f, -24f), 16f, FontStyles.Normal, TextAlignmentOptions.TopLeft, ElderCareMenuDesignTokens.TextPrimary);
        }

        private void RefreshText()
        {
            var left = _service.LeftTracker;
            var right = _service.RightTracker;
            var camera = Camera.main;
            _builder.Clear();
            _builder.Append("Connected Tracker Count: ").Append(_service.ConnectedTrackerCount).Append(" / 2\n")
                .Append("Tracker IDs: ");
            AppendConnectedTrackerIds();
            _builder.Append('\n').Append("Current Tracking Mode: ").Append(_service.CurrentTrackingMode)
                .Append("    Preference: ").Append(_service.Preference).Append('\n')
                .Append("Binding State: ").Append(_service.HasValidBinding ? "Ready" : "Required")
                .Append("    Calibration: ").Append(_service.IsCalibrationReady ? "Ready" : "Required")
                .Append("    Setup: ").Append(_service.State).Append("\n\n");

            _builder.Append("HMD Marker: ").Append(_service.IsHmdPoseValid ? "Valid" : "Lost");
            if (camera != null)
            {
                _builder.Append("    Position: ").Append(camera.transform.position.ToString("F3"))
                    .Append("    Rotation: ").Append(camera.transform.rotation.eulerAngles.ToString("F1"));
            }
            _builder.Append("\n\n");
            AppendTracker("Left Wrist Tracker", left);
            AppendTracker("Right Wrist Tracker", right);
            AppendBindingDiagnostics();
            _builder.Append("Provider Stable Frame Count: ")
                .Append(_service.Provider != null ? _service.Provider.StableFrameCount : 0)
                .Append("    Switch Count: ").Append(_service.ProviderSwitchCount).Append('\n')
                .Append("Status: ").Append(_service.StatusMessage).Append('\n')
                .Append("Last Error: ").Append(string.IsNullOrEmpty(_service.LastError) ? "None" : _service.LastError);

            if (_statusText != null) _statusText.text = _builder.ToString();
        }

        private void AppendConnectedTrackerIds()
        {
            var wroteAny = false;
            for (var i = 0; i < _service.ConnectedTrackerCount; i++)
            {
                string trackerId;
                if (!_service.TryGetConnectedTrackerId(i, out trackerId)) continue;
                if (wroteAny) _builder.Append(", ");
                _builder.Append(trackerId);
                wroteAny = true;
            }

            if (!wroteAny) _builder.Append("None");
        }

        private void AppendBindingDiagnostics()
        {
            var binding = _service.Binding;
            if (binding == null) return;

            _builder.Append("Binding Phase: ").Append(binding.State)
                .Append("    Preparing: ").Append(binding.IsPreparingSample)
                .Append("    Prep Left: ").Append(binding.PreparationRemainingSeconds.ToString("F2")).Append("s\n")
                .Append("Pending Left ID: ").Append(string.IsNullOrEmpty(binding.PendingLeftTrackerId) ? "None" : binding.PendingLeftTrackerId)
                .Append("    Candidate: ").Append(string.IsNullOrEmpty(binding.CurrentCandidateTrackerId) ? "None" : binding.CurrentCandidateTrackerId).Append('\n');

            for (var i = 0; i < binding.SampleTrackerCount; i++)
            {
                WristBindingSampleDiagnostics sample;
                if (!binding.TryGetSampleDiagnostics(i, out sample)) continue;
                _builder.Append("  Sample ").Append(i).Append(": ID=").Append(sample.trackerId)
                    .Append(" ValidFrames=").Append(sample.validSamples)
                    .Append(" Travel=").Append(sample.travelMeters.ToString("F3")).Append("m\n");
            }

            _builder.Append("Last Binding Result: ")
                .Append(string.IsNullOrEmpty(binding.LastResultMessage) ? "None" : binding.LastResultMessage)
                .Append("\n\n");
        }

        private void AppendTracker(string label, WristTrackerInfo tracker)
        {
            _builder.Append(label).Append(" Marker\n")
                .Append("  ID: ").Append(string.IsNullOrEmpty(tracker.trackerId) ? "Unbound" : tracker.trackerId)
                .Append("    Bound: ").Append(tracker.bound)
                .Append("    Connected: ").Append(tracker.connected)
                .Append("    Pose Valid: ").Append(tracker.poseValid).Append('\n')
                .Append("  Position: ").Append(tracker.position.ToString("F3"))
                .Append("    Rotation: ").Append(tracker.rotation.eulerAngles.ToString("F1"))
                .Append("    Stable Frames: ").Append(tracker.stableFrameCount).Append("\n\n");
        }

        private TMP_Text CreateText(Transform parent, string objectName, string value, Vector2 size, Vector2 position, float fontSize, FontStyles style, TextAlignmentOptions alignment, Color color)
        {
            var rect = CreateRect(objectName, parent, size, position);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (_font != null) text.font = _font;
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            return text;
        }

        private static ElderCareRoundedPanel CreatePanel(Transform parent, string objectName, Vector2 size, Vector2 position, Color color, float radius)
        {
            var rect = CreateRect(objectName, parent, size, position);
            var panel = rect.gameObject.AddComponent<ElderCareRoundedPanel>();
            panel.color = color;
            panel.cornerRadius = radius;
            panel.cornerSegments = 10;
            panel.raycastTarget = false;
            return panel;
        }

        private static RectTransform CreateRect(string objectName, Transform parent, Vector2 size, Vector2 position)
        {
            var go = new GameObject(objectName, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            rect.localScale = Vector3.one;
            return rect;
        }
    }
}
