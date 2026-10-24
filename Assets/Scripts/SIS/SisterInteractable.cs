using UnityEngine;

[RequireComponent(typeof(SisterFollow))]
public class SisterInteractable : MonoBehaviour, IInteractable
{
    [Header("Interaction")]
    [SerializeField] private string interactionId = "character.min";
    [SerializeField] private string prompt = "คุย";
    [Header("References")]
    [SerializeField] private SisterFollow sisterFollow;
    [SerializeField] private SisterDialogueUI dialogueUI;

    [Header("Optional Head Pat Reaction")]
    [SerializeField] private Animator animator;

    [Tooltip("Leave empty until the animation trigger is ready.")]
    [SerializeField] private string headPatTrigger = "";

    public bool RequiresKey => true;
    public string InteractionId => interactionId;
    public string Prompt => prompt;

    public bool AllowKeyInteraction =>
        isActiveAndEnabled &&
        dialogueUI != null &&
        dialogueUI.isActiveAndEnabled &&
        !dialogueUI.IsOpen;

    private void Reset()
    {
        sisterFollow = GetComponent<SisterFollow>();
        animator = GetComponentInChildren<Animator>();
    }

    private void Awake()
    {
        if (sisterFollow == null)
            sisterFollow = GetComponent<SisterFollow>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    public void Interact()
    {
        if (!AllowKeyInteraction ||
            sisterFollow == null ||
            !sisterFollow.isActiveAndEnabled)
        {
            return;
        }

        dialogueUI.Open(sisterFollow, this);
    }

    public void PlayHeadPatReaction()
    {
        if (animator == null ||
            !animator.isActiveAndEnabled ||
            string.IsNullOrWhiteSpace(headPatTrigger))
        {
            return;
        }

        animator.SetTrigger(headPatTrigger);
    }
}
