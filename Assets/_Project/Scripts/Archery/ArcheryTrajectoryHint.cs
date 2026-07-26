using UnityEngine;

public class ArcheryTrajectoryHint : MonoBehaviour
{
    public LineRenderer line;
    public float gravityMetersPerSecondSquared = ArcheryGeometry.ArrowGravityMetersPerSecondSquared;
    public float linearDragPerSecond = ArcheryGeometry.ArrowLinearDragPerSecond;
    public float stepSeconds = ArcheryGeometry.TrajectoryPreviewStepSeconds;
    public float maxSeconds = ArcheryGeometry.TrajectoryPreviewMaxSeconds;

    private Vector3[] _buffer;

    public void ShowPreview(Vector3 origin, Vector3 velocity)
    {
        if (line == null) return;

        if (_buffer == null || _buffer.Length != ArcheryGeometry.TrajectoryPreviewPointCapacity)
        {
            _buffer = new Vector3[ArcheryGeometry.TrajectoryPreviewPointCapacity];
        }

        var count = ArcherySolver.SampleTrajectory(
            origin,
            velocity,
            gravityMetersPerSecondSquared,
            linearDragPerSecond,
            stepSeconds,
            maxSeconds,
            _buffer);
        if (count < 2)
        {
            Hide();
            return;
        }

        line.useWorldSpace = true;
        line.positionCount = count;
        for (var i = 0; i < count; i++)
        {
            line.SetPosition(i, _buffer[i]);
        }

        if (!line.enabled)
        {
            line.enabled = true;
        }
    }

    public void Hide()
    {
        if (line != null && line.enabled)
        {
            line.enabled = false;
        }
    }

    private void OnDisable()
    {
        Hide();
    }
}
