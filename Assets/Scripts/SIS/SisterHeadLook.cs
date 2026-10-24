using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public class SisterHeadLook : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform playerCamera;

    [Header("Smoothness")]
    [SerializeField, Min(0.1f)] private float blendSpeed = 3f;
    [SerializeField, Min(0.1f)] private float turnSmoothness = 8f;

    [Header("Head Limits")]
    [SerializeField, Range(0f, 85f)] private float maxYaw = 70f;
    [SerializeField, Range(0f, 60f)] private float maxPitch = 35f;

    private Animator animator;
    private Transform head;

    private bool isTalking;
    private float currentWeight;
    private Vector3 smoothDirection = Vector3.forward;

    private void Awake()
    {
        animator = GetComponent<Animator>();

        if (animator.avatar == null ||
            !animator.avatar.isValid ||
            !animator.isHuman)
        {
            Debug.LogWarning(
                "SisterHeadLook requires a valid Humanoid Animator.",
                this);

            enabled = false;
            return;
        }

        head = animator.GetBoneTransform(HumanBodyBones.Head);

        if (head == null)
        {
            Debug.LogWarning(
                "SisterHeadLook: Head bone was not found.",
                this);

            enabled = false;
        }
    }

    public void SetTalking(bool value)
    {
        isTalking = value;
    }

    private void Update()
    {
        bool canLook = isTalking && playerCamera != null;

        currentWeight = Mathf.MoveTowards(
            currentWeight,
            canLook ? 1f : 0f,
            blendSpeed * Time.deltaTime);

        if (!canLook || head == null)
            return;

        Vector3 direction = playerCamera.position - head.position;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        // คำนวณมุมเทียบกับด้านหน้าของตัวละคร
        Vector3 localDirection =
            transform.InverseTransformDirection(direction.normalized);

        float yaw = Mathf.Atan2(
            localDirection.x,
            localDirection.z) * Mathf.Rad2Deg;

        float pitch = Mathf.Atan2(
            localDirection.y,
            new Vector2(
                localDirection.x,
                localDirection.z).magnitude) * Mathf.Rad2Deg;

        yaw = Mathf.Clamp(yaw, -maxYaw, maxYaw);
        pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);

        Vector3 limitedDirection =
            Quaternion.Euler(-pitch, yaw, 0f) * Vector3.forward;

        float smoothing =
            1f - Mathf.Exp(-turnSmoothness * Time.deltaTime);

        smoothDirection = Vector3.Slerp(
            smoothDirection,
            limitedDirection,
            smoothing).normalized;
    }

    private void OnAnimatorIK(int layerIndex)
    {
        // เปิด IK Pass เฉพาะ Base Layer
        if (layerIndex != 0 || animator == null || head == null)
            return;

        // ขยับเฉพาะหัว โดยปล่อยดวงตาให้ระบบ VRM เดิมควบคุม
        animator.SetLookAtWeight(
            currentWeight,
            0f,     // Body
            1f,     // Head
            0f,     // Eyes
            0.5f);  // จำกัดการบิดเพิ่มเติมของ Humanoid IK

        if (currentWeight <= 0f)
            return;

        Vector3 worldDirection =
            transform.TransformDirection(smoothDirection);

        animator.SetLookAtPosition(
            head.position + worldDirection * 3f);
    }

    private void OnDisable()
    {
        isTalking = false;
        currentWeight = 0f;
        smoothDirection = Vector3.forward;
    }
}