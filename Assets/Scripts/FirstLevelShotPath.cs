using UnityEngine;
using System.Collections.Generic;

// Both the projectile and the aiming preview use this exact collision query.
public static class FirstLevelShotPath
{
    public const int MaxReflections=5;
    public const float Separation=.002f;
    static readonly List<RaycastHit2D> hits=new List<RaycastHit2D>(32);
    public struct Contact
    {
        public float distance;
        public Collider2D solid,mirrorCollider;
        public MirrorRicochetSurface mirror;
        public Vector2 normal;
        public bool Hit => solid!=null || mirror!=null;
        public Vector2 Reflect(Vector2 direction) => Vector2.Reflect(direction.normalized,normal).normalized;
    }
    public static Contact Cast(Vector2 origin,Vector2 direction,float radius,float distance,Collider2D lastMirrorCollider=null,bool hostile=false,TutorialTarget shooter=null)
    {
        float nearest=distance;
        Collider2D solid=null,mirrorCollider=null;
        MirrorRicochetSurface mirror=null;
        Vector2 contactNormal=Vector2.zero;
        var filter=new ContactFilter2D();filter.SetLayerMask(Physics2D.DefaultRaycastLayers);filter.useTriggers=Physics2D.queriesHitTriggers;
        Physics2D.CircleCast(origin,radius,direction,filter,hits,distance);
        foreach(var hit in hits)
        {
            if(hit.collider==null)continue;
            // Only ignore immediate re-contact, not this mirror for the rest of the flight.
            if(hit.collider==lastMirrorCollider && hit.distance<=Separation)continue;
            var enemy=hit.collider.GetComponentInParent<TutorialTarget>();
            if(hit.collider.isTrigger && (enemy==null || !enemy.IsShielded || enemy.ShieldCollider!=hit.collider))continue;
            if(enemy!=null && (hostile || enemy==shooter))continue;
            var surface=hit.collider.GetComponentInParent<MirrorRicochetSurface>();
            if(hit.distance<=nearest)
            {
                nearest=hit.distance;solid=surface==null ? hit.collider : null;
                mirror=surface;contactNormal=hit.normal;mirrorCollider=hit.collider;
            }
        }
        return new Contact{distance=nearest,solid=solid,mirror=mirror,mirrorCollider=mirrorCollider,normal=contactNormal};
    }
}
