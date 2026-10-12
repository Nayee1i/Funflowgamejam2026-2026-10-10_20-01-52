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
    TutorialTarget shooter;
    bool hostile;
    int damageHalves=2;
    public void SetEnemyShooter(TutorialTarget enemy)
    {
        shooter=enemy;hostile=true;name="Enemy Ball";
        damageHalves=enemy!=null && enemy.TryGetComponent<TutorialRobot>(out var robot) ? robot.DamageHalves : 2;
        FirstLevelEffects.ProjectileVisual(gameObject,true,level);OrientVisual();
    }
    const int MaxReflections=FirstLevelShotPath.MaxReflections;
    const float Separation=FirstLevelShotPath.Separation;

    public static FirstLevelShot Create(Vector2 position,Vector2 direction,Sprite sprite,FirstLevelRooms owner,float speed=10f,float radius=.035f)
    {
        var go=new GameObject("Player Ball");
        go.transform.position=position;
        var render=go.AddComponent<SpriteRenderer>();render.sprite=sprite;render.sortingOrder=20;
        if(sprite!=null)go.transform.localScale=Vector3.one*(radius*2/Mathf.Max(.001f,sprite.bounds.size.x));
        var shot=go.AddComponent<FirstLevelShot>();shot.Direction=direction.normalized;shot.level=owner;shot.speed=speed;shot.radius=radius;
        FirstLevelEffects.ProjectileVisual(go,false,owner);shot.OrientVisual();
        return shot;
    }
    void OrientVisual(){transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(Direction.y,Direction.x)*Mathf.Rad2Deg+180);}
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
        if(hostile && PeacefulModeSettings.Enabled){Finish();return;}
        Physics2D.SyncTransforms();
        for(int pass=0;pass<MaxReflections+2 && distance>0;pass++)
        {
            Vector2 origin=transform.position;
            float nearest=distance;
            Collider2D solid=null;
            MirrorRicochetSurface mirror=null;
            Collider2D mirrorCollider=null;
            bool hitPlayer=false;
            if((hostile || Reflections>0) && level!=null)
            {
                Vector2 offset=origin-(Vector2)level.player.position;
                float combined=radius+level.playerRadius;
                float b=Vector2.Dot(offset,Direction);
                float c=offset.sqrMagnitude-combined*combined;
                float discriminant=b*b-c;
                if(discriminant>=0)
                {
                    float t=c<=0 ? 0 : -b-Mathf.Sqrt(discriminant);
                    if(t>=0 && t<=nearest){nearest=t;hitPlayer=true;}
                }
            }
            var contact=FirstLevelShotPath.Cast(origin,Direction,radius,nearest,lastMirrorCollider,hostile,shooter);
            if(contact.Hit)
            {
                nearest=contact.distance;solid=contact.solid;mirror=contact.mirror;
                mirrorCollider=contact.mirrorCollider;hitPlayer=false;
            }
            transform.position=origin+Direction*nearest;
            distance-=nearest;
            if(hitPlayer)
            {
                level.player.GetComponent<TutorialPlayer>().Hit(damageHalves);
                FirstLevelEffects.Impact(transform.position,.8f,level,hostile || Charged);
                Finish();return;
            }
            if(mirror!=null)
            {
                if(Reflections>=MaxReflections){Finish();return;}
                Direction=contact.Reflect(Direction);
                lastMirrorCollider=mirrorCollider;Reflections++;
                Charged|=mirror.chargesShot;
                GameAudio.Play(mirror.chargesShot ? "charge" : "bounce");
                if(Charged)FirstLevelEffects.ProjectileVisual(gameObject,hostile,level,true);
                OrientVisual();
                transform.position+=(Vector3)(Direction*Separation);
                distance=Mathf.Max(0,distance-Separation);
            }
            else if(solid!=null)
            {
                var robot=solid.GetComponentInParent<TutorialTarget>();
                bool hadShield=robot!=null && robot.IsShielded;
                if(robot!=null && !hostile)robot.Hit(Reflections>0,Charged);
                bool shieldBroken=hadShield && !robot.IsShielded;
                if(shieldBroken)FirstLevelEffects.Impact(transform.position,1.7f,level);
                float effectSize=robot!=null && robot.IsDestroyed ? 2.2f : .7f;
                FirstLevelEffects.Impact(transform.position,effectSize,level,Charged || hostile);
                var target=solid.GetComponentInParent<FirstLevelShotTarget>();
                if(target!=null)target.Hit(Damage,Charged);
                var cover=solid.GetComponentInParent<DestructibleCover>();
                if(cover!=null && !hostile)cover.Hit(Reflections>0 ? 5 : 1);
                if(shieldBroken)GameAudio.Play("shield_break");
                else if(robot!=null && !hostile){if(!robot.IsDestroyed)GameAudio.Play("enemy_hit");}
                else if(cover==null || hostile)GameAudio.Play("wall_hit");
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
