using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
public class RedMirror : MonoBehaviour
{
    private void Reset()
    {
        gameObject.layer = 0;
        GetComponent<BoxCollider2D>().isTrigger = false;
        GetComponent<SpriteRenderer>().color = new Color(1f, 0.15f, 0.2f);
    }
}
