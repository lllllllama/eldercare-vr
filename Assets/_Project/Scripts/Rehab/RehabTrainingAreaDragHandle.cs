using UnityEngine;

namespace PicoElderCare.Rehab
{
    public class RehabTrainingAreaDragHandle : MonoBehaviour
    {
        public bool allowUserPlacementDrag = false;
        public RehabSessionManager sessionManager;
        public Transform trainingAreaRoot;
        public Transform controllerTransform;
        public Transform hmdTransform;
        public float activationRadiusMeters = 0.95f;
        public float maxRayDistanceMeters = 4.5f;
        public float floorY = 0f;

        public bool IsDragging
        {
            get { return false; }
        }

        private void Awake()
        {
            ApplyInteractionState();
        }

        private void OnEnable()
        {
            ApplyInteractionState();
        }

        private void Update()
        {
            if (allowUserPlacementDrag)
            {
                ApplyInteractionState();
            }
        }

        public void ApplyInteractionState()
        {
            allowUserPlacementDrag = false;
            SetHandleVisualsAndColliders(false);
        }

        private void SetHandleVisualsAndColliders(bool active)
        {
            var renderers = GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].enabled = active;
                }
            }

            var colliders = GetComponentsInChildren<Collider>(true);
            for (var i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    colliders[i].enabled = active;
                }
            }
        }
    }
}
