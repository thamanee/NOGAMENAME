using System;
using System.Collections.Generic;
using UnityEngine;

public interface IRewindable
{
    string RewindId { get; }
    RewindObjectState CaptureRewindState();
    void RestoreRewindState(RewindObjectState state);
}

[Serializable]
public sealed class RewindObjectState
{
    public string Id;
    public Vector3 Position;
    public Quaternion Rotation;
    public bool Active;
    public string CustomJson;
}

public sealed class RewindManager : MonoBehaviour
{
    [SerializeField, Min(1)] private int maximumCheckpoints = 96;
    private readonly List<RewindCheckpoint> checkpoints = new();

    public IReadOnlyList<RewindCheckpoint> Checkpoints => checkpoints;

    public void CaptureCheckpoint(string label)
    {
        if (GameStateService.Instance == null)
            return;

        var checkpoint = new RewindCheckpoint(label, GameStateService.Instance.CaptureWorldState());
        foreach (MonoBehaviour behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (behaviour is IRewindable rewindable)
                checkpoint.Objects.Add(rewindable.CaptureRewindState());

        checkpoints.Add(checkpoint);
        if (checkpoints.Count > maximumCheckpoints)
            checkpoints.RemoveAt(0);
    }

    public bool RewindTo(int checkpointIndex)
    {
        if (checkpointIndex < 0 || checkpointIndex >= checkpoints.Count || GameStateService.Instance == null)
            return false;

        RewindCheckpoint checkpoint = checkpoints[checkpointIndex];
        float minutesBack = Mathf.Max(0f,
            GameStateService.Instance.GameMinutes - checkpoint.World.GameMinutes);
        if (!GameStateService.Instance.TryConsumeRewind(minutesBack))
            return false;

        var targets = new Dictionary<string, IRewindable>();
        foreach (MonoBehaviour behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (behaviour is IRewindable rewindable && !string.IsNullOrWhiteSpace(rewindable.RewindId))
                targets[rewindable.RewindId] = rewindable;

        GameStateService.Instance.RestoreWorldState(checkpoint.World);
        foreach (RewindObjectState objectState in checkpoint.Objects)
            if (targets.TryGetValue(objectState.Id, out IRewindable target))
                target.RestoreRewindState(objectState);

        foreach (WorldTimeline timeline in FindObjectsByType<WorldTimeline>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            timeline.ResetAfterRewind(checkpoint.World.GameMinutes);
        return true;
    }
}

[Serializable]
public sealed class RewindCheckpoint
{
    [SerializeField] private string label;
    [SerializeField] private WorldStateSnapshot world;
    [SerializeField] private List<RewindObjectState> objects = new();

    public string Label => label;
    public WorldStateSnapshot World => world;
    public List<RewindObjectState> Objects => objects;

    public RewindCheckpoint(string label, WorldStateSnapshot world)
    {
        this.label = label;
        this.world = world;
    }
}
