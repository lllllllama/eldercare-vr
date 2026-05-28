using System;
using UnityEngine;
using UnityEngine.SceneManagement;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
#endif

public class DisableJoystickLocomotionGuard : MonoBehaviour
{
    private static DisableJoystickLocomotionGuard _instance;

    public bool disableOnAwake = true;
    public bool keepChecking = true;
    public float checkIntervalSeconds = 0.5f;
    public bool disableLocomotionActionMaps = true;
    public bool verboseLogging = true;

    private float _nextCheckTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureRuntimeGuard()
    {
        if (_instance != null) return;

        var guardObject = new GameObject("JoystickLocomotionSafetyGuard");
        _instance = guardObject.AddComponent<DisableJoystickLocomotionGuard>();
        DontDestroyOnLoad(guardObject);
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (disableOnAwake)
        {
            DisableJoystickLocomotion();
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (!keepChecking) return;
        if (Time.unscaledTime < _nextCheckTime) return;

        _nextCheckTime = Time.unscaledTime + Mathf.Max(0.05f, checkIntervalSeconds);
        DisableJoystickLocomotion();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        DisableJoystickLocomotion();
    }

    public void DisableJoystickLocomotion()
    {
        DisableLocomotionBehaviours();

#if ENABLE_INPUT_SYSTEM
        if (disableLocomotionActionMaps)
        {
            DisableXriLocomotionActionMaps();
        }
#endif
    }

    private void DisableLocomotionBehaviours()
    {
        var behaviours = FindObjectsOfType<Behaviour>(true);
        for (var i = 0; i < behaviours.Length; i++)
        {
            var behaviour = behaviours[i];
            if (behaviour == null || behaviour == this || !behaviour.enabled) continue;
            if (!IsJoystickLocomotionBehaviour(behaviour.GetType())) continue;

            behaviour.enabled = false;
            if (verboseLogging)
            {
                Debug.Log($"Joystick locomotion disabled: {behaviour.GetType().Name} on {GetPath(behaviour.transform)}", behaviour);
            }
        }
    }

    private static bool IsJoystickLocomotionBehaviour(Type type)
    {
        if (type == null) return false;

        var name = type.Name;
        var fullName = type.FullName ?? name;

        if (ContainsAny(name,
                "ContinuousMoveProvider",
                "ActionBasedContinuousMoveProvider",
                "DynamicMoveProvider",
                "ContinuousTurnProvider",
                "SnapTurnProvider",
                "TeleportationProvider",
                "LocomotionSystem",
                "LocomotionMediator",
                "ActionBasedControllerManager",
                "CharacterControllerDriver",
                "GrabMoveProvider",
                "ConstrainedMoveProvider",
                "TwoHandedGrabMoveProvider",
                "ClimbProvider",
                "BodyTransformer"))
        {
            return true;
        }

        return fullName.Contains("XR.Interaction.Toolkit.Locomotion") &&
               ContainsAny(name, "Move", "Turn", "Teleport", "Climb", "Locomotion");
    }

    private static bool ContainsAny(string value, params string[] needles)
    {
        if (string.IsNullOrEmpty(value)) return false;

        for (var i = 0; i < needles.Length; i++)
        {
            if (value.IndexOf(needles[i], StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

#if ENABLE_INPUT_SYSTEM
    private void DisableXriLocomotionActionMaps()
    {
        var managers = FindObjectsOfType<InputActionManager>(true);
        for (var i = 0; i < managers.Length; i++)
        {
            var manager = managers[i];
            if (manager == null || manager.actionAssets == null) continue;

            foreach (var asset in manager.actionAssets)
            {
                if (asset == null) continue;

                foreach (var map in asset.actionMaps)
                {
                    if (map == null || !IsLocomotionActionMap(map.name)) continue;
                    if (!map.enabled) continue;

                    map.Disable();
                    if (verboseLogging)
                    {
                        Debug.Log($"Joystick locomotion action map disabled: {asset.name}/{map.name}", manager);
                    }
                }
            }
        }
    }

    private static bool IsLocomotionActionMap(string mapName)
    {
        if (string.IsNullOrEmpty(mapName)) return false;
        return mapName.IndexOf("Locomotion", StringComparison.OrdinalIgnoreCase) >= 0;
    }
#endif

    private static string GetPath(Transform transform)
    {
        if (transform == null) return "<null>";

        var path = transform.name;
        var current = transform.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }
}
