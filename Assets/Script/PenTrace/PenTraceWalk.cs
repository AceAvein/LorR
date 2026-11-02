using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

[DefaultExecutionOrder(30000)]
public class PenTraceWalk : MonoBehaviour
{
    [SerializeField] float moveSpeed = 3.5f;
    [SerializeField] float gravity = -9.81f;
    [SerializeField] float stickDeadzone = 0.15f;

    XROrigin xrOrigin;
    CharacterController character;
    Transform head;
    InputAction leftStick;
    InputAction rightStick;
    float verticalVelocity;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoStart()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryCreate(SceneManager.GetActiveScene());
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryCreate(scene);
    }

    static void TryCreate(Scene scene)
    {
        if (!scene.IsValid() || !ModuleScenes.IsPenTrace(scene.name))
            return;

        if (ModuleScenes.IsGuided(scene.name))
            return;

        if (FindAnyObjectByType<PenTraceWalk>() != null)
            return;

        GameObject host = new GameObject("PenTraceWalk");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<PenTraceWalk>();
    }

    void Awake()
    {
        ResolveRig();
        BindSticks();
        MuteXriMoveConflicts();
    }

    void OnEnable()
    {
        BindSticks();
        if (leftStick != null)
            leftStick.Enable();
        if (rightStick != null)
            rightStick.Enable();
    }

    void OnDisable()
    {
        if (leftStick != null)
            leftStick.Disable();
        if (rightStick != null)
            rightStick.Disable();
    }

    void OnDestroy()
    {
        DisposeStick(ref leftStick);
        DisposeStick(ref rightStick);
    }

    void Update()
    {
        if (GamePauseMenu.IsFrozen)
            return;

        if (IsMovementLocked())
            return;

        ResolveRig();
        MuteXriMoveConflicts();
        MoveFromSticks();
    }

    void MoveFromSticks()
    {
        if (xrOrigin == null)
            return;

        Vector2 input = ReadStick();
        Transform origin = xrOrigin.Origin != null ? xrOrigin.Origin.transform : xrOrigin.transform;

        Vector3 forward = head != null ? head.forward : origin.forward;
        Vector3 right = head != null ? head.right : origin.right;
        forward.y = 0f;
        right.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
            forward = origin.forward;
        if (right.sqrMagnitude < 0.001f)
            right = origin.right;
        forward.Normalize();
        right.Normalize();

        Vector3 wish = (forward * input.y + right * input.x) * moveSpeed;

        if (character != null && character.enabled)
        {
            if (character.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -1f;
            else
                verticalVelocity += gravity * Time.deltaTime;

            wish.y = verticalVelocity;
            character.Move(wish * Time.deltaTime);
            return;
        }

        origin.position += new Vector3(wish.x, 0f, wish.z) * Time.deltaTime;
    }

    Vector2 ReadStick()
    {
        Vector2 left = leftStick != null ? leftStick.ReadValue<Vector2>() : Vector2.zero;
        Vector2 right = rightStick != null ? rightStick.ReadValue<Vector2>() : Vector2.zero;
        Vector2 input = left.sqrMagnitude >= right.sqrMagnitude ? left : right;
        if (input.magnitude < stickDeadzone)
            return Vector2.zero;
        return Vector2.ClampMagnitude(input, 1f);
    }

    void ResolveRig()
    {
        if (xrOrigin == null)
            xrOrigin = FindFirstObjectByType<XROrigin>(FindObjectsInactive.Exclude);

        if (xrOrigin == null)
            return;

        if (head == null)
            head = xrOrigin.Camera != null ? xrOrigin.Camera.transform : Camera.main != null ? Camera.main.transform : null;

        if (character == null)
        {
            if (xrOrigin.Origin != null)
                character = xrOrigin.Origin.GetComponent<CharacterController>();
            if (character == null)
                character = xrOrigin.GetComponent<CharacterController>();
            if (character == null)
                character = xrOrigin.GetComponentInChildren<CharacterController>();
        }

        if (character != null && !character.enabled)
            character.enabled = true;
    }

    void BindSticks()
    {
        if (leftStick == null)
        {
            leftStick = new InputAction(type: InputActionType.Value, binding: "<XRController>{LeftHand}/{Primary2DAxis}");
            leftStick.AddBinding("<XRController>{LeftHand}/thumbstick");
            leftStick.AddBinding("<Gamepad>/leftStick");
            leftStick.AddBinding("<Joystick>/stick");
        }

        if (rightStick == null)
        {
            rightStick = new InputAction(type: InputActionType.Value, binding: "<XRController>{RightHand}/{Primary2DAxis}");
            rightStick.AddBinding("<XRController>{RightHand}/thumbstick");
            rightStick.AddBinding("<Gamepad>/rightStick");
        }
    }

    void MuteXriMoveConflicts()
    {
        ControllerInputActionManager[] managers = FindObjectsByType<ControllerInputActionManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < managers.Length; i++)
        {
            if (managers[i] == null)
                continue;

            managers[i].smoothMotionEnabled = true;
            if (managers[i].enabled)
                managers[i].enabled = false;
        }

        ContinuousMoveProvider[] movers = FindObjectsByType<ContinuousMoveProvider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < movers.Length; i++)
        {
            if (movers[i] != null && movers[i].enabled)
                movers[i].enabled = false;
        }

        LocomotionProvider[] providers = FindObjectsByType<LocomotionProvider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < providers.Length; i++)
        {
            if (providers[i] != null && providers[i].enabled)
                providers[i].enabled = false;
        }
    }

    static void DisposeStick(ref InputAction action)
    {
        if (action == null)
            return;

        action.Disable();
        action.Dispose();
        action = null;
    }

    static bool IsMovementLocked()
    {
        return PenTracePortal.Instance != null && PenTracePortal.Instance.IsLocked;
    }
}
