using PicoElderCare.Rehab.Tracking.Pico;
using Unity.XR.CoreUtils;
using Unity.XR.PXR;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PicoElderCare.Rehab.Tracking.Pico.ObjectTracking
{
    [DefaultExecutionOrder(-9000)]
    public sealed class WristTrackingRuntime : MonoBehaviour, IWristTrackerSetupService
    {
        private const string RuntimeObjectName = "WristTrackingRuntime";
        private const string PreferenceKey = "ElderCare.WristTracking.Preference";
        private const float RefreshIntervalSeconds = 0.75f;

        private static WristTrackingRuntime _instance;

        private IPicoObjectTrackingApi _api;
        private WristTrackerBindingManager _binding;
        private WristTrackerCalibration _calibration;
        private PicoWristObjectTrackingProvider _provider;
        private RehabPoseProviderSelector _activeSelector;
        private readonly RehabBodySample _diagnosticSample = new RehabBodySample();
        private float _nextRefreshTime;
        private bool _diagnosticsActive;
        private bool _advancedDiagnosticsVisible;
        private int _providerSwitchCount;
        private GameObject _markerRoot;
        private Transform _headMarker;
        private Transform _leftMarker;
        private Transform _rightMarker;
        private Material _markerMaterial;

        public static WristTrackingRuntime Instance { get { return _instance; } }

        public WristTrackerSetupState State
        {
            get
            {
                if (_provider != null && _provider.SetupState == WristTrackerSetupState.Ready)
                    return WristTrackerSetupState.Ready;
                if (_calibration != null && _calibration.IsCalibrating)
                    return WristTrackerSetupState.Calibrating;
                if (_binding != null && (_binding.IsBindingInProgress || _binding.IsVerificationInProgress))
                    return _binding.State;
                if (_provider != null && _provider.SetupState != WristTrackerSetupState.NoTracker)
                    return _provider.SetupState;
                return _binding != null ? _binding.State : WristTrackerSetupState.ApiError;
            }
        }

        public string StatusMessage
        {
            get
            {
                if (_calibration != null && _calibration.IsCalibrating) return _calibration.StatusMessage;
                if (_binding != null && (_binding.IsBindingInProgress || _binding.IsVerificationInProgress)) return _binding.StatusMessage;
                if (_binding != null && _binding.LastResultFailed && !string.IsNullOrEmpty(_binding.LastResultMessage))
                    return _binding.LastResultMessage;
                if (_provider != null && !string.IsNullOrEmpty(_provider.StatusMessage)) return _provider.StatusMessage;
                return _binding != null ? _binding.StatusMessage : "腕部追踪服务不可用。";
            }
        }

        public RehabTrackingPreference Preference
        {
            get
            {
                return _activeSelector != null
                    ? _activeSelector.Preference
                    : (RehabTrackingPreference)PlayerPrefs.GetInt(PreferenceKey, (int)RehabTrackingPreference.Auto);
            }
            set
            {
                PlayerPrefs.SetInt(PreferenceKey, (int)value);
                PlayerPrefs.Save();
                if (_activeSelector != null) _activeSelector.Preference = value;
            }
        }

        public RehabTrackingMode CurrentTrackingMode
        {
            get
            {
                if (_activeSelector != null) return _activeSelector.CurrentTrackingMode;
                return IsWristTrackingReady ? RehabTrackingMode.WristTrackers : RehabTrackingMode.Controllers;
            }
        }

        public WristTrackerInfo LeftTracker { get { return BuildInfo(true); } }
        public WristTrackerInfo RightTracker { get { return BuildInfo(false); } }
        public int ConnectedTrackerCount { get { return _api != null ? _api.ConnectedTrackerCount : 0; } }
        public bool HasValidBinding { get { return _binding != null && _binding.IsBindingReady; } }
        public bool IsCalibrationReady { get { return _calibration != null && _calibration.IsCalibrationReady; } }
        public bool IsWristTrackingReady { get { return _provider != null && _provider.WristTrackingReady; } }
        public bool IsHmdPoseValid { get { return _provider != null && _provider.HeadPoseValid; } }
        public bool DiagnosticsActive { get { return _diagnosticsActive; } }
        public bool AdvancedDiagnosticsVisible { get { return _advancedDiagnosticsVisible; } set { _advancedDiagnosticsVisible = value; } }
        public int ProviderSwitchCount { get { return _providerSwitchCount; } }
        public string LastError { get { return _api != null ? _api.LastError : string.Empty; } }
        public PicoWristObjectTrackingProvider Provider { get { return _provider; } }
        public WristTrackerBindingManager Binding { get { return _binding; } }
        public WristTrackerCalibration Calibration { get { return _calibration; } }

        public bool TryGetConnectedTrackerId(int index, out string trackerId)
        {
            if (_api != null) return _api.TryGetTrackerId(index, out trackerId);
            trackerId = string.Empty;
            return false;
        }

        public bool TryGetConnectedTrackerPose(int index, out PicoObjectTrackerPose pose)
        {
            pose = default(PicoObjectTrackerPose);
            string trackerId;
            if (_api == null || !_api.TryGetTrackerId(index, out trackerId)) return false;

            // A connected tracker with an invalid pose is still diagnostically
            // meaningful, so return the discovered device even when pose read fails.
            _api.TryGetTrackerPose(trackerId, out pose);
            if (string.IsNullOrEmpty(pose.trackerId)) pose.trackerId = trackerId;
            return true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            EnsureInstance();
        }

        public static WristTrackingRuntime EnsureInstance()
        {
            if (_instance != null) return _instance;

            _instance = FindObjectOfType<WristTrackingRuntime>(true);
            if (_instance != null) return _instance;

            var runtimeObject = new GameObject(RuntimeObjectName);
            return runtimeObject.AddComponent<WristTrackingRuntime>();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            _api = new PicoObjectTrackingApi();
            _binding = new WristTrackerBindingManager(_api);
            _calibration = new WristTrackerCalibration(_api, _binding);
            _provider = GetComponent<PicoWristObjectTrackingProvider>();
            if (_provider == null) _provider = gameObject.AddComponent<PicoWristObjectTrackingProvider>();
            _provider.Configure(_api, _binding, _calibration, null, null);
            _provider.StartTracking();
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void Start()
        {
            ConfigureScene(SceneManager.GetActiveScene());
        }

        private void OnDestroy()
        {
            if (_instance != this) return;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SubscribeSelector(null);
            if (_provider != null) _provider.StopTracking();
            if (_api != null) _api.Dispose();
            if (_markerMaterial != null) Destroy(_markerMaterial);
            _instance = null;
        }

        private void Update()
        {
            if (_api == null) return;

            if (Time.unscaledTime >= _nextRefreshTime)
            {
                _nextRefreshTime = Time.unscaledTime + RefreshIntervalSeconds;
                _api.RefreshTrackers();
            }

            _binding.Tick(Time.unscaledDeltaTime);
            _calibration.Tick(Time.unscaledDeltaTime);

            // In Rehab the selector owns the per-frame read. Querying here as well would
            // hit the native tracker API twice in one frame while diagnostics are visible.
            // MainEntry and the debug scene have no selector, so Runtime samples there.
            if (_activeSelector == null)
            {
                _provider.TryGetSample(_diagnosticSample);
            }

            if (_diagnosticsActive) UpdateMarkers();
        }

        public void RefreshTrackers() { if (_api != null) _api.RefreshTrackers(); }
        public void BeginBinding()
        {
            if (_calibration != null) _calibration.ClearCalibration();
            if (_binding != null) _binding.BeginBinding();
        }
        public void CancelBinding() { if (_binding != null) _binding.CancelBinding(); }
        public void ClearBinding()
        {
            if (_binding != null) _binding.ClearBinding();
            if (_calibration != null) _calibration.ClearCalibration();
        }
        public void BeginQuickVerification() { if (_binding != null) _binding.BeginQuickVerification(); }
        public void BeginCalibration() { if (_calibration != null) _calibration.BeginCalibration(); }
        public void CancelCalibration() { if (_calibration != null) _calibration.CancelCalibration(); }
        public void UseIdentityCalibration() { if (_calibration != null) _calibration.UseIdentityCalibration(); }

        public void StartDiagnostics()
        {
            EnsureMarkers();
            _diagnosticsActive = true;
            if (_markerRoot != null) _markerRoot.SetActive(true);
        }

        public void StopDiagnostics()
        {
            _diagnosticsActive = false;
            if (_markerRoot != null) _markerRoot.SetActive(false);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ConfigureScene(scene);
        }

        private void ConfigureScene(Scene scene)
        {
            var camera = Camera.main;
            var origin = FindObjectOfType<XROrigin>(true);
            _provider.Configure(
                _api,
                _binding,
                _calibration,
                camera != null ? camera.transform : null,
                origin != null ? origin.transform : null);
            if (!_provider.IsRunning) _provider.StartTracking();

            // Keep the old implementation available for A/B work, but make
            // sure it cannot claim the Motion Tracker mode in this runtime.
            var bodyProviders = FindObjectsOfType<PicoBodyTrackingProvider>(true);
            for (var i = 0; i < bodyProviders.Length; i++)
            {
                bodyProviders[i].AutoStartOnEnable = false;
                bodyProviders[i].StopTracking();
                bodyProviders[i].enabled = false;
            }

            var pxrManagers = FindObjectsOfType<PXR_Manager>(true);
            for (var i = 0; i < pxrManagers.Length; i++) pxrManagers[i].bodyTracking = false;

            var selector = FindObjectOfType<RehabPoseProviderSelector>(true);
            SubscribeSelector(selector);
            if (selector != null)
            {
                var controller = FindObjectOfType<ControllerPoseProvider>(true);
                selector.PrimaryProvider = _provider;
                selector.FallbackProvider = controller;
                selector.AllowAutomaticFallback = true;
                selector.Preference = (RehabTrackingPreference)PlayerPrefs.GetInt(
                    PreferenceKey,
                    (int)RehabTrackingPreference.Auto);
                if (!selector.IsRunning) selector.StartTracking();
            }
        }

        private void SubscribeSelector(RehabPoseProviderSelector selector)
        {
            if (_activeSelector == selector) return;
            if (_activeSelector != null) _activeSelector.ProviderChanged -= HandleProviderChanged;
            _activeSelector = selector;
            if (_activeSelector != null) _activeSelector.ProviderChanged += HandleProviderChanged;
        }

        private void HandleProviderChanged(RehabProviderChange change)
        {
            if (change.oldProvider != null && change.newProvider != null) _providerSwitchCount++;
        }

        private WristTrackerInfo BuildInfo(bool left)
        {
            var result = new WristTrackerInfo();
            if (_binding == null) return result;

            result.trackerId = left ? _binding.Profile.leftTrackerId : _binding.Profile.rightTrackerId;
            result.bound = !string.IsNullOrEmpty(result.trackerId);
            PicoObjectTrackerPose pose;
            if (result.bound)
            {
                _api.TryGetTrackerPose(result.trackerId, out pose);
                result.connected = pose.connected;
                result.poseValid = pose.poseValid;
                result.position = pose.position;
                result.rotation = pose.rotation;
                result.velocity = pose.velocity;
                result.lastUpdateAgeSeconds = Mathf.Max(0f, (float)(Time.realtimeSinceStartupAsDouble - pose.timestamp));
            }
            result.stableFrameCount = _provider != null ? _provider.StableFrameCount : 0;
            return result;
        }

        private void EnsureMarkers()
        {
            if (_markerRoot != null) return;
            _markerRoot = new GameObject("WristTrackingDiagnosticsMarkers");
            _markerRoot.transform.SetParent(transform, false);
            _headMarker = CreateMarker("Head", 0.10f);
            _leftMarker = CreateMarker("LeftWrist", 0.075f);
            _rightMarker = CreateMarker("RightWrist", 0.075f);
            _markerRoot.SetActive(false);
        }

        private Transform CreateMarker(string markerName, float size)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = markerName;
            marker.transform.SetParent(_markerRoot.transform, false);
            marker.transform.localScale = Vector3.one * size;
            var collider = marker.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            if (_markerMaterial == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                if (shader != null)
                {
                    _markerMaterial = new Material(shader) { color = new Color(0.95f, 0.58f, 0.18f, 1f) };
                }
            }
            var renderer = marker.GetComponent<Renderer>();
            if (renderer != null && _markerMaterial != null) renderer.sharedMaterial = _markerMaterial;
            return marker.transform;
        }

        private void UpdateMarkers()
        {
            var camera = Camera.main;
            SetMarker(_headMarker, camera != null, camera != null ? camera.transform.position : Vector3.zero);

            var left = _provider != null ? _provider.LeftTrackerPose : default(PicoObjectTrackerPose);
            var right = _provider != null ? _provider.RightTrackerPose : default(PicoObjectTrackerPose);
            SetMarker(_leftMarker, left.connected && left.poseValid, ToWorldPosition(left.position));
            SetMarker(_rightMarker, right.connected && right.poseValid, ToWorldPosition(right.position));
        }

        private Vector3 ToWorldPosition(Vector3 localPosition)
        {
            return _provider != null && _provider.XrOrigin != null
                ? _provider.XrOrigin.TransformPoint(localPosition)
                : localPosition;
        }

        private static void SetMarker(Transform marker, bool valid, Vector3 position)
        {
            if (marker == null) return;
            marker.gameObject.SetActive(valid);
            if (valid) marker.position = position;
        }
    }
}
