using UnityEngine;

public enum StoryCharacterId { Min, Miu }
public enum MotherControlState { Free, Resisting, Controlled, LinkSevered }

[CreateAssetMenu(menuName = "NOGAMENAME/Narrative/Character Definition")]
public sealed class CharacterDefinition : ScriptableObject
{
    [SerializeField] private StoryCharacterId characterId;
    [SerializeField] private string displayName;
    [SerializeField, TextArea] private string greeting;
    [SerializeField] private Color speechColor = Color.white;
    [SerializeField] private Color appearanceTint = Color.white;

    public StoryCharacterId CharacterId => characterId;
    public string DisplayName => displayName;
    public string Greeting => greeting;
    public Color SpeechColor => speechColor;
    public Color AppearanceTint => appearanceTint;
}
