using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class SisterFollow : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Animator animator;

    [Header("Following")]
    [SerializeField] private bool followPlayer;
    [SerializeField, Min(0.1f)] private float stopDistance = 1.5f;
    [SerializeField, Min(0.1f)] private float resumeDistance = 2.2f;
    [SerializeField, Min(0.05f)] private float pathUpdateInterval = 0.2f;
    [SerializeField, Min(0.1f)] private float targetSampleRadius = 1f;

    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float walkSpeed = 2f;
    [SerializeField, Min(0.1f)] private float acceleration = 6f;
    [SerializeField, Min(1f)] private float turnSpeed = 360f;

    [Header("Animation")]
    [SerializeField, Min(0.01f)] private float animationDamping = 0.15f;

    public bool IsFollowingEnabled => followPlayer;

    private static readonly int SpeedHash =
        Animator.StringToHash("Speed");

    private NavMeshAgent agent;
    private bool isFollowing;
    private float nextPathUpdate;

    private bool AgentReady =>
        agent != null &&
        agent.isActiveAndEnabled &&
        agent.isOnNavMesh;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        agent.speed = walkSpeed;
        agent.acceleration = acceleration;
        agent.angularSpeed = turnSpeed;
        agent.stoppingDistance = stopDistance;
        agent.autoBraking = true;
        agent.updatePosition = true;
        agent.updateRotation = true;

        if (animator != null)
            animator.applyRootMotion = false;
    }

    private void Start()
    {
        if (player == null)
            Debug.LogWarning("SisterFollow: Assign Player.", this);

        if (!AgentReady)
        {
            Debug.LogWarning(
                "SisterFollow: Place Sister on a baked NavMesh.",
                this);
        }
    }

    private void Update()
    {
        if (!AgentReady)
        {
            UpdateAnimation(0f);
            return;
        }

        if (!followPlayer || player == null)
        {
            StopFollowing();
            UpdateAnimation(agent.velocity.magnitude);
            return;
        }

        if (Time.time >= nextPathUpdate)
        {
            nextPathUpdate = Time.time + pathUpdateInterval;
            UpdateDestination();
        }

        UpdateAnimation(agent.velocity.magnitude);
    }

    private void UpdateDestination()
    {
        NavMeshQueryFilter filter = new NavMeshQueryFilter
        {
            agentTypeID = agent.agentTypeID,
            areaMask = agent.areaMask
        };

        if (!NavMesh.SamplePosition(
            player.position,
            out NavMeshHit target,
            targetSampleRadius,
            filter))
        {
            StopFollowing();
            return;
        }

        float distance = Vector3.Distance(
            transform.position,
            target.position);

        bool blocked = agent.Raycast(
            target.position,
            out NavMeshHit obstruction);

        if (isFollowing)
        {
            if (!blocked && distance <= stopDistance)
            {
                StopFollowing();
                return;
            }
        }
        else
        {
            if (!blocked && distance < resumeDistance)
                return;

            isFollowing = true;
        }

        agent.isStopped = false;

        if (!agent.SetDestination(target.position))
            StopFollowing();
    }

    private void StopFollowing()
    {
        isFollowing = false;

        if (!AgentReady)
            return;

        agent.isStopped = true;

        if (agent.hasPath || agent.pathPending)
            agent.ResetPath();
    }

    private void UpdateAnimation(float actualSpeed)
    {
        if (animator == null)
            return;

        float normalizedSpeed = Mathf.Clamp01(
            actualSpeed / Mathf.Max(walkSpeed, 0.1f));

        animator.SetFloat(
            SpeedHash,
            normalizedSpeed,
            animationDamping,
            Time.deltaTime);
    }

    public void SetFollowing(bool value)
    {
        followPlayer = value;
        nextPathUpdate = 0f;

        if (!value)
            StopFollowing();
    }

    private void OnDisable()
    {
        StopFollowing();

        if (animator != null)
            animator.SetFloat(SpeedHash, 0f);
    }

    private void OnValidate()
    {
        resumeDistance = Mathf.Max(
            resumeDistance,
            stopDistance + 0.2f);
    }
}
