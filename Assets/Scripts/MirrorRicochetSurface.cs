using UnityEngine;

// Marks a physical Collider2D as reflective. Collision normals determine every bounce.
[DisallowMultipleComponent]
public class MirrorRicochetSurface : MonoBehaviour
{
    public bool chargesShot;
    // Legacy editor authoring data. These values no longer affect projectile collisions.
    [HideInInspector] public Vector2 localReflectionPoint;
    [HideInInspector] public bool reflectFromCollider;
    public Vector2 ReflectionPoint => transform.TransformPoint(localReflectionPoint);
    public Vector2 ReflectionNormal => transform.up;
    // Used by older level authoring tools to calculate intended routes, not by projectiles.
    public Vector2 Reflect(Vector2 incoming) => Vector2.Reflect(incoming.normalized, ReflectionNormal).normalized;
}
