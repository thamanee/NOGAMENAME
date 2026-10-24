using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class SisterDialogueUI : MonoBehaviour
{
    [Header("Player References")]
    [SerializeField] private PlayerController playerMovement;
    [SerializeField] private PlayerLook playerLook;
    [SerializeField] private InteractionRaycast playerInteraction;

    [Header("Speaker Colors")]
    [SerializeField]
    private Color playerSpeechColor =
    new Color(0.45f, 0.85f, 1f, 1f);

    [SerializeField]
    private Color sisterSpeechColor =
        new Color(1f, 0.46f, 0.69f, 1f);

    [Header("Reading Time")]
    [Tooltip("เวลาค้างหลังข้อความเราแสดงครบ ก่อนน้องตอบ")]
    [SerializeField, Min(0f)]
    private float playerReadDelay = 1.5f;

    [Tooltip("เวลาค้างหลังข้อความน้องแสดงครบ ก่อนเปิดตัวเลือก")]
    [SerializeField, Min(0f)]
    private float sisterReadDelay = 1f;

    private float speechFinishedAt = -1f;

    [Tooltip("ใส่เฉพาะกลุ่ม Crosshair ห้ามใส่ Canvas ทั้งอัน")]
    [SerializeField] private GameObject crosshairRoot;

    [Header("Dialogue")]
    [SerializeField] private string sisterName = "มิน";
    [SerializeField] private string playerName = "เรา";

    [SerializeField, Min(1f)]
    private float charactersPerSecond = 35f;

    [SerializeField, TextArea(2, 4)]
    private string greeting = "กลับมาจริง ๆ สินะ... ฉันนึกว่าจะไม่ได้เจอกันอีกแล้ว";

    [SerializeField, TextArea(2, 4)]
    private string[] generalLines =
    {
        "ยังจำตอนที่เราชอบแอบเอาขนมไปกินหลังบ้านได้ไหม?",
        "ฉันเก็บรูปเก่าเอาไว้เกือบหมดเลยนะ",
        "บ้านหลังนี้เงียบไปเยอะ ตั้งแต่เธอย้ายออกไป"
    };

    public bool IsOpen { get; private set; }
    public bool IsChoosing { get; private set; }
    public SisterWorldDialogue Speech { get; private set; }

    private GameObject dialogueRoot;
    private GameObject choicesRoot;
    private GameObject commandPanel;

    private GameObject petButton;
    private GameObject commandButton;
    private GameObject talkButton;

    private TMP_Text nameText;
    private TMP_Text hintText;

    private Camera dialogueCamera;

    private SisterFollow currentSister;
    private SisterInteractable currentInteraction;
    private SisterHeadLook currentHeadLook;
    private NarrativeCharacter currentNarrativeCharacter;

    private string pendingSisterReply;
    private bool pendingPetReaction;

    private bool followAfterDialogue;
    private bool movementWasEnabled;
    private bool lookWasEnabled;
    private bool interactionWasEnabled;

    private bool crosshairWasActive;
    private bool crosshairWasHidden;

    private bool cursorWasVisible;
    private CursorLockMode previousCursorLock;

    private int nextGeneralLine;
    private int openedFrame;

    // SisterDialogueBuilder เรียกเพื่อเชื่อมข้อความลอย
    public void ConfigureWorldDialogue(
        SisterWorldDialogue speech,
        GameObject choices,
        TMP_Text hint)
    {
        Speech = speech;
        choicesRoot = choices;
        hintText = hint;
    }

    // คงรูปแบบเดิมเพื่อใช้กับ SisterDialogueBuilder เดิมได้
    public void ConfigureGeneratedUI(
        GameObject root,
        GameObject commands,
        GameObject pet,
        GameObject command,
        GameObject talk,
        TMP_Text speaker,
        TMP_Text message)
    {
        dialogueRoot = root;
        commandPanel = commands;

        petButton = pet;
        commandButton = command;
        talkButton = talk;

        nameText = speaker;

        if (dialogueRoot != null)
            dialogueRoot.SetActive(false);
    }

    private void Update()
    {
        if (!IsOpen)
            return;

        if (currentSister == null ||
            currentInteraction == null ||
            !currentSister.isActiveAndEnabled ||
            !currentInteraction.isActiveAndEnabled ||
            dialogueCamera == null ||
            !dialogueCamera.isActiveAndEnabled ||
            Speech == null)
        {
            Close();
            return;
        }

        if (Time.frameCount == openedFrame)
            return;

        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Close();
            return;
        }

        if (!IsChoosing)
        {
            if (Speech.IsTyping)
            {
                speechFinishedAt = -1f;
            }
            else
            {
                // เริ่มจับเวลาเมื่อข้อความแสดงครบแล้วเท่านั้น
                if (speechFinishedAt < 0f)
                    speechFinishedAt = Time.unscaledTime;

                bool waitingForSister = pendingSisterReply != null;

                float readDelay = waitingForSister
                    ? playerReadDelay
                    : sisterReadDelay;

                if (Time.unscaledTime - speechFinishedAt >= readDelay)
                {
                    if (waitingForSister)
                    {
                        string reply = pendingSisterReply;
                        bool playPetReaction = pendingPetReaction;

                        Say(reply);

                        if (playPetReaction &&
                            currentInteraction != null)
                        {
                            currentInteraction.PlayHeadPatReaction();
                        }
                    }
                    else
                    {
                        Advance();
                    }
                }
            }
        }

        UpdateHint();
    }

    public void Open(
        SisterFollow sister,
        SisterInteractable interaction)
    {
        if (!isActiveAndEnabled ||
            IsOpen ||
            sister == null ||
            interaction == null ||
            !sister.isActiveAndEnabled ||
            !interaction.isActiveAndEnabled)
        {
            return;
        }

        if (dialogueRoot == null ||
            choicesRoot == null ||
            commandPanel == null ||
            petButton == null ||
            commandButton == null ||
            talkButton == null ||
            nameText == null ||
            Speech == null)
        {
            Debug.LogError(
                "Dialogue UI ยังไม่พร้อม ตรวจ SisterDialogueBuilder บน Canvas",
                this);
            return;
        }

        if (playerMovement == null ||
            playerLook == null ||
            playerInteraction == null)
        {
            Debug.LogError(
                "กรุณาใส่ Player References ให้ครบใน SisterDialogueUI",
                this);
            return;
        }

        dialogueCamera =
            playerLook.GetComponentInChildren<Camera>();

        if (dialogueCamera == null ||
            !dialogueCamera.isActiveAndEnabled)
        {
            Debug.LogError(
                "ต้องมีกล้องที่เปิดใช้งานอยู่ใต้ GameObject ของ PlayerLook",
                this);
            return;
        }

        currentSister = sister;
        currentInteraction = interaction;
        currentNarrativeCharacter = sister.GetComponent<NarrativeCharacter>();

        if (currentNarrativeCharacter != null &&
            currentNarrativeCharacter.Definition != null)
        {
            sisterName = currentNarrativeCharacter.DisplayName;
            sisterSpeechColor = currentNarrativeCharacter.Definition.SpeechColor;
            if (!string.IsNullOrWhiteSpace(currentNarrativeCharacter.Definition.Greeting))
                greeting = currentNarrativeCharacter.Definition.Greeting;
        }

        followAfterDialogue = sister.IsFollowingEnabled;

        // จำสถานะก่อนเริ่มคุย
        movementWasEnabled = playerMovement.enabled;
        lookWasEnabled = playerLook.enabled;
        interactionWasEnabled = playerInteraction.enabled;

        previousCursorLock = Cursor.lockState;
        cursorWasVisible = Cursor.visible;

        openedFrame = Time.frameCount;
        IsOpen = true;
        IsChoosing = false;

        // หยุดเดิน แต่ยังให้หมุนกล้องระหว่างอ่านข้อความ
        sister.SetFollowing(false);
        playerMovement.enabled = false;
        playerInteraction.enabled = false;

        // ป้องกันการซ่อน Canvas หรือ UI บทสนทนาเอง
        crosshairWasHidden =
            crosshairRoot != null &&
            !transform.IsChildOf(crosshairRoot.transform) &&
            !dialogueRoot.transform.IsChildOf(
                crosshairRoot.transform) &&
            !crosshairRoot.transform.IsChildOf(
                dialogueRoot.transform);

        if (crosshairWasHidden)
        {
            crosshairWasActive = crosshairRoot.activeSelf;
            crosshairRoot.SetActive(false);
        }

        Speech.Bind(sister.transform, dialogueCamera);

        dialogueRoot.SetActive(true);
        nameText.text = sisterName;

        currentHeadLook =
            sister.GetComponentInChildren<SisterHeadLook>();

        if (currentHeadLook != null)
            currentHeadLook.SetTalking(true);

        Say(greeting);
    }

    // เริ่มคำพูดของน้อง
    private void Say(string message)
    {
        pendingSisterReply = null;
        pendingPetReaction = false;

        BeginSpeech(sisterName, message, sisterSpeechColor);
    }

    // เริ่มคำพูดเรา และเก็บคำตอบน้องไว้เล่นต่อ
    private void SayPlayerThenSister(
    string playerLine,
    string sisterReply,
    bool petReaction = false)
    {
        pendingSisterReply = sisterReply ?? "";
        pendingPetReaction = petReaction;

        BeginSpeech(playerName, playerLine, playerSpeechColor);
    }

    private void BeginSpeech(
    string speaker,
    string message,
    Color textColor)
    {
        IsChoosing = false;
        speechFinishedAt = -1f;

        choicesRoot.SetActive(false);

        playerLook.enabled = lookWasEnabled;

        Cursor.lockState = lookWasEnabled
            ? CursorLockMode.Locked
            : previousCursorLock;

        Cursor.visible = lookWasEnabled
            ? false
            : cursorWasVisible;

        Speech.Show(
            $"{speaker}: {message ?? ""}",
            charactersPerSecond);

        if (Speech.Text != null)
            Speech.Text.color = textColor;

        UpdateHint();
    }

    // เปิดเมนูได้เมื่อข้อความครบและไม่มีคำตอบรออยู่เท่านั้น
    // ไม่เร่งหรือข้ามข้อความ
    public void Advance()
    {
        if (!IsOpen ||
            IsChoosing ||
            Speech == null ||
            Speech.IsTyping ||
            pendingSisterReply != null)
        {
            return;
        }

        IsChoosing = true;

        // หยุดกล้องและปล่อยเมาส์สำหรับเลือกเมนู
        playerLook.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        choicesRoot.SetActive(true);
        ShowMainMenu();
        UpdateHint();
    }

    public void ShowMainMenu()
    {
        if (!IsOpen || !IsChoosing)
            return;

        commandPanel.SetActive(false);

        petButton.SetActive(true);
        commandButton.SetActive(true);
        talkButton.SetActive(true);
    }

    public void ShowCommands()
    {
        if (!IsOpen || !IsChoosing)
            return;

        petButton.SetActive(false);
        commandButton.SetActive(false);
        talkButton.SetActive(false);

        commandPanel.SetActive(true);
    }

    public void ChooseFollow()
    {
        if (!IsOpen || !IsChoosing)
            return;

        followAfterDialogue = true;

        SayPlayerThenSister(
            "ตามมาหน่อยได้ไหม?",
            "ได้เลย เดี๋ยวฉันตามไปนะ");
    }

    public void ChooseWait()
    {
        if (!IsOpen || !IsChoosing)
            return;

        followAfterDialogue = false;

        SayPlayerThenSister(
            "รออยู่ตรงนี้ก่อนนะ",
            "อื้ม ฉันจะรออยู่ตรงนี้");
    }

    public void ChoosePet()
    {
        if (!IsOpen ||
            !IsChoosing ||
            currentInteraction == null)
        {
            return;
        }

        SayPlayerThenSister(
            "ยังเก็บของสมัยเด็กไว้อยู่เหรอ?",
            "อื้ม... บางอย่างฉันทิ้งไม่ลงจริง ๆ");
    }

    public void ChooseMiu()
    {
        if (!IsOpen || !IsChoosing)
            return;

        GameStateService state = GameStateService.Instance;
        if (state != null && state.HasKnowledge(GameFlagId.KnowMiuIdentity))
        {
            SayPlayerThenSister(
                "ฉันรู้ว่ามิอุไม่ใช่มนุษย์ และเธอกำลังปกป้องมิอุอยู่",
                "...ถ้าเธอรู้แล้ว ฉันจะเล่าทุกอย่างให้ฟัง");
            state.Learn(GameFlagId.KnowMinMiuHistory);
        }
        else
        {
            SayPlayerThenSister(
                "มิอุเป็นใครเหรอ?",
                "เป็นเพื่อนที่สำคัญมากของฉัน แต่อย่าเพิ่งถามมากกว่านี้เลยนะ");
        }
    }

    public void ChooseTalk()
    {
        if (!IsOpen || !IsChoosing)
            return;

        string reply = "ฉันฟังอยู่นะ";

        if (generalLines != null && generalLines.Length > 0)
        {
            nextGeneralLine %= generalLines.Length;
            reply = generalLines[nextGeneralLine];

            nextGeneralLine =
                (nextGeneralLine + 1) % generalLines.Length;
        }

        SayPlayerThenSister(
            "มาคุยกันหน่อยไหม?",
            reply);
    }

    private void UpdateHint()
    {
        if (hintText == null)
            return;

        hintText.text = IsChoosing
            ? "เลือกด้วยเมาส์  •  Esc จบบทสนทนา"
            : "Esc จบบทสนทนา";
    }

    public void Close()
    {
        if (!IsOpen)
            return;

        IsOpen = false;
        IsChoosing = false;

        pendingSisterReply = null;
        pendingPetReaction = false;

        if (Speech != null)
            Speech.Hide();

        if (dialogueRoot != null)
            dialogueRoot.SetActive(false);

        if (currentHeadLook != null)
            currentHeadLook.SetTalking(false);

        // ตามหรือรอตามคำสั่งล่าสุด หลังจบบทสนทนา
        if (currentSister != null)
            currentSister.SetFollowing(followAfterDialogue);

        if (playerMovement != null)
            playerMovement.enabled = movementWasEnabled;

        if (playerLook != null)
            playerLook.enabled = lookWasEnabled;

        if (playerInteraction != null)
            playerInteraction.enabled = interactionWasEnabled;

        if (crosshairWasHidden && crosshairRoot != null)
            crosshairRoot.SetActive(crosshairWasActive);

        // คืนเคอร์เซอร์หลังเปิด PlayerLook
        Cursor.lockState = previousCursorLock;
        Cursor.visible = cursorWasVisible;

        crosshairWasHidden = false;

        currentSister = null;
        currentInteraction = null;
        currentHeadLook = null;
        currentNarrativeCharacter = null;
        dialogueCamera = null;
    }

    private void OnDisable()
    {
        Close();
    }
}
