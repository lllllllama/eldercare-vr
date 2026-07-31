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

        private bool _isRunning;
        private RehabPoseProviderBase _currentProvider;
        private RehabTrackingState _trackingState = RehabTrackingState.Unavailable;
        private string _statusMessage = NoProviderMessage;

        public event Action<RehabPoseProviderBase, RehabTrackingState> ProviderStatusChanged;

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

        public RehabPoseProviderBase CurrentProvider
        {
            get { return _currentProvider; }
        }

        public override bool IsSupported
        {
            get
            {
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
            StartProvider(primaryProvider);
            if (allowAutomaticFallback && fallbackProvider != primaryProvider)
            {
                StartProvider(fallbackProvider);
            }

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

            if (TryProvider(primaryProvider, target))
            {
                ReportStatus(primaryProvider, target.trackingState, primaryProvider.StatusMessage);
                return true;
            }

            if (allowAutomaticFallback &&
                fallbackProvider != primaryProvider &&
                TryProvider(fallbackProvider, target))
            {
                ReportStatus(fallbackProvider, target.trackingState, fallbackProvider.StatusMessage);
                return true;
            }

            target.Clear();
            var failedState = primaryProvider != null
                ? primaryProvider.TrackingState
                : RehabTrackingState.Unavailable;
            var failedMessage = primaryProvider != null
                ? primaryProvider.StatusMessage
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
                   provider.TryGetSample(target) &&
                   target.IsTrackingUsable;
        }

        private static void StartProvider(RehabPoseProviderBase provider)
        {
            if (provider != null && provider.IsSupported && !provider.IsRunning)
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

        private void ReportStatus(
            RehabPoseProviderBase provider,
            RehabTrackingState state,
            string message)
        {
            var changed = _currentProvider != provider || _trackingState != state;
            _currentProvider = provider;
            _trackingState = state;
            _statusMessage = string.IsNullOrEmpty(message) ? NoProviderMessage : message;

            if (changed)
            {
                var handler = ProviderStatusChanged;
                if (handler != null)
                {
                    handler(_currentProvider, _trackingState);
                }
            }
        }
    }
}
