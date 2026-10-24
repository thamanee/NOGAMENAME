using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-500)]
public sealed class GameStateService : MonoBehaviour
{
    [SerializeField] private float gameMinutes;
    [SerializeField, Min(0)] private int rewindUseCount;
    [SerializeField, Min(5f)] private float initialRewindLimitMinutes = 360f;
    [SerializeField, Min(0f)] private float rewindLimitLossMinutes = 5f;

    private readonly HashSet<GameFlagId> knowledge = new();
    private readonly HashSet<GameFlagId> worldFlags = new();

    public static GameStateService Instance { get; private set; }
    public float GameMinutes => gameMinutes;
    public int RewindUseCount => rewindUseCount;
    public float CurrentRewindLimitMinutes =>
        Mathf.Max(0f, initialRewindLimitMinutes - rewindUseCount * rewindLimitLossMinutes);

    public event Action<GameFlagId, bool, bool> FlagChanged;
    public event Action<float> TimeChanged;
    public event Action PersistentProgressChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (Application.isPlaying)
            DontDestroyOnLoad(gameObject);
        SetWorldFlag(GameFlagId.MinAlive, true);
        SetWorldFlag(GameFlagId.MotherAlive, true);
    }

    public bool HasKnowledge(GameFlagId flag) => flag != GameFlagId.None && knowledge.Contains(flag);
    public bool HasWorldFlag(GameFlagId flag) => flag != GameFlagId.None && worldFlags.Contains(flag);

    public void Learn(GameFlagId flag)
    {
        if (flag != GameFlagId.None && knowledge.Add(flag))
        {
            FlagChanged?.Invoke(flag, true, true);
            PersistentProgressChanged?.Invoke();
        }
    }

    public void SetWorldFlag(GameFlagId flag, bool value)
    {
        if (flag == GameFlagId.None)
            return;

        bool changed = value ? worldFlags.Add(flag) : worldFlags.Remove(flag);
        if (changed)
            FlagChanged?.Invoke(flag, value, false);
    }

    public void SetGameMinutes(float value)
    {
        gameMinutes = Mathf.Max(0f, value);
        TimeChanged?.Invoke(gameMinutes);
    }

    public WorldStateSnapshot CaptureWorldState() =>
        new(gameMinutes, new List<GameFlagId>(worldFlags));

    public void RestoreWorldState(WorldStateSnapshot snapshot)
    {
        worldFlags.Clear();
        foreach (GameFlagId flag in snapshot.WorldFlags)
            worldFlags.Add(flag);
        SetGameMinutes(snapshot.GameMinutes);
    }

    public bool TryConsumeRewind(float minutesBack)
    {
        if (minutesBack < 0f || minutesBack > CurrentRewindLimitMinutes)
            return false;

        rewindUseCount++;
        PersistentProgressChanged?.Invoke();
        return true;
    }

    public PersistentProgressData CapturePersistentProgress() => new()
    {
        Knowledge = new List<GameFlagId>(knowledge),
        RewindUseCount = rewindUseCount
    };

    public void RestorePersistentProgress(PersistentProgressData data)
    {
        if (data == null) return;
        knowledge.Clear();
        foreach (GameFlagId flag in data.Knowledge)
            if (flag != GameFlagId.None) knowledge.Add(flag);
        rewindUseCount = Mathf.Max(0, data.RewindUseCount);
    }
}

[Serializable]
public sealed class PersistentProgressData
{
    public List<GameFlagId> Knowledge = new();
    public int RewindUseCount;
}

[Serializable]
public sealed class WorldStateSnapshot
{
    [SerializeField] private float gameMinutes;
    [SerializeField] private List<GameFlagId> worldFlags;

    public float GameMinutes => gameMinutes;
    public IReadOnlyList<GameFlagId> WorldFlags => worldFlags;

    public WorldStateSnapshot(float gameMinutes, List<GameFlagId> worldFlags)
    {
        this.gameMinutes = gameMinutes;
        this.worldFlags = worldFlags ?? new List<GameFlagId>();
    }
}
