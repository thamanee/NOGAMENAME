using UnityEngine;

[DisallowMultipleComponent]
public sealed class NarrativeCharacter : MonoBehaviour, IRewindable
{
    [SerializeField] private string rewindId = "character.min";
    [SerializeField] private CharacterDefinition definition;
    [SerializeField] private MotherControlState controlState;
    [SerializeField] private bool presentingAsMin;
    [SerializeField] private Renderer[] tintedRenderers = new Renderer[0];

    private MaterialPropertyBlock propertyBlock;

    public string RewindId => rewindId;
    public CharacterDefinition Definition => definition;
    public StoryCharacterId Identity => definition != null ? definition.CharacterId : StoryCharacterId.Min;
    public string DisplayName => presentingAsMin ? "มิน" :
        definition != null ? definition.DisplayName : name;
    public MotherControlState ControlState => controlState;
    public bool PresentingAsMin => presentingAsMin;

    private void Awake() => ApplyAppearance();

    public void Configure(CharacterDefinition value, string stableId)
    {
        definition = value;
        rewindId = stableId;
        ApplyAppearance();
    }

    public void SetControlState(MotherControlState value)
    {
        controlState = value;
        if (value == MotherControlState.LinkSevered && GameStateService.Instance != null)
            GameStateService.Instance.SetWorldFlag(GameFlagId.MiuLinkSevered, true);
    }

    public void SetPresentingAsMin(bool value) => presentingAsMin = value;

    private void ApplyAppearance()
    {
        if (definition == null)
            return;

        propertyBlock ??= new MaterialPropertyBlock();
        foreach (Renderer target in tintedRenderers)
        {
            if (target == null) continue;
            target.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_BaseColor", definition.AppearanceTint);
            propertyBlock.SetColor("_Color", definition.AppearanceTint);
            target.SetPropertyBlock(propertyBlock);
        }
    }

    public RewindObjectState CaptureRewindState() => new()
    {
        Id = rewindId,
        Position = transform.position,
        Rotation = transform.rotation,
        Active = gameObject.activeSelf,
        CustomJson = JsonUtility.ToJson(new CharacterSnapshot
        {
            ControlState = controlState,
            PresentingAsMin = presentingAsMin
        })
    };

    public void RestoreRewindState(RewindObjectState state)
    {
        transform.SetPositionAndRotation(state.Position, state.Rotation);
        gameObject.SetActive(state.Active);
        if (!string.IsNullOrWhiteSpace(state.CustomJson))
        {
            CharacterSnapshot snapshot = JsonUtility.FromJson<CharacterSnapshot>(state.CustomJson);
            controlState = snapshot.ControlState;
            presentingAsMin = snapshot.PresentingAsMin;
        }
        ApplyAppearance();
    }

    [System.Serializable]
    private struct CharacterSnapshot
    {
        public MotherControlState ControlState;
        public bool PresentingAsMin;
    }
}
