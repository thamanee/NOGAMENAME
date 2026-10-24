using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 2.8f;
    [SerializeField] private float runSpeed = 4.8f;
    [SerializeField] private float crouchSpeed = 1.4f;

    [Header("Collider")]
    [SerializeField] private float standingHeight = 2.0f;
    [SerializeField] private float crouchingHeight = 1.1f;
    [SerializeField] private float crouchTransitionSpeed = 8f;
    [SerializeField] private LayerMask collisionMask = ~0;

    [Header("Camera")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private float cameraTopOffset = 0.15f;

    [Header("Gravity")]
    [SerializeField] private float gravity = -20f;

    [Header("Input")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference sprintAction;
    [SerializeField] private InputActionReference crouchAction;

    private CharacterController controller;

    private Vector3 velocity;
    private float currentSpeed;

    private bool isCrouching;
    private readonly Collider[] standCheckResults = new Collider[12];

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (playerCamera == null)
        {
            Debug.LogError("PlayerController: assign Player Camera.", this);
            enabled = false;
            return;
        }

        controller.height = standingHeight;
        controller.center = new Vector3(
            0f,
            standingHeight / 2f,
            0f
        );

        UpdateCameraPosition(true);
    }

    private void OnEnable()
    {
        if (moveAction == null || moveAction.action == null ||
            sprintAction == null || sprintAction.action == null ||
            crouchAction == null || crouchAction.action == null)
        {
            Debug.LogError("PlayerController: assign Move, Sprint and Crouch actions.", this);
            enabled = false;
            return;
        }

        moveAction.action.Enable();
        sprintAction.action.Enable();
        crouchAction.action.Enable();
    }

    private void OnDisable()
    {
        if (moveAction != null && moveAction.action != null) moveAction.action.Disable();
        if (sprintAction != null && sprintAction.action != null) sprintAction.action.Disable();
        if (crouchAction != null && crouchAction.action != null) crouchAction.action.Disable();
    }

    private void Update()
    {
        HandleCrouch();
        HandleMovement();
        HandleGravity();
    }

    private void HandleMovement()
    {
        Vector2 input = Vector2.ClampMagnitude(
            moveAction.action.ReadValue<Vector2>(),
            1f);

        // เอาเฉพาะทิศกล้องบนพื้น ไม่เดินขึ้นลงตามการก้มเงย
        Vector3 cameraForward = Vector3.ProjectOnPlane(
            playerCamera.forward,
            Vector3.up);

        if (cameraForward.sqrMagnitude < 0.0001f)
            cameraForward = transform.forward;

        cameraForward.Normalize();

        Vector3 cameraRight =
            Vector3.Cross(Vector3.up, cameraForward).normalized;

        Vector3 move =
            cameraRight * input.x +
            cameraForward * input.y;

        move = Vector3.ClampMagnitude(move, 1f);

        if (isCrouching)
        {
            currentSpeed = crouchSpeed;
        }
        else if (sprintAction.action.IsPressed() && input.y > 0.1f)
        {
            currentSpeed = runSpeed;
        }
        else
        {
            currentSpeed = walkSpeed;
        }

        controller.Move(
            move * currentSpeed * Time.deltaTime);
    }

    private void HandleGravity()
    {
        if (controller.isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }

        velocity.y += gravity * Time.deltaTime;

        controller.Move(
            velocity * Time.deltaTime
        );
    }

    private void HandleCrouch()
    {
        if (crouchAction.action.WasPressedThisFrame())
        {
            if (isCrouching)
            {
                if (CanStand()) isCrouching = false;
            }
            else
            {
                isCrouching = true;
            }
        }

        float targetHeight = isCrouching
            ? crouchingHeight
            : standingHeight;

        controller.height = Mathf.Lerp(
            controller.height,
            targetHeight,
            crouchTransitionSpeed * Time.deltaTime
        );

        // Collider เริ่มจากพื้น
        controller.center = new Vector3(
            0f,
            controller.height / 2f,
            0f
        );

        // Camera คำนวณจากความสูงของ Collider
        UpdateCameraPosition(false);
    }

    private void UpdateCameraPosition(bool instant)
    {
        float cameraY =
            controller.height - cameraTopOffset;

        Vector3 cameraPosition = playerCamera.localPosition;

        if (instant)
        {
            cameraPosition.y = cameraY;
        }
        else
        {
            cameraPosition.y = Mathf.Lerp(
                cameraPosition.y,
                cameraY,
                crouchTransitionSpeed * Time.deltaTime
            );
        }

        playerCamera.localPosition = cameraPosition;
    }

    private bool CanStand()
    {
        float radius = Mathf.Max(0.01f, controller.radius - controller.skinWidth);
        Vector3 bottom = transform.position + Vector3.up * radius;
        Vector3 top = transform.position + Vector3.up * (standingHeight - radius);
        int count = Physics.OverlapCapsuleNonAlloc(
            bottom, top, radius, standCheckResults, collisionMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Collider hit = standCheckResults[i];
            if (hit != null && hit != controller && !hit.transform.IsChildOf(transform))
                return false;
        }
        return true;
    }
}
