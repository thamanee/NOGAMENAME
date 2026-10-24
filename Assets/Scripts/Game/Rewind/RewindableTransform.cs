using UnityEngine;

public sealed class RewindableTransform : MonoBehaviour, IRewindable
{
    [SerializeField] private string rewindId;
    public string RewindId => rewindId;

    private void Reset() => rewindId = $"{gameObject.scene.name}/{name}";

    public RewindObjectState CaptureRewindState() => new()
    {
        Id = rewindId,
        Position = transform.position,
        Rotation = transform.rotation,
        Active = gameObject.activeSelf
    };

    public void RestoreRewindState(RewindObjectState state)
    {
        transform.SetPositionAndRotation(state.Position, state.Rotation);
        gameObject.SetActive(state.Active);
    }
}
