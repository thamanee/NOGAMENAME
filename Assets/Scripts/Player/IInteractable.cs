using UnityEngine;

public interface IInteractable
{
    string InteractionId { get; }
    string Prompt { get; }
    bool RequiresKey { get; }
    bool AllowKeyInteraction { get; }

    void Interact();
}
