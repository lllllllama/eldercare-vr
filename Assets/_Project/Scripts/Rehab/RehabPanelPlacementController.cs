using UnityEngine;

namespace PicoElderCare.Rehab
{
    public class RehabPanelPlacementController : MonoBehaviour
    {
        public Transform headTransform;
        public Transform promptPanelRoot;
        public Transform videoPanelRoot;
        public RehabVideoPanelLayoutController videoLayoutController;
        public bool placeOnStart = false;
        public float promptPanelDistance = 1.8f;
        public float videoPanelDistance = 2.2f;
        public float videoPanelYawOffsetDegrees = 40f;
        public float panelHeight = 1.45f;

        private bool _hasPlacedPanels;

        public bool HasPlacedPanels
        {
            get { return _hasPlacedPanels; }
        }

        private void Start()
        {
            ResolveReferences();
            if (placeOnStart)
            {
                RecenterPanels();
            }
        }

        public void PlacePanelsIfNeeded()
        {
            if (_hasPlacedPanels) return;
            RecenterPanels();
        }

        public void RecenterPanels()
        {
            ResolveReferences();
            if (headTransform == null) return;

            var headPosition = headTransform.position;
            var forward = GetHeadYawForward();
            var promptPosition = headPosition + forward * Mathf.Max(0.1f, promptPanelDistance);
            promptPosition.y = panelHeight;

            if (promptPanelRoot != null)
            {
                promptPanelRoot.position = promptPosition;
                promptPanelRoot.rotation = CreatePanelRotation(promptPosition, headPosition, forward);
            }

            var videoDirection = Quaternion.AngleAxis(videoPanelYawOffsetDegrees, Vector3.up) * forward;
            if (videoDirection.sqrMagnitude < 0.0001f)
            {
                videoDirection = forward;
            }

            videoDirection.Normalize();
            var videoPosition = headPosition + videoDirection * Mathf.Max(0.1f, videoPanelDistance);
            videoPosition.y = panelHeight;
            var videoRotation = CreatePanelRotation(videoPosition, headPosition, videoDirection);

            if (videoLayoutController != null)
            {
                videoLayoutController.ApplyExternalPanelPose(videoPosition, videoRotation, true);
            }
            else if (videoPanelRoot != null)
            {
                videoPanelRoot.position = videoPosition;
                videoPanelRoot.rotation = videoRotation;
            }

            _hasPlacedPanels = true;
        }

        public void ResolveReferences()
        {
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

            if (promptPanelRoot == null)
            {
                var session = FindObjectOfType<RehabSessionManager>(true);
                if (session != null)
                {
                    promptPanelRoot = session.promptCanvas;
                }
            }

            if (videoLayoutController == null)
            {
                videoLayoutController = FindObjectOfType<RehabVideoPanelLayoutController>(true);
            }

            if (videoPanelRoot == null)
            {
                if (videoLayoutController != null && videoLayoutController.panelRoot != null)
                {
                    videoPanelRoot = videoLayoutController.panelRoot;
                }
                else
                {
                    var videoGuide = FindObjectOfType<RehabVideoGuideController>(true);
                    if (videoGuide != null && videoGuide.videoPanel != null)
                    {
                        videoPanelRoot = videoGuide.videoPanel.transform;
                    }
                }
            }

            if (videoLayoutController != null)
            {
                videoLayoutController.headTransform = headTransform;
                if (videoPanelRoot != null)
                {
                    videoLayoutController.panelRoot = videoPanelRoot;
                }
            }
        }

        private Vector3 GetHeadYawForward()
        {
            var forward = headTransform != null
                ? Vector3.ProjectOnPlane(headTransform.forward, Vector3.up)
                : Vector3.forward;

            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }

            return forward.normalized;
        }

        private static Quaternion CreatePanelRotation(Vector3 panelPosition, Vector3 headPosition, Vector3 fallbackForward)
        {
            var awayFromUser = panelPosition - headPosition;
            awayFromUser.y = 0f;
            if (awayFromUser.sqrMagnitude < 0.0001f)
            {
                awayFromUser = fallbackForward;
            }

            awayFromUser.y = 0f;
            if (awayFromUser.sqrMagnitude < 0.0001f)
            {
                awayFromUser = Vector3.forward;
            }

            return Quaternion.LookRotation(awayFromUser.normalized, Vector3.up);
        }
    }
}
