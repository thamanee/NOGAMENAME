using System.Globalization;
using TMPro;
using UnityEngine;

/// <summary>One reusable world-space text mesh; no additional Canvas.</summary>
[DisallowMultipleComponent]
public sealed class SisterWorldDialogue : MonoBehaviour
{
    private TextMeshPro label;
    private Transform speaker;
    private Camera viewCamera;
    private Material ownedMaterial;
    private Vector3 offset;
    private float bob, elapsed, rate;
    private int[] visibleCounts = new int[0];
    private int revealed;
    private bool shown;

    public bool IsTyping => shown && revealed < visibleCounts.Length;
    public TMP_Text Text => label;

    public void Initialize(TMP_FontAsset font, Color color, Vector3 worldOffset,
        float width, float fontSize, float bobAmount)
    {
        offset = worldOffset;
        bob = bobAmount;
        label = gameObject.AddComponent<TextMeshPro>();
        label.font = font;
        label.color = color;
        label.richText = false;
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = fontSize;
        label.enableAutoSizing = true;
        label.fontSizeMin = fontSize * .65f;
        label.fontSizeMax = fontSize;
        label.rectTransform.sizeDelta = new Vector2(width, .85f);
        label.raycastTarget = false;
        if (font != null && font.material != null)
        {
            ownedMaterial = new Material(font.material);
            ownedMaterial.SetColor(ShaderUtilities.ID_OutlineColor, Color.white);
            ownedMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, .18f);
            label.fontSharedMaterial = ownedMaterial;
        }
        Hide();
    }

    public void Bind(Transform target, Camera camera)
    {
        speaker = target;
        viewCamera = camera;
    }

    public void Show(string message, float charactersPerSecond)
    {
        if (speaker == null || viewCamera == null || label == null) return;
        shown = true;
        gameObject.SetActive(true);
        label.text = message ?? "";
        label.maxVisibleCharacters = int.MaxValue;
        label.ForceMeshUpdate();
        // Reveal a base character and its combining marks together (including Thai marks).
        int[] starts = StringInfo.ParseCombiningCharacters(label.text);
        visibleCounts = new int[starts.Length];
        int character = 0;
        for (int i = 0; i < starts.Length; i++)
        {
            int end = i + 1 < starts.Length ? starts[i + 1] : label.text.Length;
            while (character < label.textInfo.characterCount &&
                   label.textInfo.characterInfo[character].index < end) character++;
            visibleCounts[i] = character;
        }
        elapsed = 0;
        revealed = 0;
        rate = Mathf.Max(1, charactersPerSecond);
        label.maxVisibleCharacters = 0;
        PositionText(true);
    }

    public void RevealAll()
    {
        revealed = visibleCounts.Length;
        if (label != null) label.maxVisibleCharacters = label.textInfo.characterCount;
    }

    public void Hide()
    {
        shown = false;
        if (label != null) label.text = "";
        gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (!shown || speaker == null || viewCamera == null) { Hide(); return; }
        elapsed += Time.unscaledDeltaTime;
        if (IsTyping)
        {
            revealed = Mathf.Min(visibleCounts.Length, Mathf.FloorToInt(elapsed * rate));
            label.maxVisibleCharacters = revealed > 0 ? visibleCounts[revealed - 1] : 0;
        }
        PositionText(false);
    }

    private void PositionText(bool instant)
    {
        // Offset is in metres, independent of the imported model's scale.
        Vector3 target = speaker.position + speaker.rotation * offset;
        target += Vector3.up * (Mathf.Sin(elapsed * 2f) * bob);
        transform.position = instant ? target : Vector3.Lerp(transform.position, target,
            1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
        Vector3 away = transform.position - viewCamera.transform.position;
        if (away.sqrMagnitude > .0001f)
            transform.rotation = Quaternion.LookRotation(away, viewCamera.transform.up);
        label.renderer.enabled = Vector3.Dot(viewCamera.transform.forward, away) > 0f;
    }

    private void OnDestroy()
    {
        if (ownedMaterial != null) Destroy(ownedMaterial);
    }
}
