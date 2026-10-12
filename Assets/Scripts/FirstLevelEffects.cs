using UnityEngine;

public static class FirstLevelEffects
{
    static Sprite[] hero,enemy,explosion,red,confetti;
    static Sprite[] Load(string prefix,int count)
    {
        var frames=new Sprite[count];
        for(int i=0;i<count;i++)frames[i]=Resources.Load<Sprite>("FirstLevelFX/"+prefix+"_"+(i+1).ToString("00"));
        return frames;
    }
    public static Sprite[] Projectile(bool hostile) => hostile ? enemy ?? (enemy=Load("Enemy",8)) : hero ?? (hero=Load("Hero",8));
    public static Sprite[] RedFrames => red ?? (red=Load("Red",10));
    public static void ProjectileVisual(GameObject shot,bool hostile,FirstLevelRooms level,bool charged=false)
    {
        var frames=Projectile(hostile || charged);
        if(frames[0]==null)return;
        var renderer=shot.GetComponent<SpriteRenderer>();renderer.color=Color.white;
        shot.transform.localScale=Vector3.one*(.5f/frames[0].bounds.size.x);
        var animation=shot.GetComponent<FirstLevelSpriteAnimation>();
        if(animation==null)animation=shot.AddComponent<FirstLevelSpriteAnimation>();
        animation.Initialize(frames,16,true,level);
    }
    public static void Impact(Vector2 point,float size,FirstLevelRooms level,bool redEffect=false)
    {
        if(!Application.isPlaying)return;
        explosion=explosion ?? Load("Explosion",10);
        Play(redEffect ? "Figma Red Impact" : "Figma Impact",point,redEffect ? RedFrames : explosion,20,size,level,redEffect ? 31 : 30);
    }
    public static bool HasPendingImpacts(FirstLevelRooms owner)
    {
        foreach(var effect in Object.FindObjectsByType<FirstLevelSpriteAnimation>())
            if(effect.IsPendingImpact(owner))return true;
        return false;
    }
    public static void Celebrate(Vector2 point,FirstLevelRooms level)
    {
        if(confetti==null)
        {
            var atlas=Resources.Load<Texture2D>("FirstLevelFX/Confetti_01");
            if(atlas==null)return;
            confetti=new Sprite[64];float tile=atlas.width/8f;
            for(int i=0;i<64;i++)confetti[i]=Sprite.Create(atlas,new Rect(i%8*tile,(7-i/8)*tile,tile,tile),new Vector2(.5f,.5f),100);
        }
        Play("Figma Victory Confetti",point,confetti,24,7,null);
    }
    static void Play(string name,Vector2 point,Sprite[] frames,float fps,float size,FirstLevelRooms owner,int sortingOrder=30)
    {
        if(frames.Length==0 || frames[0]==null)return;
        var obj=new GameObject(name);obj.transform.position=point;
        var renderer=obj.AddComponent<SpriteRenderer>();renderer.sortingOrder=sortingOrder;
        obj.transform.localScale=Vector3.one*(size/frames[0].bounds.size.x);
        obj.AddComponent<FirstLevelSpriteAnimation>().Initialize(frames,fps,false,owner);
    }
    public static void Clear()
    {
        foreach(var animation in Object.FindObjectsByType<FirstLevelSpriteAnimation>(FindObjectsSortMode.None))
            if(Application.isPlaying)Object.Destroy(animation.gameObject);else Object.DestroyImmediate(animation.gameObject);
    }
}
