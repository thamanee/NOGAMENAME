using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLook : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Transform playerCamera;

    [Header("Look Settings")]
    [SerializeField] private float mouseSensitivity = 0.08f;

    [SerializeField, Range(1f, 89f)]
    private float verticalLimit = 85f;

    [Header("Body Follow")]
    [Tooltip("กล้องหันจากลำตัวเกินมุมนี้ ลำตัวจึงเริ่มหมุนตาม")]
    [SerializeField, Range(0f, 85f)]
    private float bodyFollowAngle = 70f;

    [Tooltip("ความเร็วหมุนลำตัว หน่วยองศาต่อวินาที")]
    [SerializeField, Min(1f)]
    private float bodyTurnSpeed = 180f;

    [Header("Input")]
    [SerializeField] private InputActionReference lookAction;

    private float cameraYaw;
    private float cameraPitch;

    private void OnEnable()
    {
        if (playerCamera == null ||
            lookAction == null ||
            lookAction.action == null)
        {
            Debug.LogError(
                "PlayerLook: กรุณาใส่ Player Camera และ Look Action",
                this);

            enabled = false;
            return;
        }

        // อ่านมุมปัจจุบัน เพื่อไม่ให้กล้องกระตุก
        // เมื่อกลับมาจากเมนูบทสนทนา
        Vector3 cameraAngles = playerCamera.eulerAngles;

        cameraYaw = cameraAngles.y;
        cameraPitch = Mathf.Clamp(
            Mathf.DeltaAngle(0f, cameraAngles.x),
            -verticalLimit,
            verticalLimit);

        lookAction.action.Enable();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDisable()
    {
        if (lookAction != null && lookAction.action != null)
            lookAction.action.Disable();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        Vector2 lookInput =
            lookAction.action.ReadValue<Vector2>();

        cameraYaw = Mathf.Repeat(
            cameraYaw + lookInput.x * mouseSensitivity,
            360f);

        cameraPitch -= lookInput.y * mouseSensitivity;

        cameraPitch = Mathf.Clamp(
            cameraPitch,
            -verticalLimit,
            verticalLimit);

        RotateBodyWhenNeeded();

        // ใช้มุมโลก เพื่อให้ลำตัวหมุนแล้วไม่ลากกล้องหมุนซ้ำ
        playerCamera.rotation =
            Quaternion.Euler(cameraPitch, cameraYaw, 0f);
    }

    private void RotateBodyWhenNeeded()
    {
        float bodyYaw = transform.eulerAngles.y;

        float yawDifference =
            Mathf.DeltaAngle(bodyYaw, cameraYaw);

        if (Mathf.Abs(yawDifference) <= bodyFollowAngle)
            return;

        // หมุนเฉพาะส่วนที่เกินขอบเขต
        // เช่น กล้องหัน 90° ลำตัวจะตามมา 20°
        float targetBodyYaw =
            cameraYaw -
            Mathf.Sign(yawDifference) * bodyFollowAngle;

        float nextBodyYaw = Mathf.MoveTowardsAngle(
            bodyYaw,
            targetBodyYaw,
            bodyTurnSpeed * Time.deltaTime);

        transform.rotation =
            Quaternion.Euler(0f, nextBodyYaw, 0f);
    }
}