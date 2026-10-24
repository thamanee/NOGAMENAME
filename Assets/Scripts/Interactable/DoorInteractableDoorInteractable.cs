using UnityEngine;

public class DoorInteractable : MonoBehaviour, IInteractable
{
    [Header("Interaction")]
    [SerializeField] private string interactionId = "door";
    [SerializeField] private string openPrompt = "เปิดประตู";
    [SerializeField] private string closePrompt = "ปิดประตู";

    [Header("References")]
    [SerializeField] private Transform doorPivot;
    [SerializeField] private Transform player;

    [Header("Door Settings")]
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float openSpeed = 180f;
    [SerializeField] private float closeSpeed = 180f;

    [Header("Push / Pull")]
    [SerializeField] private float pushDistance = 0.8f;

    [Header("Auto Close")]
    [SerializeField] private float closeDistance = 1.5f;

    [Header("Direction")]
    [SerializeField] private bool invertDirection = false;

    private float closedAngle;
    private float targetAngle;

    private bool isOpen;
    private bool playerHasPassed;

    private bool playerOnPushSide;
    private bool hasBeenNearSinceOpening;

    public string InteractionId => interactionId;
    public string Prompt => isOpen ? closePrompt : openPrompt;

    // Push side = ไม่ต้องกด E
    // Pull side = ต้องกด E
    public bool RequiresKey => !playerOnPushSide;

    // ทั้งสองฝั่งสามารถใช้ E ได้
    public bool AllowKeyInteraction => true;

    private void Start()
    {
        if (doorPivot == null || player == null)
        {
            Debug.LogError("DoorInteractable: assign Door Pivot and Player.", this);
            enabled = false;
            return;
        }

        closedAngle = doorPivot.localEulerAngles.y;
        targetAngle = closedAngle;
    }

    private void Update()
    {
        UpdatePlayerSide();

        HandlePush();

        HandleAutoClose();

        RotateDoor();
    }

    // ==========================================
    // ตรวจว่าผู้เล่นอยู่ฝั่งผลักหรือดึง
    // ==========================================

    private void UpdatePlayerSide()
    {
        Vector3 playerPosition =
            player.position - doorPivot.position;

        playerPosition.y = 0f;

        if (playerPosition.sqrMagnitude < 0.001f)
            return;

        float side =
            Vector3.Dot(
                doorPivot.forward,
                playerPosition.normalized
            );

        playerOnPushSide =
            invertDirection
                ? side < 0f
                : side > 0f;
    }

    // ==========================================
    // ผลักด้วยการเดินชน
    // เฉพาะฝั่ง Push
    // ==========================================

    private void HandlePush()
    {
        // ฝั่ง Pull เดินชนไม่ได้
        if (!playerOnPushSide)
            return;

        Vector3 playerPosition =
            player.position - doorPivot.position;

        playerPosition.y = 0f;

        float distance =
            playerPosition.magnitude;

        if (distance > pushDistance)
            return;

        if (playerPosition.sqrMagnitude < 0.001f)
            return;

        Vector3 toDoor =
            doorPivot.position - player.position;

        toDoor.y = 0f;

        // ต้องหันหน้าเข้าหาประตู
        float facing =
            Vector3.Dot(
                player.forward,
                toDoor.normalized
            );

        if (facing < 0.3f)
            return;

        // เดินชน → เปิด
        OpenDoor();
    }

    // ==========================================
    // กด E
    // ==========================================

    public void Interact()
    {
        if (isOpen)
            CloseDoor();
        else
            OpenDoor();
    }

    private void OpenDoor()
    {
        Vector3 playerPosition =
            player.position - doorPivot.position;

        playerPosition.y = 0f;

        if (playerPosition.sqrMagnitude < 0.001f)
            return;

        float side =
            Vector3.Dot(
                doorPivot.forward,
                playerPosition.normalized
            );

        float direction;

        // ==========================================
        // PUSH SIDE
        // ==========================================
        if (playerOnPushSide)
        {
            // ผลักออกจากตัวผู้เล่น
            direction = side > 0f ? 1f : -1f;
        }

        // ==========================================
        // PULL SIDE
        // ==========================================
        else
        {
            // ดึงเข้าหาตัวผู้เล่น
            direction = side > 0f ? -1f : 1f;
        }

        if (invertDirection)
            direction *= -1f;

        targetAngle =
            closedAngle +
            openAngle * direction;

        isOpen = true;
        playerHasPassed = false;
        hasBeenNearSinceOpening = false;
    }

    private void CloseDoor()
    {
        targetAngle = closedAngle;
        playerHasPassed = true;
    }

    // ==========================================
    // ปิดอัตโนมัติ
    // ==========================================

    private void HandleAutoClose()
    {
        if (!isOpen)
            return;

        Vector3 playerPosition =
            player.position - doorPivot.position;

        playerPosition.y = 0f;

        float distance =
            playerPosition.magnitude;

        if (distance <= closeDistance)
            hasBeenNearSinceOpening = true;

        if (hasBeenNearSinceOpening && distance > closeDistance)
        {
            playerHasPassed = true;
        }

        if (playerHasPassed)
        {
            targetAngle = closedAngle;
        }
    }

    // ==========================================
    // หมุนประตู
    // ==========================================

    private void RotateDoor()
    {
        float currentAngle =
            doorPivot.localEulerAngles.y;

        float speed =
            targetAngle == closedAngle
                ? closeSpeed
                : openSpeed;

        float newAngle =
            Mathf.MoveTowardsAngle(
                currentAngle,
                targetAngle,
                speed * Time.deltaTime
            );

        doorPivot.localRotation =
            Quaternion.Euler(
                0f,
                newAngle,
                0f
            );

        if (Mathf.Abs(
            Mathf.DeltaAngle(
                newAngle,
                targetAngle
            )
        ) < 0.1f)
        {
            doorPivot.localRotation =
                Quaternion.Euler(
                    0f,
                    targetAngle,
                    0f
                );

            if (targetAngle == closedAngle)
            {
                isOpen = false;
                playerHasPassed = false;
            }
        }
    }
}
