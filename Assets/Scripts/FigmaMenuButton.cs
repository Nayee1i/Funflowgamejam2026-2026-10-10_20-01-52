using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FigmaMenuButton : Button
{
    [SerializeField] private RectTransform label;
    [SerializeField] private Image whiteText;
    [SerializeField] private float hoverScale = 1.06f;
    [SerializeField] private float fadeSeconds = .12f;
    private bool hovered;
    private float amount;

    public void Configure(RectTransform text, Image whiteOverlay)
    {
        label = text;
        whiteText = whiteOverlay;
        transition = Transition.None;
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData);
        hovered = true;
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);
        hovered = false;
    }

    private void Update()
    {
        amount = Mathf.MoveTowards(amount, hovered && IsInteractable() ? 1f : 0f,
            Time.unscaledDeltaTime / Mathf.Max(.01f, fadeSeconds));
        Apply(amount);
    }

    private void Apply(float value)
    {
        if (label != null) label.localScale = Vector3.one * Mathf.Lerp(1f, hoverScale, value);
        if (whiteText != null) whiteText.color = new Color(1f, 1f, 1f, value * .9f);
    }

    protected override void OnDisable()
    {
        hovered = false;
        amount = 0f;
        Apply(0f);
        base.OnDisable();
    }

#if UNITY_EDITOR
    public void PreviewHover() => Apply(1f);
#endif
}
