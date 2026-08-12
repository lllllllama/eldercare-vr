using UnityEngine;

namespace PicoElderCare.Rehab
{
    public class RehabPanelPlacementController : MonoBehaviour
    {
        public Transform headTransform;
        public Transform viewTransform;
        public Transform selectionPanelRoot;
        public Transform trainingLayoutAnchor;
        public Transform trainingFunctionPanelRoot;
        public Transform resultPanelRoot;
        public Transform promptPanelRoot;
        public Transform videoPanelRoot;
        public RehabVideoPanelLayoutController videoLayoutController;
        public bool useSceneAuthoredTrainingLayout = true;
        public bool placeOnStart = false;
        public float selectionPanelDistance = 1.35f;
        public float selectionPanelHeight = 1.35f;
        public float trainingLayoutDistance = 1.8f;
        public float trainingLayoutHeightOffset = -0.1f;
        public float resultPanelDistance = 2f;
        public float resultPanelHeightOffset = -0.1f;
        public bool clampResultPanelHeight = true;
        public float minResultPanelHeight = 1.25f;
        public float maxResultPanelHeight = 1.75f;
        public float promptPanelDistance = 1.8f;
        public float videoPanelDistance = 2.2f;
        public float videoPanelYawOffsetDegrees = 40f;
        public float panelHeight = 1.45f;
        [Range(0.5f, 1.2f)] public float compactTrainingFunctionScale = 0.84f;
        public float minPanelSeparationMeters = 0.25f;

        private bool _hasPlacedPanels;
        private Transform _capturedTrainingLayoutAnchor;
        private Vector3 _trainingFunctionLocalPosition;
        private Quaternion _trainingFunctionLocalRotation;
        private Vector3 _videoLocalPosition;
        private Quaternion _videoLocalRotation;
        private bool _hasCapturedTrainingLayoutOffsets;

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
            if (viewTransform == null) return;

            if (useSceneAuthoredTrainingLayout)
            {
                RecenterSelectionPanelInternal();
                _hasPlacedPanels = true;
                return;
            }

            var headPosition = viewTransform.position;
            var forward = GetViewYawForward();
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

        public void RecenterSelectionPanel()
        {
            ResolveReferences();
            if (viewTransform == null) return;

            RecenterSelectionPanelInternal();
            _hasPlacedPanels = true;
        }

        public void RecenterTrainingLayout()
        {
            ResolveReferences();
            if (viewTransform == null || trainingLayoutAnchor == null) return;

            CaptureExternalTrainingLayoutOffsetsIfNeeded();

            var viewPosition = viewTransform.position;
            var forward = GetViewYawForward();
            var position = viewPosition + forward * Mathf.Max(0.1f, trainingLayoutDistance);
            position.y = viewPosition.y + trainingLayoutHeightOffset;

            trainingLayoutAnchor.position = position;
            trainingLayoutAnchor.rotation = CreatePanelRotation(position, viewPosition, forward);
            ApplyExternalTrainingLayoutOffsets();
            _hasPlacedPanels = true;
        }

        public void RecenterResultPanel()
        {
            ResolveReferences();
            if (viewTransform == null || resultPanelRoot == null) return;

            var viewPosition = viewTransform.position;
            var forward = GetViewYawForward();
            var position = viewPosition + forward * Mathf.Max(0.1f, resultPanelDistance);
            position.y = viewPosition.y + resultPanelHeightOffset;
            if (clampResultPanelHeight)
            {
                var minimum = Mathf.Min(minResultPanelHeight, maxResultPanelHeight);
                var maximum = Mathf.Max(minResultPanelHeight, maxResultPanelHeight);
                position.y = Mathf.Clamp(position.y, minimum, maximum);
            }

            resultPanelRoot.position = position;
            resultPanelRoot.rotation = CreatePanelRotation(position, viewPosition, forward);
            _hasPlacedPanels = true;
        }

        public void ResolveReferences()
        {
            if (viewTransform == null && headTransform != null)
            {
                viewTransform = headTransform;
            }

            if (viewTransform == null)
            {
                var tracker = FindObjectOfType<HandPoseTracker>(true);
                if (tracker != null)
                {
                    viewTransform = tracker.hmdTransform;
                }
            }

            if (viewTransform == null && Camera.main != null)
            {
                viewTransform = Camera.main.transform;
            }

            if (headTransform == null)
            {
                headTransform = viewTransform;
            }

            if (selectionPanelRoot == null)
            {
                var modeSelectUi = FindObjectOfType<RehabModeSelectUI>(true);
                if (modeSelectUi != null && modeSelectUi.SelectionPanelRoot != null)
                {
                    selectionPanelRoot = modeSelectUi.SelectionPanelRoot.transform;
                }
            }

            if (trainingFunctionPanelRoot == null)
            {
                var modeSelectUi = FindObjectOfType<RehabModeSelectUI>(true);
                if (modeSelectUi != null && modeSelectUi.TrainingFunctionPanelRoot != null)
                {
                    trainingFunctionPanelRoot = modeSelectUi.TrainingFunctionPanelRoot.transform;
                }
            }

            if (resultPanelRoot == null)
            {
                var modeSelectUi = FindObjectOfType<RehabModeSelectUI>(true);
                if (modeSelectUi != null && modeSelectUi.ResultPanelRoot != null)
                {
                    resultPanelRoot = modeSelectUi.ResultPanelRoot.transform;
                }
            }

            if (promptPanelRoot == null && selectionPanelRoot != null)
            {
                promptPanelRoot = selectionPanelRoot;
            }

            if (promptPanelRoot == null)
            {
                var session = FindObjectOfType<RehabSessionManager>(true);
                if (session != null)
                {
                    promptPanelRoot = session.promptCanvas;
                }
            }

            if (selectionPanelRoot == null && promptPanelRoot != null)
            {
                selectionPanelRoot = promptPanelRoot;
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

            if (trainingLayoutAnchor == null)
            {
                if (trainingFunctionPanelRoot != null && videoPanelRoot != null &&
                    trainingFunctionPanelRoot.parent == videoPanelRoot.parent)
                {
                    trainingLayoutAnchor = trainingFunctionPanelRoot.parent;
                }
                else
                {
                    var anchorObject = GameObject.Find("TrainingLayoutAnchor");
                    if (anchorObject != null)
                    {
                        trainingLayoutAnchor = anchorObject.transform;
                    }
                }
            }

            if (trainingLayoutAnchor == null && useSceneAuthoredTrainingLayout && trainingFunctionPanelRoot != null)
            {
                var anchorObject = new GameObject("TrainingLayoutAnchor");
                trainingLayoutAnchor = anchorObject.transform;
                var authoredRoot = videoPanelRoot != null ? videoPanelRoot : trainingFunctionPanelRoot;
                trainingLayoutAnchor.position = authoredRoot.position;
                trainingLayoutAnchor.rotation = authoredRoot.rotation;
            }

            if (videoLayoutController != null)
            {
                videoLayoutController.headTransform = viewTransform;
                if (videoPanelRoot != null)
                {
                    videoLayoutController.panelRoot = videoPanelRoot;
                }
            }
        }

        private void RecenterSelectionPanelInternal()
        {
            var target = selectionPanelRoot != null ? selectionPanelRoot : promptPanelRoot;
            if (target == null || viewTransform == null) return;

            var headPosition = viewTransform.position;
            var forward = GetViewYawForward();
            var position = headPosition + forward * Mathf.Max(0.1f, selectionPanelDistance);
            position.y = selectionPanelHeight;
            target.position = position;
            target.rotation = CreatePanelRotation(position, headPosition, forward);
        }

        private void CaptureExternalTrainingLayoutOffsetsIfNeeded()
        {
            if (trainingLayoutAnchor == null) return;
            if (_hasCapturedTrainingLayoutOffsets && _capturedTrainingLayoutAnchor == trainingLayoutAnchor) return;

            if (trainingFunctionPanelRoot != null && !trainingFunctionPanelRoot.IsChildOf(trainingLayoutAnchor))
            {
                _trainingFunctionLocalPosition = trainingLayoutAnchor.InverseTransformPoint(trainingFunctionPanelRoot.position);
                _trainingFunctionLocalRotation = Quaternion.Inverse(trainingLayoutAnchor.rotation) * trainingFunctionPanelRoot.rotation;
            }

            if (videoPanelRoot != null && !videoPanelRoot.IsChildOf(trainingLayoutAnchor))
            {
                _videoLocalPosition = trainingLayoutAnchor.InverseTransformPoint(videoPanelRoot.position);
                _videoLocalRotation = Quaternion.Inverse(trainingLayoutAnchor.rotation) * videoPanelRoot.rotation;
            }

            _capturedTrainingLayoutAnchor = trainingLayoutAnchor;
            _hasCapturedTrainingLayoutOffsets = true;
        }

        private void ApplyExternalTrainingLayoutOffsets()
        {
            if (!_hasCapturedTrainingLayoutOffsets || trainingLayoutAnchor == null) return;

            if (trainingFunctionPanelRoot != null && !trainingFunctionPanelRoot.IsChildOf(trainingLayoutAnchor))
            {
                trainingFunctionPanelRoot.position = trainingLayoutAnchor.TransformPoint(_trainingFunctionLocalPosition);
                trainingFunctionPanelRoot.rotation = trainingLayoutAnchor.rotation * _trainingFunctionLocalRotation;
            }

            if (videoPanelRoot == null || videoPanelRoot.IsChildOf(trainingLayoutAnchor)) return;

            var videoPosition = trainingLayoutAnchor.TransformPoint(_videoLocalPosition);
            var videoRotation = trainingLayoutAnchor.rotation * _videoLocalRotation;
            if (videoLayoutController != null)
            {
                videoLayoutController.ApplyExternalPanelPose(videoPosition, videoRotation, true);
            }
            else
            {
                videoPanelRoot.position = videoPosition;
                videoPanelRoot.rotation = videoRotation;
            }
        }

        private Vector3 GetViewYawForward()
        {
            var forward = viewTransform != null
                ? Vector3.ProjectOnPlane(viewTransform.forward, Vector3.up)
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
