using UnityEngine;

// A solid tank barrier ignored by ball sweeps and enemy visibility checks.
[RequireComponent(typeof(BoxCollider2D))]
public class BallPassBarrier : MonoBehaviour
{
    [SerializeField] private TutorialTarget target;

    private void Update()
    {
        if (target != null && target.IsDestroyed) gameObject.SetActive(false);
    }
}
