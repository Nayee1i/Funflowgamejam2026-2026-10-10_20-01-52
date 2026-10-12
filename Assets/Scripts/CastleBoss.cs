using UnityEngine;

// The castle's last-room boss keeps the authored three-cannon aiming rig.
public class CastleBoss : MonoBehaviour
{
    [Min(1)] public int hitPoints = 8;
    [Min(.1f)] public float vulnerableSeconds = 3.5f;
    [Min(0f)] public float verticalDistance = 1.6f;
    [Min(0f)] public float speed = .8f;
    public float ShieldRecoveryRemaining { get; private set; }
    FirstLevelRooms level;
    TutorialTarget target;
    CircleCollider2D body;
    int room, direction = 1;
    float startX, startY;
    bool recovering;
    readonly RaycastHit2D[] hits = new RaycastHit2D[32];

    public void Configure(FirstLevelRooms owner, int index)
    {
        level=owner;room=index;target=GetComponent<TutorialTarget>();
        body=GetComponent<CircleCollider2D>();
        startX=transform.position.x;startY=transform.position.y;
        target.SetCombatHealth(hitPoints,target.IsRedRobot);
    }

    public void OnShieldBroken()
    {
        recovering=true;ShieldRecoveryRemaining=vulnerableSeconds;
    }

    void Update()
    {
        if(level==null || !level.CanPlay || Time.timeScale<=0 || level.CurrentRoom!=room || target.IsDestroyed)return;
        if(recovering)
        {
            ShieldRecoveryRemaining=Mathf.Max(0,ShieldRecoveryRemaining-Time.deltaTime);
            if(ShieldRecoveryRemaining<=0){recovering=false;target.RestoreShield();}
        }
        if(body==null || speed<=0 || verticalDistance<=0)return;
        float end=startY+direction*verticalDistance;
        float travel=Mathf.Min(speed*Time.deltaTime,Mathf.Abs(end-transform.position.y));
        Vector2 centre=transform.TransformPoint(body.offset);
        float radius=body.radius*Mathf.Max(Mathf.Abs(transform.lossyScale.x),Mathf.Abs(transform.lossyScale.y));
        var filter=new ContactFilter2D();filter.SetLayerMask(Physics2D.AllLayers);filter.useTriggers=false;
        int count=Physics2D.CircleCast(centre,radius,Vector2.up*direction,filter,hits,travel+.025f);
        float free=travel;
        for(int i=0;i<count;i++)
        {
            if(hits[i].collider.transform.IsChildOf(transform))continue;
            free=Mathf.Min(free,Mathf.Max(0,hits[i].distance-.025f));
        }
        transform.position=new Vector3(startX,transform.position.y+direction*free,transform.position.z);
        if(free<travel || Mathf.Abs(transform.position.y-end)<.001f)direction=-direction;
    }
}
