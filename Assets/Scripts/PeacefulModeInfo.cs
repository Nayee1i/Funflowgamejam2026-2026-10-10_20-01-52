using UnityEngine;
using UnityEngine.EventSystems;

public sealed class PeacefulModeInfo : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject tooltip;
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltip != null) tooltip.SetActive(true);
    }
    public void OnPointerExit(PointerEventData eventData) => Hide();
    private void OnDisable() => Hide();
    private void Hide()
    {
        if (tooltip != null) tooltip.SetActive(false);
    }
}
