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
    [SerializeField] float snapTurnDegrees = 45f;
    [SerializeField] float snapTurnCooldown = 0.35f;

    XROrigin xrOrigin;
    CharacterController character;
    Transform head;
    InputAction leftStick;
    InputAction rightStick;
    float verticalVelocity;
    float nextSnapTime;

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
        TurnFromRightStick();
        MoveFromLeftStick();
    }

    void MoveFromLeftStick()
    {
        if (xrOrigin == null)
            return;

        Vector2 input = ReadStick(leftStick);
        if (input.sqrMagnitude < 0.0001f && character == null)
            return;

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

    void TurnFromRightStick()
    {
        if (xrOrigin == null || Time.time < nextSnapTime)
            return;

        Vector2 input = ReadStick(rightStick);
        if (Mathf.Abs(input.x) < 0.6f)
            return;

        float angle = input.x > 0f ? snapTurnDegrees : -snapTurnDegrees;
        if (!xrOrigin.RotateAroundCameraUsingOriginUp(angle))
        {
            Transform origin = xrOrigin.Origin != null ? xrOrigin.Origin.transform : xrOrigin.transform;
            origin.Rotate(0f, angle, 0f, Space.World);
        }

        nextSnapTime = Time.time + snapTurnCooldown;
    }

    Vector2 ReadStick(InputAction stick)
    {
        if (stick == null)
            return Vector2.zero;

        Vector2 input = stick.ReadValue<Vector2>();
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
        }

        if (rightStick == null)
        {
            rightStick = new InputAction(type: InputActionType.Value, binding: "<XRController>{RightHand}/{Primary2DAxis}");
            rightStick.AddBinding("<XRController>{RightHand}/thumbstick");
        }
    }

    void MuteXriMoveConflicts()
    {
        ControllerInputActionManager[] managers = FindObjectsByType<ControllerInputActionManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < managers.Length; i++)
        {
            if (managers[i] != null && managers[i].enabled)
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
