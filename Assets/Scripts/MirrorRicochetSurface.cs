using UnityEngine;

public class MirrorRicochetSurface : MonoBehaviour
{
    public Vector2 localReflectionPoint;
    public bool chargesShot;
    public bool reflectFromCollider;
    public Vector2 ReflectionPoint => transform.TransformPoint(localReflectionPoint);
    public Vector2 ReflectionNormal => transform.up;
    public Vector2 Reflect(Vector2 incoming) => Vector2.Reflect(incoming.normalized, ReflectionNormal).normalized;
}
