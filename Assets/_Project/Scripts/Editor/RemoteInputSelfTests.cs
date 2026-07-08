using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class RemoteInputSelfTests
{
    private const string PassedMarker = "REMOTE_INPUT_TEST_PASSED";

    [MenuItem("Tools/PICO ElderCare/Run Remote Input Self Tests")]
    public static void RunAll()
    {
        RemoteGripInputSourceReadsDebugState();
        ControllerBallGrabberUsesRemoteGripInputSource();
        RemoteTableDragControllerUsesRemoteGripInputSource();
        Debug.Log(PassedMarker);
    }

    private static void RemoteGripInputSourceReadsDebugState()
    {
        var rigObject = new GameObject("RemoteDebugRigTest");
        try
        {
            var rig = rigObject.AddComponent<RemoteDebugControllerRig>();
            rig.EnsureRig();

            AssertTrue(rig.inputState != null, "Remote rig should create an input state.");
            AssertTrue(rig.LeftGripSource != null, "Remote rig should create a left grip source.");
            AssertTrue(rig.RightGripSource != null, "Remote rig should create a right grip source.");

            rig.inputState.ReleaseAll();
            AssertTrue(!rig.LeftGripSource.IsGripPressed, "Left remote grip should start released.");
            AssertTrue(!rig.RightGripSource.IsGripPressed, "Right remote grip should start released.");

            rig.inputState.SetLeftGrip(1f);
            AssertTrue(rig.LeftGripSource.IsGripPressed, "Left remote grip should read pressed debug state.");
            AssertTrue(!rig.RightGripSource.IsGripPressed, "Right remote grip should stay released.");

            rig.inputState.SetRightGrip(1f);
            AssertTrue(rig.RightGripSource.IsGripPressed, "Right remote grip should read pressed debug state.");
        }
        finally
        {
            Object.DestroyImmediate(rigObject);
        }
    }

    private static void ControllerBallGrabberUsesRemoteGripInputSource()
    {
        var stateObject = new GameObject("RemoteInputGripStateTest");
        var rigObject = new GameObject("RemoteInputRigTest");
        var grabberObject = new GameObject("RemoteInputGrabberTest");
        var ballObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        try
        {
            var interactionState = Object.FindObjectOfType<SimpleGripInteractionState>(true);
            if (interactionState == null)
            {
                interactionState = stateObject.AddComponent<SimpleGripInteractionState>();
            }

            interactionState.ResetState();

            var rig = rigObject.AddComponent<RemoteDebugControllerRig>();
            rig.EnsureRig();
            rig.leftControllerTransform.position = Vector3.zero;

            ballObject.name = "RemoteInputBall";
            ballObject.transform.position = new Vector3(0.08f, 0f, 0f);
            var ballRigidbody = ballObject.AddComponent<Rigidbody>();
            ballObject.AddComponent<PingPongBall>();
            Physics.SyncTransforms();

            var grabber = grabberObject.AddComponent<ControllerBallGrabber>();
            grabber.controllerTransform = rig.leftControllerTransform;
            grabber.gripInputSourceBehaviour = rig.LeftGripSource;
            grabber.interactionState = interactionState;
            grabber.grabRadius = 0.28f;
            InvokeLifecycle(grabber, "OnEnable");

            rig.inputState.SetLeftGrip(1f);
            InvokeLifecycle(grabber, "Update");
            AssertTrue(grabber.IsHoldingBall, "ControllerBallGrabber should grab when remote grip is pressed.");
            AssertTrue(ballObject.transform.parent == rig.leftControllerTransform, "Grabbed ball should be parented to the simulated controller.");

            rig.inputState.ReleaseAll();
            InvokeLifecycle(grabber, "Update");
            AssertTrue(!grabber.IsHoldingBall, "ControllerBallGrabber should release when remote grip is released.");
            AssertTrue(ballObject.transform.parent == null, "Released ball should restore its original parent.");
            AssertTrue(!ballRigidbody.isKinematic, "Released ball rigidbody should return to physics simulation.");
        }
        finally
        {
            Object.DestroyImmediate(ballObject);
            Object.DestroyImmediate(grabberObject);
            Object.DestroyImmediate(rigObject);
            Object.DestroyImmediate(stateObject);
        }
    }

    private static void RemoteTableDragControllerUsesRemoteGripInputSource()
    {
        var stateObject = new GameObject("RemoteTableDragGripStateTest");
        var rigObject = new GameObject("RemoteTableDragRigTest");
        var tableObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var dragObject = new GameObject("RemoteTableDragControllerTest");
        try
        {
            var interactionState = stateObject.AddComponent<SimpleGripInteractionState>();
            InvokeLifecycle(interactionState, "Awake");
            interactionState.ResetState();

            var rig = rigObject.AddComponent<RemoteDebugControllerRig>();
            rig.EnsureRig();
            rig.leftControllerTransform.position = Vector3.zero;
            rig.leftControllerTransform.rotation = Quaternion.identity;
            rig.hmdTransform.position = new Vector3(0f, 1.6f, -0.8f);
            rig.hmdTransform.rotation = Quaternion.identity;

            tableObject.name = "RemoteInputTable";
            tableObject.transform.position = new Vector3(0f, 0f, 1.25f);
            tableObject.transform.localScale = new Vector3(0.8f, 0.12f, 0.8f);
            Physics.SyncTransforms();

            var remoteDrag = dragObject.AddComponent<RemoteTableDragController>();
            remoteDrag.tableRoot = tableObject.transform;
            remoteDrag.controllerTransform = rig.leftControllerTransform;
            remoteDrag.gripInputSourceBehaviour = rig.LeftGripSource;
            remoteDrag.interactionState = interactionState;
            remoteDrag.hmdTransform = rig.hmdTransform;
            remoteDrag.controlServing = false;
            remoteDrag.clearBallsWhenDragging = false;
            remoteDrag.remoteGrabMaxDistanceMeters = 4f;
            remoteDrag.SetRemoteDragEnabled(true);
            InvokeLifecycle(remoteDrag, "OnEnable");

            rig.inputState.SetLeftGrip(1f);
            InvokeLifecycle(remoteDrag, "Update");
            AssertTrue(remoteDrag.IsDragging, "RemoteTableDragController should start dragging from remote grip input.");

            rig.inputState.ReleaseAll();
            InvokeLifecycle(remoteDrag, "Update");
            AssertTrue(!remoteDrag.IsDragging, "RemoteTableDragController should release when remote grip input is released.");
        }
        finally
        {
            Object.DestroyImmediate(dragObject);
            Object.DestroyImmediate(tableObject);
            Object.DestroyImmediate(rigObject);
            Object.DestroyImmediate(stateObject);
        }
    }

    private static void InvokeLifecycle(MonoBehaviour behaviour, string methodName)
    {
        var method = behaviour.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        AssertTrue(method != null, $"{behaviour.GetType().Name} should have {methodName}.");
        method.Invoke(behaviour, null);
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new System.Exception(message);
        }
    }
}
