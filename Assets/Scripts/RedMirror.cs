using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class RedMirror : MonoBehaviour
{
    private void Reset()
    {
        gameObject.layer = 0;
        var collider = GetComponent<Collider2D>();
        if (collider == null) collider = gameObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = false;
        GetComponent<SpriteRenderer>().color = new Color(1f, 0.15f, 0.2f);
    }
}
