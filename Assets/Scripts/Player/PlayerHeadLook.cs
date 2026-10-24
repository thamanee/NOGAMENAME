using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public class PlayerHeadLook : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerCamera;

    [Tooltip("Player ตัวหลักที่มี PlayerController และ PlayerLook")]
    [SerializeField] private Transform playerRoot;

    [Header("Head Limits")]
    [SerializeField, Range(0f, 85f)]
    private float maxHeadYaw = 70f;

    [SerializeField, Range(0f, 60f)]
    private float maxHeadPitch = 40f;

    [Header("Smoothness")]
    [SerializeField, Min(0.1f)]
    private float lookSmoothness = 12f;

    [SerializeField, Range(0f, 1f)]
    private float lookWeight = 1f;

    private Animator animator;
    private Transform head;

    private Vector3 smoothLocalDirection = Vector3.forward;
    private bool ready;

    private void Awake()
    {
        animator = GetComponent<Animator>();

        if (playerCamera == null || playerRoot == null)
        {
            Debug.LogError(
                "PlayerHeadLook: ใส่ Player Camera และ Player Root ให้ครบ",
                this);

            enabled = false;
            return;
        }

        if (animator.avatar == null ||
            !animator.avatar.isValid ||
            !animator.isHuman)
        {
            Debug.LogError(
                "PlayerHeadLook ต้องใช้ Animator แบบ Humanoid",
                this);

            enabled = false;
            return;
        }

        head = animator.GetBoneTransform(HumanBodyBones.Head);

        if (head == null)
        {
            Debug.LogError(
                "PlayerHeadLook: ไม่พบกระดูก Head",
                this);

            enabled = false;
            return;
        }

        ready = true;
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (!ready || layerIndex != 0)
            return;

        // มองไปในทิศเดียวกับกล้อง
        // ไม่ใช่หันกลับมามองตำแหน่งกล้อง
        Vector3 localDirection =
            playerRoot.InverseTransformDirection(
                playerCamera.forward);

        float yaw = Mathf.Atan2(
            localDirection.x,
            localDirection.z) * Mathf.Rad2Deg;

        float pitch = Mathf.Atan2(
            localDirection.y,
            new Vector2(
                localDirection.x,
                localDirection.z).magnitude) * Mathf.Rad2Deg;

        yaw = Mathf.Clamp(yaw, -maxHeadYaw, maxHeadYaw);

        pitch = Mathf.Clamp(
            pitch,
            -maxHeadPitch,
            maxHeadPitch);

        Vector3 targetLocalDirection =
            Quaternion.Euler(-pitch, yaw, 0f) *
            Vector3.forward;

        float blend =
            1f - Mathf.Exp(-lookSmoothness * Time.deltaTime);

        smoothLocalDirection = Vector3.Slerp(
            smoothLocalDirection,
            targetLocalDirection,
            blend).normalized;

        Vector3 worldDirection =
            playerRoot.TransformDirection(
                smoothLocalDirection);

        // ให้ระบบนี้ขยับเฉพาะหัว
        // การหมุนลำตัวจัดการโดย PlayerLook
        animator.SetLookAtWeight(
            lookWeight,
            0f,
            1f,
            0f,
            0.5f);

        animator.SetLookAtPosition(
            head.position + worldDirection * 5f);
    }

    private void OnDisable()
    {
        smoothLocalDirection = Vector3.forward;
    }
}