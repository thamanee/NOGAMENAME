using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(SisterDialogueUI))]
[RequireComponent(typeof(GraphicRaycaster))]
public class SisterDialogueBuilder : MonoBehaviour
{
    [Header("Font")]
    [SerializeField] private TMP_FontAsset thaiFont;

    [Header("Colors")]
    [SerializeField]
    private Color buttonColor =
        new Color(0.16f, 0.18f, 0.24f, 0.98f);

    [SerializeField]
    private Color accentColor =
        new Color(1f, 0.72f, 0.82f, 1f);

    private SisterDialogueUI dialogue;
    private GameObject generatedRoot;
    private SisterWorldDialogue worldSpeech;

    [Header("World speech (metres, relative to sister rotation)")]
    [SerializeField] private Vector3 speechOffset = new Vector3(.25f, 1.35f, .45f);
    [SerializeField, Min(.5f)] private float speechWidth = 2.6f;
    [SerializeField, Min(.5f)] private float speechFontSize = 3.4f;
    [SerializeField, Range(0f, .1f)] private float speechBob = .012f;
    [SerializeField] private Color speechColor = new Color(1f, .46f, .69f, 1f);

    private void Awake()
    {
        dialogue = GetComponent<SisterDialogueUI>();
        Build();
    }

    private void Build()
    {
        if (generatedRoot != null)
            return;

        RectTransform root = CreateRect(
            "SisterDialogue_Runtime",
            transform,
            Vector2.zero,
            Vector2.zero,
            Vector2.zero);

        Stretch(root);
        root.SetAsLastSibling();
        generatedRoot = root.gameObject;

        RectTransform choices = CreateRect("Choices", root, Vector2.zero, Vector2.zero, Vector2.zero);
        Stretch(choices);
        AddBackground(choices, Color.clear);

        RectTransform panel = CreateRect(
            "DialoguePanel",
            choices,
            new Vector2(0.5f, 0f),
            new Vector2(0f, 150f),
            new Vector2(1100f, 220f));

        // The old subtitle panel is now only a name heading for the choices.
        panel.anchoredPosition = new Vector2(400f, 515f);
        panel.sizeDelta = new Vector2(250f, 50f);

        TMP_Text speaker = CreateText(
            "SpeakerName",
            panel,
            new Vector2(0.5f, 1f),
            new Vector2(0f, -35f),
            new Vector2(250f, 45f),
            "มิน",
            28f,
            TextAlignmentOptions.MidlineLeft);

        speaker.color = accentColor;

        var speechObject = new GameObject("SisterSpeech_Runtime");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(speechObject, gameObject.scene);
        worldSpeech = speechObject.AddComponent<SisterWorldDialogue>();
        worldSpeech.Initialize(thaiFont != null ? thaiFont : TMP_Settings.defaultFontAsset,
            speechColor, speechOffset, speechWidth, speechFontSize, speechBob);
        TMP_Text message = worldSpeech.Text;
        TMP_Text hint = CreateText(
            "DialogueHint",
            root,
            new Vector2(0.5f, 0f),
            new Vector2(0f, 42f),
            new Vector2(1020f, 100f),
            "",
            22f,
            TextAlignmentOptions.Center);

        Button pet = CreateButton(
            "MemoryButton", choices, "คุยเรื่องอดีต",
            new Vector2(400f, 450f));

        Button command = CreateButton(
            "MiuButton", choices, "ถามเรื่องมิอุ",
            new Vector2(400f, 385f));

        Button talk = CreateButton(
            "TalkButton", choices, "คุยทั่วไป",
            new Vector2(400f, 320f));

        RectTransform commands = CreateRect(
            "CommandPanel",
            choices,
            Vector2.zero,
            Vector2.zero,
            Vector2.zero);

        Stretch(commands);

        Button follow = CreateButton(
            "FollowButton", commands, "ตามมา",
            new Vector2(400f, 450f));

        Button wait = CreateButton(
            "WaitButton", commands, "รอตรงนี้",
            new Vector2(400f, 385f));

        Button back = CreateButton(
            "BackButton", commands, "กลับ",
            new Vector2(400f, 320f));

        Button close = CreateButton(
            "CloseButton", choices, "จบบทสนทนา",
            new Vector2(400f, 240f));

        pet.onClick.AddListener(dialogue.ChoosePet);
        command.onClick.AddListener(dialogue.ChooseMiu);
        talk.onClick.AddListener(dialogue.ChooseTalk);

        follow.onClick.AddListener(dialogue.ChooseFollow);
        wait.onClick.AddListener(dialogue.ChooseWait);
        back.onClick.AddListener(dialogue.ShowMainMenu);
        close.onClick.AddListener(dialogue.Close);

        commands.gameObject.SetActive(false);
        dialogue.ConfigureWorldDialogue(worldSpeech, choices.gameObject, hint);

        dialogue.ConfigureGeneratedUI(
            generatedRoot,
            commands.gameObject,
            pet.gameObject,
            command.gameObject,
            talk.gameObject,
            speaker,
            message);
    }

    private void OnDestroy()
    {
        if (dialogue != null) dialogue.Close();
        if (worldSpeech != null) Destroy(worldSpeech.gameObject);
        if (generatedRoot != null) Destroy(generatedRoot);
    }

    private RectTransform CreateRect(
        string objectName,
        Transform parent,
        Vector2 anchor,
        Vector2 position,
        Vector2 size)
    {
        GameObject obj = new GameObject(
            objectName,
            typeof(RectTransform));

        obj.layer = gameObject.layer;

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);

        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        return rect;
    }

    private void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private Image AddBackground(
        RectTransform rect,
        Color color)
    {
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = true;
        return image;
    }

    private TMP_Text CreateText(
        string objectName,
        Transform parent,
        Vector2 anchor,
        Vector2 position,
        Vector2 size,
        string content,
        float fontSize,
        TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateRect(
            objectName,
            parent,
            anchor,
            position,
            size);

        TextMeshProUGUI text =
            rect.gameObject.AddComponent<TextMeshProUGUI>();

        text.font = thaiFont != null
            ? thaiFont
            : TMP_Settings.defaultFontAsset;

        text.text = content;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = alignment;
        text.raycastTarget = false;

        return text;
    }

    private Button CreateButton(
        string objectName,
        Transform parent,
        string label,
        Vector2 position)
    {
        RectTransform rect = CreateRect(
            objectName,
            parent,
            new Vector2(0.5f, 0f),
            position,
            new Vector2(250f, 54f));

        Image background = AddBackground(rect, buttonColor);

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = background;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor =
            new Color(1.2f, 1.2f, 1.2f, 1f);
        colors.pressedColor =
            new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.selectedColor = Color.white;
        colors.fadeDuration = 0.1f;
        button.colors = colors;

        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;

        CreateText(
            "Label",
            rect,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(230f, 48f),
            label,
            24f,
            TextAlignmentOptions.Center);

        return button;
    }
}
