using UnityEngine;

// Both the projectile and the aiming preview use this exact collision query.
public static class FirstLevelShotPath
{
    public const int MaxReflections=5;
    public const float Separation=.002f;
    public struct Contact
    {
        public float distance;
        public Collider2D solid,mirrorCollider;
        public MirrorRicochetSurface mirror;
        public Vector2 normal;
        public bool Hit => solid!=null || mirror!=null;
        public Vector2 Reflect(Vector2 direction) => mirror.reflectFromCollider ? Vector2.Reflect(direction,normal).normalized : mirror.Reflect(direction);
    }
    public static Contact Cast(Vector2 origin,Vector2 direction,float radius,float distance,Collider2D lastMirrorCollider=null,bool hostile=false,TutorialTarget shooter=null)
    {
        float nearest=distance;
        Collider2D solid=null,mirrorCollider=null;
        MirrorRicochetSurface mirror=null;
        Vector2 contactNormal=Vector2.zero;
            foreach(var hit in Physics2D.CircleCastAll(origin,radius,direction,distance))
            {
                if(hit.collider==null || hit.collider==lastMirrorCollider)continue;
                var enemy=hit.collider.GetComponentInParent<TutorialTarget>();
                if(hit.collider.isTrigger && (enemy==null || !enemy.IsShielded || enemy.ShieldCollider!=hit.collider))continue;
                if(enemy!=null && (hostile || enemy==shooter))continue;
                var surface=hit.collider.GetComponentInParent<MirrorRicochetSurface>();
                if(surface!=null && !surface.reflectFromCollider)continue;
                if(hit.distance<=nearest)
                {
                    nearest=hit.distance;solid=surface==null ? hit.collider : null;
                    
                    mirror=surface;contactNormal=hit.normal;mirrorCollider=hit.collider;
                }
            }
            foreach(var surface in Object.FindObjectsByType<MirrorRicochetSurface>())
            {
                if(surface.reflectFromCollider)continue;
                Vector2 normal=surface.ReflectionNormal;
                float dot=Vector2.Dot(direction,normal);
                if(Mathf.Abs(dot)<.0001f)continue;
                float t=Vector2.Dot(surface.ReflectionPoint-origin,normal)/dot;
                if(t<Separation*.5f || t>nearest)continue;
                Vector2 tangent=new Vector2(-normal.y,normal.x);
                var renderer=surface.GetComponent<SpriteRenderer>();
                float halfWidth=renderer!=null && renderer.sprite!=null ? renderer.sprite.bounds.size.x*Mathf.Abs(surface.transform.lossyScale.x)*.28f : .5f;
                if(Mathf.Abs(Vector2.Dot(origin+direction*t-surface.ReflectionPoint,tangent))>halfWidth+radius)continue;
                nearest=t;mirror=surface;solid=null;mirrorCollider=null;
                
            }
        return new Contact{distance=nearest,solid=solid,mirror=mirror,mirrorCollider=mirrorCollider,normal=contactNormal};
    }
}
