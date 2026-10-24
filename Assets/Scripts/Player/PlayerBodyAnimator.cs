using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController))]
public class PlayerBodyAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator bodyAnimator;

    [Header("Movement Animation")]
    [Tooltip("ตั้งให้ตรงกับ Walk Speed ใน PlayerController")]
    [SerializeField, Min(0.01f)]
    private float referenceWalkSpeed = 2.8f;

    [SerializeField, Min(0.01f)]
    private float blendSmoothTime = 0.12f;

    [SerializeField, Min(0f)]
    private float movementDeadZone = 0.03f;

    [Tooltip("เคลื่อนที่เกินระยะนี้ในเฟรมเดียว จะถือว่าเป็นการวาร์ป")]
    [SerializeField, Min(0.1f)]
    private float teleportDistance = 2f;

    [Header("First Person Visibility")]
    [Tooltip(
        "ใส่เฉพาะ Renderer ของหัว/ผม/ใบหน้าที่แยกจากลำตัว " +
        "ห้ามใส่ Renderer ที่รวมทั้งตัว")]
    [SerializeField]
    private Renderer[] hiddenHeadRenderers = new Renderer[0];

    private static readonly int MoveXHash =
        Animator.StringToHash("MoveX");

    private static readonly int MoveYHash =
        Animator.StringToHash("MoveY");

    private PlayerController movement;
    private Vector3 previousPosition;

    private ShadowCastingMode[] originalShadowModes;
    private bool animatorReady;

    private void Awake()
    {
        movement = GetComponent<PlayerController>();

        if (bodyAnimator == null)
        {
            Debug.LogError(
                "PlayerBodyAnimator: กรุณาใส่ Animator ของโมเดล Player",
                this);
            enabled = false;
            return;
        }

        animatorReady =
            HasFloatParameter(MoveXHash) &&
            HasFloatParameter(MoveYHash);

        if (!animatorReady)
        {
            Debug.LogError(
                "Player Animator ต้องมี Float ชื่อ MoveX และ MoveY",
                this);
            enabled = false;
            return;
        }

        bodyAnimator.applyRootMotion = false;
    }

    private void OnEnable()
    {
        previousPosition = transform.position;

        // ซ่อนชิ้นส่วนหัว แต่ยังให้ทอดเงาได้
        originalShadowModes =
            new ShadowCastingMode[hiddenHeadRenderers.Length];

        for (int i = 0; i < hiddenHeadRenderers.Length; i++)
        {
            Renderer headRenderer = hiddenHeadRenderers[i];

            if (headRenderer == null)
                continue;

            originalShadowModes[i] =
                headRenderer.shadowCastingMode;

            headRenderer.shadowCastingMode =
                ShadowCastingMode.ShadowsOnly;
        }
    }

    private void LateUpdate()
    {
        Vector3 displacement =
            transform.position - previousPosition;

        previousPosition = transform.position;

        if (!animatorReady || bodyAnimator == null)
            return;

        float deltaTime = Time.deltaTime;

        if (deltaTime <= 0f)
            return;

        Vector2 targetBlend = Vector2.zero;

        bool canAnimateMovement =
            movement != null &&
            movement.isActiveAndEnabled &&
            displacement.sqrMagnitude <=
                teleportDistance * teleportDistance;

        if (canAnimateMovement)
        {
            // ไม่ใช้ความเร็วแนวดิ่งจากแรงโน้มถ่วง
            Vector3 horizontalVelocity =
                Vector3.ProjectOnPlane(displacement, Vector3.up)
                / deltaTime;

            if (horizontalVelocity.magnitude > movementDeadZone)
            {
                // แปลงทิศเดินโลก ให้เป็นซ้าย/ขวา/หน้า/หลังของ Player
                Vector3 localVelocity =
                    transform.InverseTransformDirection(
                        horizontalVelocity);

                targetBlend = new Vector2(
                    localVelocity.x,
                    localVelocity.z);

                targetBlend /= Mathf.Max(0.01f, referenceWalkSpeed);
            }
        }

        bodyAnimator.SetFloat(
            MoveXHash,
            targetBlend.x,
            blendSmoothTime,
            deltaTime);

        bodyAnimator.SetFloat(
            MoveYHash,
            targetBlend.y,
            blendSmoothTime,
            deltaTime);
    }

    private bool HasFloatParameter(int parameterHash)
    {
        foreach (AnimatorControllerParameter parameter
                 in bodyAnimator.parameters)
        {
            if (parameter.nameHash == parameterHash &&
                parameter.type == AnimatorControllerParameterType.Float)
            {
                return true;
            }
        }

        return false;
    }

    private void OnDisable()
    {
        if (animatorReady && bodyAnimator != null)
        {
            bodyAnimator.SetFloat(MoveXHash, 0f);
            bodyAnimator.SetFloat(MoveYHash, 0f);
        }

        if (originalShadowModes == null)
            return;

        for (int i = 0; i < hiddenHeadRenderers.Length &&
                        i < originalShadowModes.Length; i++)
        {
            if (hiddenHeadRenderers[i] != null)
            {
                hiddenHeadRenderers[i].shadowCastingMode =
                    originalShadowModes[i];
            }
        }
    }
}