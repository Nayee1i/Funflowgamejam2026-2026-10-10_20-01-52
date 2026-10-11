using UnityEngine;

public class FirstLevelShot : MonoBehaviour
{
    public float speed=10f;
    public float radius=.035f;
    public float lifetime=8f;
    public bool Charged { get; private set; }
    public int Damage => Charged ? 2 : 1;
    public Vector2 Direction { get; private set; }
    public int Reflections { get; private set; }
    public bool Finished { get; private set; }
    FirstLevelRooms level;
    float age;
    Collider2D lastMirrorCollider;
    const int MaxReflections=5;
    const float Separation=.002f;

    public static FirstLevelShot Create(Vector2 position,Vector2 direction,Sprite sprite,FirstLevelRooms owner,float speed=10f,float radius=.035f)
    {
        var go=new GameObject("Player Ball");
        go.transform.position=position;
        var render=go.AddComponent<SpriteRenderer>();render.sprite=sprite;render.sortingOrder=20;
        if(sprite!=null)go.transform.localScale=Vector3.one*(radius*2/Mathf.Max(.001f,sprite.bounds.size.x));
        var shot=go.AddComponent<FirstLevelShot>();shot.Direction=direction.normalized;shot.level=owner;shot.speed=speed;shot.radius=radius;
        return shot;
    }
    void Update()
    {
        if(Finished || (level!=null && !level.CanPlay))return;
        age+=Time.deltaTime;
        if(age>=lifetime){Finish();return;}
        Step(speed*Time.deltaTime);
    }
    public void Step(float distance)
    {
        if(Finished)return;
        Physics2D.SyncTransforms();
        for(int pass=0;pass<MaxReflections+2 && distance>0;pass++)
        {
            Vector2 origin=transform.position;
            float nearest=distance;
            Collider2D solid=null;
            MirrorRicochetSurface mirror=null;
            Vector2 contactNormal=Vector2.zero;
            Collider2D mirrorCollider=null;
            foreach(var hit in Physics2D.CircleCastAll(origin,radius,Direction,distance))
            {
                if(hit.collider==null || hit.collider.isTrigger || hit.collider==lastMirrorCollider)continue;
                var surface=hit.collider.GetComponentInParent<MirrorRicochetSurface>();
                if(surface!=null && !surface.reflectFromCollider)continue;
                if(hit.distance<=nearest)
                {
                    nearest=hit.distance;solid=surface==null ? hit.collider : null;
                    mirror=surface;contactNormal=hit.normal;mirrorCollider=hit.collider;
                }
            }
            foreach(var surface in FindObjectsByType<MirrorRicochetSurface>(FindObjectsSortMode.None))
            {
                if(surface.reflectFromCollider)continue;
                Vector2 normal=surface.ReflectionNormal;
                float dot=Vector2.Dot(Direction,normal);
                if(Mathf.Abs(dot)<.0001f)continue;
                float t=Vector2.Dot(surface.ReflectionPoint-origin,normal)/dot;
                if(t<Separation*.5f || t>nearest)continue;
                Vector2 tangent=new Vector2(-normal.y,normal.x);
                var renderer=surface.GetComponent<SpriteRenderer>();
                float halfWidth=renderer!=null && renderer.sprite!=null ? renderer.sprite.bounds.size.x*Mathf.Abs(surface.transform.lossyScale.x)*.28f : .5f;
                if(Mathf.Abs(Vector2.Dot(origin+Direction*t-surface.ReflectionPoint,tangent))>halfWidth+radius)continue;
                nearest=t;mirror=surface;solid=null;mirrorCollider=null;
            }
            transform.position=origin+Direction*nearest;
            distance-=nearest;
            if(mirror!=null)
            {
                if(Reflections>=MaxReflections){Finish();return;}
                Direction=mirror.reflectFromCollider ? Vector2.Reflect(Direction,contactNormal).normalized : mirror.Reflect(Direction);
                lastMirrorCollider=mirrorCollider;Reflections++;
                Charged|=mirror.chargesShot;
                GetComponent<SpriteRenderer>().color=Charged ? new Color(1,.2f,.15f) : Color.white;
                transform.position+=(Vector3)(Direction*Separation);
                distance=Mathf.Max(0,distance-Separation);
            }
            else if(solid!=null)
            {
                var target=solid.GetComponentInParent<FirstLevelShotTarget>();
                if(target!=null)target.Hit(Damage,Charged);
                Finish();return;
            }
            else return;
        }
    }
    void Finish(){Finished=true;gameObject.SetActive(false);if(Application.isPlaying)Destroy(gameObject);else DestroyImmediate(gameObject);}
    public static void ClearAll()
    {
        foreach(var shot in FindObjectsByType<FirstLevelShot>(FindObjectsSortMode.None))
            if(Application.isPlaying)Destroy(shot.gameObject);else DestroyImmediate(shot.gameObject);
    }
}
