using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionRaycast : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private CrosshairUI crosshair;

    [Header("Settings")]
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private float rayRadius = 0.08f;
    [SerializeField] private LayerMask interactionLayers = ~0;

    [SerializeField] private InputActionReference interactAction;

    private IInteractable currentInteractable;

    public IInteractable CurrentInteractable => currentInteractable;

    private void OnEnable()
    {
        if (playerCamera == null || crosshair == null ||
            interactAction == null || interactAction.action == null)
        {
            Debug.LogError("InteractionRaycast: assign Camera, Crosshair and Interact Action.", this);
            enabled = false;
            return;
        }

        interactAction.action.Enable();
    }

    private void OnDisable()
    {
        if (interactAction != null && interactAction.action != null)
            interactAction.action.Disable();

        currentInteractable = null;
        if (crosshair != null)
            crosshair.SetNormal();
    }

    private void Update()
    {
        CheckInteraction();

        if (currentInteractable != null &&
            currentInteractable.AllowKeyInteraction &&
            interactAction.action.WasPressedThisFrame())
        {
            currentInteractable.Interact();
        }
    }

    private void CheckInteraction()
    {
        currentInteractable = null;

        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        if (Physics.SphereCast(
            ray,
            rayRadius,
            out RaycastHit hit,
            interactionDistance,
            interactionLayers,
            QueryTriggerInteraction.Ignore))
        {
            IInteractable interactable =
                hit.collider.GetComponentInParent<IInteractable>();

            if (interactable != null)
            {
                currentInteractable = interactable;

                crosshair.SetInteractable(
                    interactable.RequiresKey,
                    interactable.Prompt
                );

                return;
            }
        }

        crosshair.SetNormal();
    }
}
