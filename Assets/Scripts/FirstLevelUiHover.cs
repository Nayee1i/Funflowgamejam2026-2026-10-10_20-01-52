using UnityEngine;
using UnityEngine.EventSystems;

public class FirstLevelUiHover : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
{
    bool hovered;
    public void OnPointerEnter(PointerEventData eventData){hovered=true;}
    public void OnPointerExit(PointerEventData eventData){hovered=false;}
    void Update(){transform.localScale=Vector3.Lerp(transform.localScale,Vector3.one*(hovered ? 1.08f : 1),Time.unscaledDeltaTime*18);}
}
