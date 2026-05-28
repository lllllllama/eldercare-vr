using UnityEngine;

namespace PicoElderCare.Rehab
{
    public class RehabVideoPanelLayoutController : MonoBehaviour
    {
        public Transform panelRoot;
        public Transform videoQuad;
        public Transform headTransform;
        public Transform promptCanvas;
        public Transform trainingAreaRoot;

        public float panelDistance = 2.15f;
        public float videoRightOffset = 0.9f;
        public float heightOffset = 0.08f;
        public float videoWidth = 0.78f;
        public float videoHeight = 0.44f;
        public float videoScale = 1f;
        public float minVideoScale = 0.65f;
        public float maxVideoScale = 1.7f;
        public float videoScaleStep = 0.12f;
        public float minPanelDistance = 0.95f;
        public float maxPanelDistance = 3.25f;
        public float minHeightFromHead = -0.55f;
        public float maxHeightFromHead = 0.45f;
        public bool preferPromptCanvasLayout = false;
        public bool followTrainingAreaRoot = false;
        public bool preserveUserPlacement = true;
        public bool faceUser = true;
        public bool lockManualMoveHeight = true;

        private RehabSessionManager _sessionManager;
        private bool _hasUserPlacement;

        public Vector3 PanelPosition
        {
            get { return panelRoot != null ? panelRoot.position : transform.position; }
        }

        private void Reset()
        {
            ResolveReferences();
            ApplyVideoSize();
        }

        public void PlaceInRightFrontOfUserOnce()
        {
            ResolveReferences();
            ApplyVideoSize();

            if (panelRoot == null) return;

            var headPosition = headTransform != null
                ? headTransform.position
                : new Vector3(0f, 1.5f, 0f);

            if (preserveUserPlacement && _hasUserPlacement)
            {
                if (faceUser)
                {
                    FaceHeadYaw(headPosition);
                }

                return;
            }

            var forward = GetHeadYawForward();
            var right = Vector3.Cross(Vector3.up, forward).normalized;
            if (right.sqrMagnitude < 0.0001f)
            {
                right = Vector3.right;
            }

            var baseCenter = headPosition + forward * Mathf.Max(0.1f, panelDistance);
            if (preferPromptCanvasLayout && promptCanvas != null && !IsPromptCanvasBoundToTrainingArea())
            {
                baseCenter = promptCanvas.position;
            }
            else if (followTrainingAreaRoot && trainingAreaRoot != null)
            {
                var trainingToHead = headPosition - trainingAreaRoot.position;
                trainingToHead.y = 0f;
                if (trainingToHead.sqrMagnitude > 0.0001f)
                {
                    baseCenter = trainingAreaRoot.position - trainingToHead.normalized * 0.2f + Vector3.up * headPosition.y;
                }
            }

            var videoPosition = baseCenter + right * videoRightOffset + Vector3.up * heightOffset;
            panelRoot.position = videoPosition;

            if (faceUser)
            {
                FaceHeadYaw(headPosition);
            }
        }

        public void ResetVideoPlacement()
        {
            _hasUserPlacement = false;
            PlaceInRightFrontOfUserOnce();
        }

        public void MoveVideoToWorldPoint(Vector3 targetPosition, Vector3 headPosition)
        {
            ResolveReferences();
            if (panelRoot == null) return;

            if (lockManualMoveHeight)
            {
                targetPosition.y = panelRoot.position.y;
            }

            var constrained = ConstrainPanelPosition(targetPosition, headPosition);
            panelRoot.position = constrained;
            _hasUserPlacement = true;

            if (faceUser)
            {
                FaceHeadYaw(headPosition);
            }
        }

        public void AdjustVideoScale(float delta)
        {
            SetVideoScale(videoScale + delta);
        }

        public void ScaleVideoUp()
        {
            AdjustVideoScale(Mathf.Abs(videoScaleStep));
        }

        public void ScaleVideoDown()
        {
            AdjustVideoScale(-Mathf.Abs(videoScaleStep));
        }

        public void SetVideoScale(float scale)
        {
            videoScale = Mathf.Clamp(scale, Mathf.Max(0.01f, minVideoScale), Mathf.Max(minVideoScale, maxVideoScale));
            ApplyVideoSize();
        }

        public void ApplyVideoSize()
        {
            videoScale = Mathf.Clamp(videoScale, Mathf.Max(0.01f, minVideoScale), Mathf.Max(minVideoScale, maxVideoScale));
            if (videoQuad != null)
            {
                videoQuad.localScale = new Vector3(videoWidth * videoScale, videoHeight * videoScale, 1f);
            }
        }

        private Vector3 ConstrainPanelPosition(Vector3 targetPosition, Vector3 headPosition)
        {
            var horizontal = targetPosition - headPosition;
            horizontal.y = 0f;
            if (horizontal.sqrMagnitude < 0.0001f)
            {
                horizontal = GetHeadYawForward() * Mathf.Max(0.1f, panelDistance);
            }

            var distance = Mathf.Clamp(
                horizontal.magnitude,
                Mathf.Max(0.1f, minPanelDistance),
                Mathf.Max(minPanelDistance, maxPanelDistance));
            horizontal = horizontal.normalized * distance;

            targetPosition.x = headPosition.x + horizontal.x;
            targetPosition.z = headPosition.z + horizontal.z;
            targetPosition.y = Mathf.Clamp(
                targetPosition.y,
                headPosition.y + minHeightFromHead,
                headPosition.y + maxHeightFromHead);

            return targetPosition;
        }

        private Vector3 GetHeadYawForward()
        {
            if (headTransform == null)
            {
                return Vector3.forward;
            }

            var forward = Vector3.ProjectOnPlane(headTransform.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }

            return forward.normalized;
        }

        private void FaceHeadYaw(Vector3 headPosition)
        {
            if (panelRoot == null) return;

            var awayFromUser = panelRoot.position - headPosition;
            awayFromUser.y = 0f;
            if (awayFromUser.sqrMagnitude < 0.0001f)
            {
                awayFromUser = GetHeadYawForward();
            }

            panelRoot.rotation = Quaternion.LookRotation(awayFromUser.normalized, Vector3.up);
        }

        private bool IsPromptCanvasBoundToTrainingArea()
        {
            return _sessionManager != null && _sessionManager.placePromptCanvasWithTrainingArea;
        }

        private void ResolveReferences()
        {
            if (panelRoot == null)
            {
                panelRoot = transform;
            }

            if (videoQuad == null)
            {
                var quad = transform.Find("VideoQuad");
                if (quad != null)
                {
                    videoQuad = quad;
                }
            }

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

            if (promptCanvas == null)
            {
                var sessionManager = FindObjectOfType<RehabSessionManager>(true);
                if (sessionManager != null)
                {
                    _sessionManager = sessionManager;
                    promptCanvas = sessionManager.promptCanvas;
                    trainingAreaRoot = trainingAreaRoot != null ? trainingAreaRoot : sessionManager.trainingAreaRoot;
                }
            }
            else if (_sessionManager == null)
            {
                _sessionManager = FindObjectOfType<RehabSessionManager>(true);
            }
        }
    }
}
