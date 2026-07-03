using UnityEngine;

namespace PicoElderCare.Rehab
{
    public class TrainingCircleAnchor : MonoBehaviour
    {
        public Transform headTransform;
        public Transform trainingAreaRoot;
        public float fallbackFloorY = 0f;
        public bool useRaycastFloorHeight = true;
        public float floorRaycastUpMeters = 1.2f;
        public float floorRaycastDownMeters = 2.8f;
        public float minimumFloorNormalY = 0.65f;
        public float maximumFloorHeightOffsetMeters = 0.35f;
        public LayerMask floorMask = ~0;

        private static readonly RaycastHit[] FloorHits = new RaycastHit[16];

        private Vector3 _trainingCenter;
        private bool _hasTrainingCenter;

        public Vector3 TrainingCenter
        {
            get
            {
                if (_hasTrainingCenter) return _trainingCenter;
                return trainingAreaRoot != null ? trainingAreaRoot.position : new Vector3(0f, fallbackFloorY, 0f);
            }
        }

        public bool HasTrainingCenter
        {
            get { return _hasTrainingCenter; }
        }

        private void Awake()
        {
            ResolveReferences();
            if (trainingAreaRoot != null)
            {
                _trainingCenter = trainingAreaRoot.position;
                _hasTrainingCenter = true;
            }
        }

        public Vector3 RecenterToUser()
        {
            ResolveReferences();

            var headPosition = headTransform != null
                ? headTransform.position
                : TrainingCenter;

            var center = new Vector3(
                headPosition.x,
                ResolveFloorY(headPosition),
                headPosition.z);

            SetTrainingCenter(center);
            return _trainingCenter;
        }

        public void SetTrainingCenter(Vector3 center)
        {
            center.y = ResolveFloorY(center);
            _trainingCenter = center;
            _hasTrainingCenter = true;

            if (trainingAreaRoot != null)
            {
                trainingAreaRoot.position = _trainingCenter;
                trainingAreaRoot.rotation = Quaternion.identity;
            }
        }

        public float GetHorizontalDistanceToUser()
        {
            ResolveReferences();
            if (headTransform == null) return float.PositiveInfinity;
            return SafetyMonitor.CalculateHorizontalDistance(headTransform.position, TrainingCenter);
        }

        public bool IsUserInside(float radiusMeters)
        {
            return GetHorizontalDistanceToUser() <= Mathf.Max(0.1f, radiusMeters);
        }

        public bool IsPositionInside(Vector3 position, float radiusMeters)
        {
            return SafetyMonitor.CalculateHorizontalDistance(position, TrainingCenter) <= Mathf.Max(0.1f, radiusMeters);
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
        }

        private float ResolveFloorY(Vector3 referencePosition)
        {
            var floorY = trainingAreaRoot != null
                ? trainingAreaRoot.position.y
                : fallbackFloorY;

            if (!useRaycastFloorHeight)
            {
                return floorY;
            }

            var origin = referencePosition + Vector3.up * Mathf.Max(0.01f, floorRaycastUpMeters);
            var maxDistance = Mathf.Max(0.01f, floorRaycastUpMeters + floorRaycastDownMeters);
            var hitCount = Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                FloorHits,
                maxDistance,
                floorMask,
                QueryTriggerInteraction.Ignore);

            var bestDistance = float.MaxValue;
            var foundFloor = false;
            for (var i = 0; i < hitCount; i++)
            {
                var hit = FloorHits[i];
                FloorHits[i] = default;
                if (hit.collider == null) continue;
                if (hit.normal.y < Mathf.Clamp01(minimumFloorNormalY)) continue;
                if (Mathf.Abs(hit.point.y - floorY) > Mathf.Max(0f, maximumFloorHeightOffsetMeters)) continue;
                if (hit.distance >= bestDistance) continue;

                bestDistance = hit.distance;
                floorY = hit.point.y;
                foundFloor = true;
            }

            if (foundFloor)
            {
                return floorY;
            }

            return floorY;
        }
    }
}
