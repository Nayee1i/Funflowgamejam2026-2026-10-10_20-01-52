using UnityEngine;

public class FirstLevelRicochetRoute : MonoBehaviour
{
    public MirrorRicochetSurface redMirror, blueMirror;
    public Vector2 shotOrigin, futureBossPosition;
    private void OnDrawGizmosSelected()
    {
        if(redMirror==null||blueMirror==null)return;
        Gizmos.color=Color.yellow;
        Gizmos.DrawLine(shotOrigin,redMirror.ReflectionPoint);
        Gizmos.color=Color.red;
        Gizmos.DrawLine(redMirror.ReflectionPoint,blueMirror.ReflectionPoint);
        Gizmos.color=Color.cyan;
        Gizmos.DrawLine(blueMirror.ReflectionPoint,futureBossPosition);
        Gizmos.DrawWireSphere(futureBossPosition,.12f);
    }
}
