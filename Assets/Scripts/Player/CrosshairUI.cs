using UnityEngine;
using TMPro;
using System.Collections;

public class CrosshairUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI dot;
    [SerializeField] private RectTransform circle;
    [SerializeField] private TextMeshProUGUI eText;

    [Header("Circle Size")]
    [SerializeField] private float circleSize = 24f;

    [Header("E Size")]
    [SerializeField] private float eSize = 18f;

    [Header("E Position")]
    [SerializeField] private float eOffsetY = -25f;

    [Header("Animation")]
    [SerializeField] private float animationDuration = 0.15f;
    [SerializeField] private float startScale = 0.3f;

    private Coroutine animationCoroutine;
    private bool isInteractable;

    private void Start()
    {
        ApplySize();
        SetNormal(true);
    }

    private void ApplySize()
    {
        // ขนาดวงกลม
        circle.sizeDelta = new Vector2(circleSize, circleSize);

        // ขนาดตัว E
        eText.fontSize = eSize;

        // ตำแหน่ง E
        eText.rectTransform.anchoredPosition =
            new Vector2(0f, eOffsetY);
    }

    public void SetNormal(bool force = false)
    {
        if (!force && !isInteractable)
            return;

        isInteractable = false;

        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        dot.gameObject.SetActive(true);
        circle.gameObject.SetActive(false);
        eText.gameObject.SetActive(false);
    }

    public void SetInteractable(bool requiresKey, string prompt = null)
    {
        // ถ้ายังอยู่บน interactable อยู่ ไม่ต้องเล่น animation ซ้ำ
        if (isInteractable)
        {
            eText.text = string.IsNullOrWhiteSpace(prompt) ? "E" : $"E  {prompt}";
            eText.gameObject.SetActive(requiresKey);
            return;
        }

        isInteractable = true;

        dot.gameObject.SetActive(false);
        circle.gameObject.SetActive(true);

        // E แสดงเฉพาะ interaction ที่ต้องกด E
        eText.text = string.IsNullOrWhiteSpace(prompt) ? "E" : $"E  {prompt}";
        eText.gameObject.SetActive(requiresKey);

        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(ExpandCircle());
    }

    private IEnumerator ExpandCircle()
    {
        circle.localScale = Vector3.one * startScale;

        float time = 0f;

        while (time < animationDuration)
        {
            time += Time.deltaTime;

            float t = time / animationDuration;
            t = Mathf.SmoothStep(0f, 1f, t);

            circle.localScale = Vector3.Lerp(
                Vector3.one * startScale,
                Vector3.one,
                t
            );

            yield return null;
        }

        circle.localScale = Vector3.one;
    }
}
