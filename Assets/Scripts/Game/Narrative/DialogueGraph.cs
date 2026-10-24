using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "NOGAMENAME/Narrative/Dialogue Graph")]
public sealed class DialogueGraph : ScriptableObject
{
    [SerializeField] private string entryNodeId = "start";
    [SerializeField] private List<DialogueNode> nodes = new();

    public string EntryNodeId => entryNodeId;
    public DialogueNode FindNode(string id) => nodes.Find(node => node != null && node.Id == id);
}

[Serializable]
public sealed class DialogueNode
{
    [SerializeField] private string id;
    [SerializeField] private string speakerId;
    [SerializeField, TextArea(2, 5)] private string text;
    [SerializeField] private List<DialogueChoice> choices = new();

    public string Id => id;
    public string SpeakerId => speakerId;
    public string Text => text;
    public IReadOnlyList<DialogueChoice> Choices => choices;
}

[Serializable]
public sealed class DialogueChoice
{
    [SerializeField, TextArea] private string text;
    [SerializeField] private string nextNodeId;
    [SerializeField] private GameFlagId[] requiredKnowledge = Array.Empty<GameFlagId>();
    [SerializeField] private GameFlagId[] requiredWorldFlags = Array.Empty<GameFlagId>();
    [SerializeField] private GameFlagId knowledgeGranted;
    [SerializeField] private GameFlagId worldFlagGranted;

    public string Text => text;
    public string NextNodeId => nextNodeId;

    public bool IsAvailable(GameStateService state)
    {
        if (state == null) return requiredKnowledge.Length == 0 && requiredWorldFlags.Length == 0;
        foreach (GameFlagId flag in requiredKnowledge)
            if (!state.HasKnowledge(flag)) return false;
        foreach (GameFlagId flag in requiredWorldFlags)
            if (!state.HasWorldFlag(flag)) return false;
        return true;
    }

    public void Apply(GameStateService state)
    {
        if (state == null) return;
        if (knowledgeGranted != GameFlagId.None) state.Learn(knowledgeGranted);
        if (worldFlagGranted != GameFlagId.None) state.SetWorldFlag(worldFlagGranted, true);
    }
}
