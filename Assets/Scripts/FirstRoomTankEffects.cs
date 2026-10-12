using System.Collections.Generic;
using UnityEngine;

// Ambient sunlit dust and tank shadows, restricted to the first tutorial room.
public class FirstRoomTankEffects : MonoBehaviour
{
    const int Capacity=40;
    class Mote { public SpriteRenderer sprite; public Vector2 origin; public float phase,period,size; }
    readonly List<Mote> dust=new List<Mote>();
    FirstLevelRooms level;
    SpriteRenderer shadow,silhouette,body;
    Texture2D texture;
    Sprite soft;
    Material material;
    float clock;
    public int VisibleDust => dust.FindAll(p=>p.sprite.gameObject.activeSelf).Count;
    public SpriteRenderer Shadow => shadow;
    public bool Initialize(FirstLevelRooms owner)
    {
        if(level!=null)return true;
        if(owner==null || owner.emptySandbox || owner.GetComponent<SecondLevelWorkshop>()!=null)return false;
        level=owner;
        texture=new Texture2D(64,64,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp};
        var pixels=new Color[64*64];
        for(int y=0;y<64;y++)for(int x=0;x<64;x++)
        {
            Vector2 p=new Vector2((x+.5f)/32-1,(y+.5f)/32-1);
            pixels[y*64+x]=new Color(1,1,1,Mathf.Exp(-p.sqrMagnitude*2.7f)*Mathf.Clamp01((1-p.magnitude)*8));
        }
        texture.SetPixels(pixels);texture.Apply(false,true);
        soft=Sprite.Create(texture,new Rect(0,0,64,64),Vector2.one*.5f,64);
        material=new Material(Shader.Find("Sprites/Default"));
        shadow=CreateVisual("Tank Soft Shadow — Room 1.1",3);
        shadow.color=new Color(.08f,.055f,.025f,.45f);
        shadow.transform.localScale=new Vector3(owner.playerRadius*2.9f,owner.playerRadius*2.4f,1);
        body=owner.player.GetComponent<SpriteRenderer>();
        if(body!=null && body.sprite!=null)
        {
            silhouette=CreateVisual("Tank Contact Shadow — Room 1.1",3);
            silhouette.sprite=body.sprite;silhouette.color=new Color(.05f,.035f,.02f,.32f);
        }
        // Deterministic room positions; no tank movement and no shared gameplay RNG.
        Vector2 beamStart=new Vector2(4.25f,2.7f),beamEnd=new Vector2(-.8f,-1.9f);
        Vector2 across=new Vector2(.67f,-.74f);
        for(int i=0;i<Capacity;i++)
        {
            float t=Mathf.Repeat((i+.5f)*.618034f,1),spread=Mathf.Repeat(i*.754877f+.21f,1)-.5f;
            var mote=new Mote{sprite=CreateVisual("Sunlit Room Dust — Room 1.1",12),origin=Vector2.Lerp(beamStart,beamEnd,t)+across*spread*1.8f,phase=i*.83f,period=7+Mathf.Repeat(i*.37f,1)*5,size=.035f+Mathf.Repeat(i*.41f,1)*.04f};
            mote.sprite.transform.localScale=Vector3.one*mote.size;dust.Add(mote);
        }
        Tick(0);return true;
    }
    SpriteRenderer CreateVisual(string name,int order)
    {
        var s=new GameObject(name).AddComponent<SpriteRenderer>();s.transform.SetParent(level.rooms[0].transform,false);
        s.sprite=soft;s.sharedMaterial=material;s.sortingOrder=order;return s;
    }
    void LateUpdate(){Tick(Time.deltaTime);}
    public void Tick(float dt)
    {
        if(level==null)return;
        bool active=level.CurrentRoom==0;
        shadow.gameObject.SetActive(active && !level.Defeated);
        if(silhouette!=null)silhouette.gameObject.SetActive(active && !level.Defeated);
        foreach(var p in dust)p.sprite.gameObject.SetActive(active);
        if(!active)return;
        Vector2 offset=new Vector2(-.18f,-.22f);
        shadow.transform.position=(Vector3)((Vector2)level.player.position+offset);
        if(silhouette!=null)
        {
            silhouette.transform.SetPositionAndRotation(body.transform.position+(Vector3)offset,body.transform.rotation);
            silhouette.transform.localScale=body.transform.lossyScale;
        }
        if(level.pausePanel!=null && level.pausePanel.activeSelf)return;
        clock+=Mathf.Max(0,dt);
        foreach(var p in dust)
        {
            float angle=clock*.18f+p.phase;
            p.sprite.transform.position=p.origin+new Vector2(Mathf.Sin(angle)*.13f,Mathf.Sin(angle*.73f+p.phase)*.20f);
            float pulse=.5f+.5f*Mathf.Sin(clock*2*Mathf.PI/p.period+p.phase);
            p.sprite.color=new Color(1,.91f,.66f,.15f+.42f*pulse*pulse);
        }
    }
    static void Release(Object o){if(o==null)return;if(Application.isPlaying)Destroy(o);else DestroyImmediate(o);}
    void OnDestroy(){if(shadow!=null)Release(shadow.gameObject);if(silhouette!=null)Release(silhouette.gameObject);foreach(var p in dust)if(p.sprite!=null)Release(p.sprite.gameObject);Release(soft);Release(texture);Release(material);}
}
