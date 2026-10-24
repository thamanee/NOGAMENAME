using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public sealed class WorldTimeline : MonoBehaviour
{
    [SerializeField] private List<TimedWorldEvent> events = new();
    private readonly HashSet<string> fired = new();

    private void Update()
    {
        GameStateService state = GameStateService.Instance;
        if (state == null)
            return;

        foreach (TimedWorldEvent item in events)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Id) || fired.Contains(item.Id))
                continue;
            if (state.GameMinutes < item.AtGameMinute || !item.RequirementsMet(state))
                continue;

            fired.Add(item.Id);
            item.Invoke();
        }
    }

    public void ResetAfterRewind(float restoredMinute)
    {
        foreach (TimedWorldEvent item in events)
            if (item != null && item.AtGameMinute > restoredMinute)
                fired.Remove(item.Id);
    }
}

[Serializable]
public sealed class TimedWorldEvent
{
    [SerializeField] private string id;
    [SerializeField, Min(0f)] private float atGameMinute;
    [SerializeField] private GameFlagId[] requiredKnowledge = Array.Empty<GameFlagId>();
    [SerializeField] private GameFlagId[] requiredWorldFlags = Array.Empty<GameFlagId>();
    [SerializeField] private UnityEvent onTriggered;

    public string Id => id;
    public float AtGameMinute => atGameMinute;

    public bool RequirementsMet(GameStateService state)
    {
        foreach (GameFlagId flag in requiredKnowledge)
            if (!state.HasKnowledge(flag)) return false;
        foreach (GameFlagId flag in requiredWorldFlags)
            if (!state.HasWorldFlag(flag)) return false;
        return true;
    }

    public void Invoke() => onTriggered?.Invoke();
}
