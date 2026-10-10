using UnityEngine;

// Attach this component to a solid rectangular surface to reflect ball shots.
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
public class BlueMirror : MonoBehaviour
{
    private void Reset()
    {
        gameObject.layer = 0;
        GetComponent<BoxCollider2D>().isTrigger = false;
        GetComponent<SpriteRenderer>().color = new Color(0.12f, 0.52f, 1f);
    }
}
