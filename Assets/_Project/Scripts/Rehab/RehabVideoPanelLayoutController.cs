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

        public float panelDistance = 1.8f;
        public float videoRightOffset = 0.75f;
        public float heightOffset = -0.05f;
        public float videoWidth = 0.62f;
        public float videoHeight = 0.35f;
        public bool preferPromptCanvasLayout = true;
        public bool faceUser = true;

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
            var forward = GetHeadYawForward();
            var right = Vector3.Cross(Vector3.up, forward).normalized;
            if (right.sqrMagnitude < 0.0001f)
            {
                right = Vector3.right;
            }

            var baseCenter = headPosition + forward * Mathf.Max(0.1f, panelDistance);
            if (preferPromptCanvasLayout && promptCanvas != null)
            {
                baseCenter = promptCanvas.position;
            }
            else if (trainingAreaRoot != null)
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

        private void ApplyVideoSize()
        {
            if (videoQuad != null)
            {
                videoQuad.localScale = new Vector3(videoWidth, videoHeight, 1f);
            }
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
                    promptCanvas = sessionManager.promptCanvas;
                    trainingAreaRoot = trainingAreaRoot != null ? trainingAreaRoot : sessionManager.trainingAreaRoot;
                }
            }
        }
    }
}
