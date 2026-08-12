using System;
using UnityEngine;

namespace PicoElderCare.Rehab.Tracking
{
    public sealed class RehabPoseProviderSelector : RehabPoseProviderBase
    {
        private const string NoProviderMessage = "No usable rehab pose provider is available.";

        [SerializeField] private RehabPoseProviderBase primaryProvider;
        [SerializeField] private RehabPoseProviderBase fallbackProvider;
        [SerializeField] private bool allowAutomaticFallback = true;
        [SerializeField] private RehabTrackingPreference preference = RehabTrackingPreference.Auto;

        private bool _isRunning;
        private RehabPoseProviderBase _currentProvider;
        private RehabTrackingState _trackingState = RehabTrackingState.Unavailable;
        private string _statusMessage = NoProviderMessage;

        public event Action<RehabPoseProviderBase, RehabTrackingState> ProviderStatusChanged;
        public event Action<RehabProviderChange> ProviderChanged;

        public RehabPoseProviderBase PrimaryProvider
        {
            get { return primaryProvider; }
            set { primaryProvider = value; }
        }

        public RehabPoseProviderBase FallbackProvider
        {
            get { return fallbackProvider; }
            set { fallbackProvider = value; }
        }

        public bool AllowAutomaticFallback
        {
            get { return allowAutomaticFallback; }
            set { allowAutomaticFallback = value; }
        }

        public RehabTrackingPreference Preference
        {
            get { return preference; }
            set
            {
                if (preference == value) return;
                preference = value;
                if (_isRunning)
                {
                    EnsureProvidersForPreference();
                }
            }
        }

        public RehabPoseProviderBase CurrentProvider
        {
            get { return _currentProvider; }
        }

        public RehabTrackingMode CurrentTrackingMode { get; private set; }

        public override bool IsSupported
        {
            get
            {
                if (preference == RehabTrackingPreference.ControllersOnly)
                {
                    return fallbackProvider != null && fallbackProvider.IsSupported;
                }

                if (preference == RehabTrackingPreference.WristTrackersOnly)
                {
                    return primaryProvider != null && primaryProvider.IsSupported;
                }

                return (primaryProvider != null && primaryProvider.IsSupported) ||
                       (allowAutomaticFallback && fallbackProvider != null && fallbackProvider.IsSupported);
            }
        }

        public override bool IsRunning
        {
            get { return _isRunning; }
        }

        public override RehabTrackingState TrackingState
        {
            get { return _trackingState; }
        }

        public override string StatusMessage
        {
            get { return _statusMessage; }
        }

        public override void StartTracking()
        {
            if (_isRunning)
            {
                return;
            }

            _isRunning = true;
            EnsureProvidersForPreference();

            ReportStatus(null, RehabTrackingState.Starting, NoProviderMessage);
        }

        public override void StopTracking()
        {
            StopProvider(primaryProvider);
            if (fallbackProvider != primaryProvider)
            {
                StopProvider(fallbackProvider);
            }

            _isRunning = false;
            ReportStatus(null, RehabTrackingState.Unavailable, NoProviderMessage);
        }

        public override bool TryGetSample(RehabBodySample target)
        {
            if (target == null)
            {
                return false;
            }

            if (!_isRunning)
            {
                target.Clear();
                ReportStatus(null, RehabTrackingState.Unavailable, NoProviderMessage);
                return false;
            }

            if (preference != RehabTrackingPreference.ControllersOnly &&
                TryProvider(primaryProvider, target))
            {
                ReportStatus(primaryProvider, target.trackingState, primaryProvider.StatusMessage);
                return true;
            }

            if (preference != RehabTrackingPreference.WristTrackersOnly &&
                (preference == RehabTrackingPreference.ControllersOnly || allowAutomaticFallback) &&
                fallbackProvider != primaryProvider &&
                TryProvider(fallbackProvider, target))
            {
                ReportStatus(fallbackProvider, target.trackingState, fallbackProvider.StatusMessage);
                return true;
            }

            target.Clear();
            var expectedProvider = preference == RehabTrackingPreference.ControllersOnly
                ? fallbackProvider
                : primaryProvider;
            var failedState = expectedProvider != null
                ? expectedProvider.TrackingState
                : RehabTrackingState.Unavailable;
            var failedMessage = expectedProvider != null
                ? expectedProvider.StatusMessage
                : NoProviderMessage;
            target.trackingState = failedState;
            ReportStatus(null, failedState, failedMessage);
            return false;
        }

        private static bool TryProvider(RehabPoseProviderBase provider, RehabBodySample target)
        {
            return provider != null &&
                   provider.IsSupported &&
                   provider.IsRunning &&
                   provider.TryGetSample(target);
        }

        private static void StartProvider(RehabPoseProviderBase provider)
        {
            if (provider != null && !provider.IsRunning)
            {
                provider.StartTracking();
            }
        }

        private static void StopProvider(RehabPoseProviderBase provider)
        {
            if (provider != null && provider.IsRunning)
            {
                provider.StopTracking();
            }
        }

        private void EnsureProvidersForPreference()
        {
            if (preference != RehabTrackingPreference.ControllersOnly)
            {
                StartProvider(primaryProvider);
            }

            if (preference != RehabTrackingPreference.WristTrackersOnly &&
                fallbackProvider != primaryProvider)
            {
                StartProvider(fallbackProvider);
            }
        }

        private void ReportStatus(
            RehabPoseProviderBase provider,
            RehabTrackingState state,
            string message)
        {
            var oldProvider = _currentProvider;
            var oldMode = CurrentTrackingMode;
            var newMode = GetMode(provider);
            var providerChanged = oldProvider != provider || oldMode != newMode;
            var statusChanged = providerChanged || _trackingState != state;
            _currentProvider = provider;
            CurrentTrackingMode = newMode;
            _trackingState = state;
            _statusMessage = string.IsNullOrEmpty(message) ? NoProviderMessage : message;

            if (providerChanged)
            {
                var providerChangedHandler = ProviderChanged;
                if (providerChangedHandler != null)
                {
                    providerChangedHandler(new RehabProviderChange(oldProvider, provider, oldMode, newMode));
                }
            }

            if (statusChanged)
            {
                var handler = ProviderStatusChanged;
                if (handler != null)
                {
                    handler(_currentProvider, _trackingState);
                }
            }
        }

        private RehabTrackingMode GetMode(RehabPoseProviderBase provider)
        {
            if (provider == null) return RehabTrackingMode.Unavailable;
            if (provider == primaryProvider) return RehabTrackingMode.WristTrackers;
            if (provider == fallbackProvider) return RehabTrackingMode.Controllers;
            return RehabTrackingMode.Unavailable;
        }
    }
}
