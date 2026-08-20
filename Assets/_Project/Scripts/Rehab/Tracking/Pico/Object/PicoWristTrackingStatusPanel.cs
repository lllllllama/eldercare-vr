using System.Text;
using PicoElderCare.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PicoElderCare.Rehab.Tracking.Pico.ObjectTracking
{
    /// <summary>Runtime-built secondary panel for the existing MainEntry canvas.</summary>
    public sealed class PicoWristTrackingStatusPanel : MonoBehaviour
    {
        private const float RefreshIntervalSeconds = 0.2f;
        private static readonly Vector2 PanelSize = new Vector2(980f, 600f);

        private readonly StringBuilder _statusBuilder = new StringBuilder(1024);
        private IWristTrackerSetupService _service;
        [SerializeField] private UnifiedEntryMenu entryMenu;
        private Transform _mainPanel;
        private RectTransform _panelRoot;
        private TMP_FontAsset _font;
        private TMP_Text _summaryText;
        private TMP_Text _leftText;
        private TMP_Text _rightText;
        private TMP_Text _detailText;
        private GameObject _leftCard;
        private GameObject _rightCard;
        private GameObject _advancedCard;
        private Button _diagnosticsButton;
        private Button _advancedButton;
        private float _nextRefreshTime;
        private bool _trackerSetupRequestedThisOpen;

        public bool IsOpen { get { return _panelRoot != null && _panelRoot.gameObject.activeSelf; } }
        public UnifiedEntryMenu EntryMenu { get { return entryMenu; } }

        public static PicoWristTrackingStatusPanel Ensure(Transform canvas, UnifiedEntryMenu entryMenu)
        {
            if (canvas == null) return null;
            var panel = canvas.GetComponent<PicoWristTrackingStatusPanel>();
            if (panel == null) panel = canvas.gameObject.AddComponent<PicoWristTrackingStatusPanel>();
            panel.Configure(entryMenu);
            return panel;
        }

        /// <summary>
        /// Creates the authored panel hierarchy without starting the PICO runtime.
        /// SceneBuilder uses this in Edit Mode so the generated scene stays
        /// deterministic and opening the project never calls device APIs.
        /// </summary>
        public bool BuildOrRepairAuthoredPanel(UnifiedEntryMenu owner)
        {
            entryMenu = owner != null ? owner : entryMenu;
            if (_panelRoot == null) BuildPanel();
            if (_mainPanel == null) _mainPanel = transform.Find("Panel");
            var changed = MigrateSetupAction();
            ResolveExistingText();
            return changed;
        }

        public void Configure(UnifiedEntryMenu owner)
        {
            Configure(owner, null);
        }

        public void Configure(UnifiedEntryMenu owner, IWristTrackerSetupService service)
        {
            entryMenu = owner != null ? owner : entryMenu;
            if (service != null) _service = service;
            else if (_service == null) _service = WristTrackingRuntime.EnsureInstance();
            if (_panelRoot == null) BuildPanel();
            if (_mainPanel == null) _mainPanel = transform.Find("Panel");
            MigrateSetupAction();
            BindRuntimeActions();
        }

        public void Open()
        {
            Configure(entryMenu);
            if (_mainPanel != null) _mainPanel.gameObject.SetActive(false);
            if (_panelRoot != null)
            {
                _panelRoot.gameObject.SetActive(true);
                _panelRoot.SetAsLastSibling();
            }
            RequestTrackerSetupOncePerOpen();
            RefreshNow();
        }

        public void Close()
        {
            if (_service != null) _service.StopDiagnostics();
            if (_panelRoot != null) _panelRoot.gameObject.SetActive(false);
            if (_mainPanel != null) _mainPanel.gameObject.SetActive(true);
            _trackerSetupRequestedThisOpen = false;
        }

        private void RequestTrackerSetupOncePerOpen()
        {
            if (_trackerSetupRequestedThisOpen || _service == null) return;
            _trackerSetupRequestedThisOpen = true;
            _service.RequestTrackerSetup();
        }

        private void Update()
        {
            if (!IsOpen || Time.unscaledTime < _nextRefreshTime) return;
            RefreshNow();
        }

        private void OnDestroy()
        {
            if (_service != null) _service.StopDiagnostics();
        }

        private void BuildPanel()
        {
            var existing = transform.Find("TrackerSettingsPanel") as RectTransform;
            if (existing != null)
            {
                _panelRoot = existing;
                ResolveExistingText();
                return;
            }

            var textInScene = GetComponentInChildren<TMP_Text>(true);
            _font = textInScene != null ? textInScene.font : null;

            _panelRoot = CreateRect("TrackerSettingsPanel", transform, PanelSize, new Vector2(0f, 18f));
            CreatePanel(_panelRoot, "Shadow", PanelSize + new Vector2(26f, 24f), new Vector2(0f, -8f), ElderCareMenuDesignTokens.WarmShadow, 42f);
            var frame = CreatePanel(_panelRoot, "WoodFrame", PanelSize, Vector2.zero, ElderCareMenuDesignTokens.Wood, 40f);
            ElderCareMenuPanelBuilder.ConfigureNativeStroke(frame, ElderCareMenuDesignTokens.WoodDark, 2f);
            var paper = CreatePanel(frame.transform, "RicePaper", PanelSize - new Vector2(26f, 26f), Vector2.zero, ElderCareMenuDesignTokens.RiceLight, 32f);
            ElderCareMenuPanelBuilder.ConfigureNativeStroke(paper, ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.GoldStroke, 0.75f), 1.5f);

            CreateText(frame.transform, "Title", "设备与传感器", new Vector2(620f, 52f), new Vector2(0f, 246f), 36f, FontStyles.Bold, TextAlignmentOptions.Center, ElderCareMenuDesignTokens.TextPrimary);
            CreateText(frame.transform, "Subtitle", "设置腕部传感器，也可以继续使用手柄", new Vector2(680f, 32f), new Vector2(0f, 207f), 20f, FontStyles.Normal, TextAlignmentOptions.Center, ElderCareMenuDesignTokens.TextSecondary);

            var summaryCard = CreatePanel(frame.transform, "DeviceSummary", new Vector2(900f, 142f), new Vector2(0f, 112f), ElderCareMenuDesignTokens.Card, 25f);
            ElderCareMenuPanelBuilder.ConfigureNativeStroke(summaryCard, ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.GoldStroke, 0.52f), 1.2f);
            _summaryText = CreateText(summaryCard.transform, "Summary", string.Empty, new Vector2(848f, 112f), Vector2.zero, 22f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, ElderCareMenuDesignTokens.TextPrimary);

            var leftCard = CreatePanel(frame.transform, "LeftWrist", new Vector2(436f, 118f), new Vector2(-232f, -32f), ElderCareMenuDesignTokens.CardHighlight, 23f);
            _leftCard = leftCard.gameObject;
            ElderCareMenuPanelBuilder.ConfigureNativeStroke(leftCard, ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.Jade, 0.58f), 1.5f);
            _leftText = CreateText(leftCard.transform, "Status", string.Empty, new Vector2(392f, 90f), Vector2.zero, 19f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, ElderCareMenuDesignTokens.TextPrimary);

            var rightCard = CreatePanel(frame.transform, "RightWrist", new Vector2(436f, 118f), new Vector2(232f, -32f), ElderCareMenuDesignTokens.CardHighlight, 23f);
            _rightCard = rightCard.gameObject;
            ElderCareMenuPanelBuilder.ConfigureNativeStroke(rightCard, ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.Amber, 0.58f), 1.5f);
            _rightText = CreateText(rightCard.transform, "Status", string.Empty, new Vector2(392f, 90f), Vector2.zero, 19f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, ElderCareMenuDesignTokens.TextPrimary);

            var actions = CreateRect("Actions", frame.transform, new Vector2(906f, 132f), new Vector2(0f, -166f));
            CreateActionButton(actions, "Setup", "重新配置传感器", new Vector2(-338f, 36f), RequestTrackerSetupExplicitly);
            CreateActionButton(actions, "Bind", "开始左右匹配", new Vector2(-113f, 36f), delegate { _service.BeginBinding(); });
            CreateActionButton(actions, "Clear", "清除匹配", new Vector2(113f, 36f), delegate { _service.ClearBinding(); });
            CreateActionButton(actions, "Verify", "快速佩戴测试", new Vector2(338f, 36f), delegate { _service.BeginQuickVerification(); });
            CreateActionButton(actions, "Calibrate", "开始腕部校准", new Vector2(-338f, -36f), delegate { _service.BeginCalibration(); });
            _diagnosticsButton = CreateActionButton(actions, "Markers", "开始传感器测试", new Vector2(-113f, -36f), ToggleDiagnostics);
            _advancedButton = CreateActionButton(actions, "Advanced", "高级诊断", new Vector2(113f, -36f), ToggleAdvanced);
            CreateActionButton(actions, "Identity", "使用腕带中心", new Vector2(338f, -36f), delegate { _service.UseIdentityCalibration(); });

            var advancedCard = CreatePanel(frame.transform, "AdvancedDiagnostics", new Vector2(900f, 126f), new Vector2(0f, -28f), ElderCareMenuDesignTokens.CardHighlight, 23f);
            ElderCareMenuPanelBuilder.ConfigureNativeStroke(advancedCard, ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.GoldStroke, 0.72f), 1.3f);
            _advancedCard = advancedCard.gameObject;
            _detailText = CreateText(advancedCard.transform, "Details", string.Empty, new Vector2(856f, 110f), Vector2.zero, 12f, FontStyles.Normal, TextAlignmentOptions.TopLeft, ElderCareMenuDesignTokens.TextSecondary);
            _advancedCard.SetActive(false);
            CreateActionButton(frame.transform, "Back", "返回", new Vector2(-360f, -252f), Close, new Vector2(180f, 54f));

            _panelRoot.gameObject.SetActive(false);
        }

        private void ResolveExistingText()
        {
            if (_panelRoot == null) return;
            _summaryText = FindText("DeviceSummary/Summary");
            _leftText = FindText("LeftWrist/Status");
            _rightText = FindText("RightWrist/Status");
            _detailText = FindText("AdvancedDiagnostics/Details");
            _leftCard = _panelRoot.Find("WoodFrame/LeftWrist") != null ? _panelRoot.Find("WoodFrame/LeftWrist").gameObject : null;
            _rightCard = _panelRoot.Find("WoodFrame/RightWrist") != null ? _panelRoot.Find("WoodFrame/RightWrist").gameObject : null;
            _advancedCard = _panelRoot.Find("WoodFrame/AdvancedDiagnostics") != null ? _panelRoot.Find("WoodFrame/AdvancedDiagnostics").gameObject : null;
            _diagnosticsButton = FindButton("WoodFrame/Actions/Markers");
            _advancedButton = FindButton("WoodFrame/Actions/Advanced");
        }

        private Button FindButton(string relativePath)
        {
            var target = _panelRoot != null ? _panelRoot.Find(relativePath) : null;
            return target != null ? target.GetComponent<Button>() : null;
        }

        private void BindRuntimeActions()
        {
            if (_panelRoot == null || _service == null) return;
            BindAction("WoodFrame/Actions/Setup", RequestTrackerSetupExplicitly);
            BindAction("WoodFrame/Actions/Bind", delegate { _service.BeginBinding(); });
            BindAction("WoodFrame/Actions/Clear", delegate { _service.ClearBinding(); });
            BindAction("WoodFrame/Actions/Verify", delegate { _service.BeginQuickVerification(); });
            BindAction("WoodFrame/Actions/Calibrate", delegate { _service.BeginCalibration(); });
            _diagnosticsButton = BindAction("WoodFrame/Actions/Markers", ToggleDiagnostics);
            _advancedButton = BindAction("WoodFrame/Actions/Advanced", ToggleAdvanced);
            BindAction("WoodFrame/Actions/Identity", delegate { _service.UseIdentityCalibration(); });
            BindAction("WoodFrame/Back", Close);
        }

        private bool MigrateSetupAction()
        {
            if (_panelRoot == null) return false;
            var actions = _panelRoot.Find("WoodFrame/Actions");
            if (actions == null) return false;

            var changed = false;
            var setupAction = actions.Find("Setup");
            var legacyAction = actions.Find("Rescan");
            if (setupAction == null && legacyAction != null)
            {
                legacyAction.name = "Setup";
                setupAction = legacyAction;
                changed = true;
            }

            var label = setupAction != null ? setupAction.GetComponentInChildren<TMP_Text>(true) : null;
            if (label != null && label.text != "重新配置传感器")
            {
                label.text = "重新配置传感器";
                changed = true;
            }

            return changed;
        }

        private void RequestTrackerSetupExplicitly()
        {
            if (_service == null) return;
            _service.RequestTrackerSetup();
            RefreshNow();
        }

        private Button BindAction(string relativePath, UnityAction action)
        {
            var target = _panelRoot.Find(relativePath);
            var button = target != null ? target.GetComponent<Button>() : null;
            if (button == null) return null;
            button.onClick.RemoveAllListeners();
            if (action != null) button.onClick.AddListener(action);
            return button;
        }

        private TMP_Text FindText(string relativePath)
        {
            var child = _panelRoot.Find("WoodFrame/" + relativePath);
            return child != null ? child.GetComponent<TMP_Text>() : null;
        }

        private void RefreshNow()
        {
            if (_service == null) return;
            _nextRefreshTime = Time.unscaledTime + RefreshIntervalSeconds;
            var left = _service.LeftTracker;
            var right = _service.RightTracker;

            _statusBuilder.Clear();
            _statusBuilder.Append("Motion Tracker：已检测 ").Append(_service.ConnectedTrackerCount).Append(" / 2\n");
            _statusBuilder.Append("HMD：").Append(_service.IsHmdPoseValid ? "正常" : "追踪丢失").Append("    当前模式：");
            _statusBuilder.Append(ModeLabel(_service.CurrentTrackingMode)).Append("\n");
            _statusBuilder.Append("状态：").Append(_service.StatusMessage);
            if (_summaryText != null) _summaryText.text = _statusBuilder.ToString();

            if (_leftText != null) _leftText.text = BuildWristText("左腕", left, true);
            if (_rightText != null) _rightText.text = BuildWristText("右腕", right, false);
            if (_diagnosticsButton != null) SetButtonLabel(_diagnosticsButton, _service.DiagnosticsActive ? "停止传感器测试" : "开始传感器测试");
            if (_advancedButton != null) SetButtonLabel(_advancedButton, _service.AdvancedDiagnosticsVisible ? "关闭高级诊断" : "高级诊断");

            var showAdvanced = _service.AdvancedDiagnosticsVisible;
            if (_leftCard != null) _leftCard.SetActive(!showAdvanced);
            if (_rightCard != null) _rightCard.SetActive(!showAdvanced);
            if (_advancedCard != null) _advancedCard.SetActive(showAdvanced);
            if (_detailText != null && showAdvanced)
            {
                BuildAdvancedDiagnostics();
                _detailText.text = _statusBuilder.ToString();
            }
        }

        private string BuildWristText(string label, WristTrackerInfo info, bool isLeft)
        {
            _statusBuilder.Clear();
            _statusBuilder.Append(label).Append("：");
            if (!info.bound)
            {
                var binding = _service.Binding;
                if (binding != null && isLeft && binding.State == WristTrackerSetupState.BindingRight &&
                    !string.IsNullOrEmpty(binding.PendingLeftTrackerId))
                {
                    return _statusBuilder.Append("已识别，等待右腕\nID：").Append(binding.PendingLeftTrackerId).ToString();
                }

                if (binding != null && !isLeft && binding.State == WristTrackerSetupState.BindingRight)
                {
                    return binding.IsPreparingSample
                        ? _statusBuilder.Append("准备识别\n请先保持双腕静止").ToString()
                        : _statusBuilder.Append("正在识别\n请只移动右手腕").ToString();
                }

                if (binding != null && isLeft && binding.LastResultFailed &&
                    !string.IsNullOrEmpty(binding.LastPendingLeftTrackerId))
                {
                    return _statusBuilder.Append("上次已识别，未保存\nID：").Append(binding.LastPendingLeftTrackerId).ToString();
                }

                return _statusBuilder.Append("未绑定\n请开始左右匹配").ToString();
            }
            _statusBuilder.Append(info.connected ? "已连接" : "未连接").Append(" · ")
                .Append(info.poseValid ? "Pose 正常" : "信号暂时丢失").Append('\n')
                .Append("ID：").Append(info.trackerId).Append("    稳定帧：").Append(info.stableFrameCount);
            return _statusBuilder.ToString();
        }

        private void BuildAdvancedDiagnostics()
        {
            var binding = _service.Binding;
            _statusBuilder.Clear();
            _statusBuilder.Append("Phase=").Append(binding != null ? binding.State.ToString() : "Unavailable")
                .Append("  Preparing=").Append(binding != null && binding.IsPreparingSample)
                .Append("  PrepLeft=").Append(binding != null ? binding.PreparationRemainingSeconds.ToString("F2") : "0.00")
                .Append("  Time=").Append(binding != null ? binding.SampleElapsedSeconds.ToString("F2") : "0.00")
                .Append('/').Append(binding != null ? binding.SampleDurationSeconds.ToString("F2") : "0.00")
                .Append("  PendingLeft=").Append(binding != null && !string.IsNullOrEmpty(binding.PendingLeftTrackerId) ? binding.PendingLeftTrackerId : "None")
                .Append("  Candidate=").Append(binding != null && !string.IsNullOrEmpty(binding.CurrentCandidateTrackerId) ? binding.CurrentCandidateTrackerId : "None").Append('\n');

            for (var i = 0; i < _service.ConnectedTrackerCount; i++)
            {
                PicoObjectTrackerPose pose;
                if (!_service.TryGetConnectedTrackerPose(i, out pose)) continue;
                _statusBuilder.Append("Raw[").Append(i).Append("] ID=").Append(pose.trackerId)
                    .Append(" Valid=").Append(pose.poseValid)
                    .Append(" Pos=").Append(pose.position.ToString("F3"))
                    .Append(" Rot=").Append(pose.rotation.eulerAngles.ToString("F1")).Append('\n');
            }

            if (binding != null)
            {
                for (var i = 0; i < binding.SampleTrackerCount; i++)
                {
                    WristBindingSampleDiagnostics sample;
                    if (!binding.TryGetSampleDiagnostics(i, out sample)) continue;
                    _statusBuilder.Append("Sample[").Append(i).Append("] ID=").Append(sample.trackerId)
                        .Append(" Pose=").Append(sample.poseValid)
                        .Append(" Frames=").Append(sample.validSamples)
                        .Append(" Travel=").Append(sample.travelMeters.ToString("F3")).Append("m").Append('\n');
                }

                _statusBuilder.Append("SavedLeft=").Append(string.IsNullOrEmpty(binding.Profile.leftTrackerId) ? "None" : binding.Profile.leftTrackerId)
                    .Append("  SavedRight=").Append(string.IsNullOrEmpty(binding.Profile.rightTrackerId) ? "None" : binding.Profile.rightTrackerId)
                    .Append("  Best/Second=").Append(binding.CurrentBestTravelMeters.ToString("F3"))
                    .Append('/').Append(binding.CurrentSecondTravelMeters.ToString("F3")).Append("m").Append('\n')
                    .Append("LastResult=").Append(string.IsNullOrEmpty(binding.LastResultMessage) ? "None" : binding.LastResultMessage);
            }

            if (!string.IsNullOrEmpty(_service.LastError))
                _statusBuilder.Append("  API Error=").Append(_service.LastError);
        }

        private void ToggleDiagnostics()
        {
            if (_service.DiagnosticsActive) _service.StopDiagnostics(); else _service.StartDiagnostics();
            RefreshNow();
        }

        private void ToggleAdvanced()
        {
            _service.AdvancedDiagnosticsVisible = !_service.AdvancedDiagnosticsVisible;
            RefreshNow();
        }

        private Button CreateActionButton(Transform parent, string objectName, string label, Vector2 position, UnityAction action)
        {
            return CreateActionButton(parent, objectName, label, position, action, new Vector2(206f, 58f));
        }

        private Button CreateActionButton(Transform parent, string objectName, string label, Vector2 position, UnityAction action, Vector2 size)
        {
            var rect = CreateRect(objectName, parent, size, position);
            var surface = CreatePanel(rect, "Surface", size, Vector2.zero, ElderCareMenuDesignTokens.CardHighlight, size.y * 0.45f);
            surface.raycastTarget = true;
            ElderCareMenuPanelBuilder.ConfigureNativeStroke(surface, ElderCareMenuDesignTokens.WithAlpha(ElderCareMenuDesignTokens.GoldStroke, 0.72f), 1.3f);
            CreateText(rect, "Label", label, size - new Vector2(18f, 8f), Vector2.zero, 18f, FontStyles.Bold, TextAlignmentOptions.Center, ElderCareMenuDesignTokens.TextPrimary);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = surface;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = ColorBlock.defaultColorBlock;
            if (action != null) button.onClick.AddListener(action);
            return button;
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

        private static string ModeLabel(RehabTrackingMode mode)
        {
            if (mode == RehabTrackingMode.WristTrackers) return "腕部传感器";
            if (mode == RehabTrackingMode.Controllers) return "手柄";
            return "等待设备";
        }

        private static void SetButtonLabel(Button button, string label)
        {
            var text = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
            if (text != null) text.text = label;
        }
    }
}
