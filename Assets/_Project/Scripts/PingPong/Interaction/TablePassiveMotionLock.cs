using UnityEngine;

[DefaultExecutionOrder(200)]
public class TablePassiveMotionLock : MonoBehaviour
{
    public TableDragHandle dragHandle;
    public float restoreThreshold = 0.002f;
    public bool normalizeTableHeightOnEnable = true;
    public float standardTableTopHeight = PingPongGeometry.TableTopHeight;

    private Rigidbody _rigidbody;
    private Vector3 _acceptedPosition;
    private Quaternion _acceptedRotation;

    private void OnEnable()
    {
        _rigidbody = GetComponent<Rigidbody>();
        NormalizeTableHeightIfNeeded();
        _acceptedPosition = transform.position;
        _acceptedRotation = transform.rotation;
        StabilizeRigidbody();

        if (dragHandle != null)
        {
            dragHandle.SyncHeightDependentValues();
        }
    }

    private void LateUpdate()
    {
        StabilizeRigidbody();

        if (dragHandle != null && dragHandle.IsDragging)
        {
            _acceptedPosition = transform.position;
            _acceptedRotation = transform.rotation;
            return;
        }

        if ((transform.position - _acceptedPosition).sqrMagnitude > restoreThreshold * restoreThreshold || Quaternion.Angle(transform.rotation, _acceptedRotation) > 0.1f)
        {
            transform.SetPositionAndRotation(_acceptedPosition, _acceptedRotation);
            StabilizeRigidbody();
        }
    }

    public void AcceptCurrentTransform()
    {
        _acceptedPosition = transform.position;
        _acceptedRotation = transform.rotation;
        StabilizeRigidbody();
    }

    private void StabilizeRigidbody()
    {
        if (_rigidbody == null)
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        if (_rigidbody == null) return;

        if (!_rigidbody.isKinematic)
        {
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }

        _rigidbody.isKinematic = true;
        _rigidbody.useGravity = false;
        _rigidbody.constraints = RigidbodyConstraints.FreezeAll;
    }

    public void NormalizeTableHeightIfNeeded()
    {
        if (!normalizeTableHeightOnEnable) return;

        var targetY = Mathf.Max(0.1f, standardTableTopHeight) - PingPongGeometry.TableThickness * 0.5f;
        var position = transform.position;
        if (Mathf.Abs(position.y - targetY) <= 0.0001f) return;

        transform.position = new Vector3(position.x, targetY, position.z);
    }
}
