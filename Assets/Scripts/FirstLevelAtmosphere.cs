using System.Collections.Generic;
using UnityEngine;

// Decorative floor effects: no colliders and no changes to movement or aiming.
public class FirstLevelAtmosphere : MonoBehaviour
{
    public float trackLifetime=4.5f;
    public float trackSpacing=.25f;
    public float trackOpacity=.45f;
    const int Capacity=96;
    class Mark { public SpriteRenderer visual; public float age; }
    readonly List<Mark> marks=new List<Mark>();
    readonly List<SpriteRenderer> lamps=new List<SpriteRenderer>();
    FirstLevelRooms level;
    Sprite tile,glow;
    Texture2D glowTexture;
    Vector2 previous;
    int room=-1,next;
    float remainder,clock;
    public int VisibleMarks => marks.FindAll(m=>m.visual!=null && m.visual.gameObject.activeSelf).Count;
    public void Initialize(FirstLevelRooms owner)
    {
        if(level!=null)return;
        level=owner;previous=owner.player.position;
        var atlas=Resources.Load<Texture2D>("FirstLevelFX/TankTrackAtlas");
        // A repeating section of Figma's original 'след от танка', without its fading tail.
        tile=Sprite.Create(atlas,new Rect(1450,41,70,164),new Vector2(.5f,.5f),100);
        glowTexture=new Texture2D(96,96,TextureFormat.RGBA32,false);
        glowTexture.wrapMode=TextureWrapMode.Clamp;
        var pixels=new Color[96*96];
        for(int y=0;y<96;y++)for(int x=0;x<96;x++)
        {
            Vector2 p=new Vector2((x+.5f)/48-1,(y+.5f)/48-1);
            float a=Mathf.Exp(-p.sqrMagnitude*5)*Mathf.Clamp01((1-p.magnitude)*8);
            pixels[y*96+x]=new Color(1,1,1,a);
        }
        glowTexture.SetPixels(pixels);glowTexture.Apply(false,true);
        glow=Sprite.Create(glowTexture,new Rect(0,0,96,96),new Vector2(.5f,.5f),100);
        for(int i=0;i<owner.rooms.Length;i++)
        {
            var floor=owner.rooms[i].transform.Find("Empty Room Background")?.GetComponent<SpriteRenderer>();
            if(floor!=null)AddLamp(floor,new Vector2(1237f/1536,145f/1024));
            floor=owner.rooms[i].transform.Find("Arena Background")?.GetComponent<SpriteRenderer>();
            if(floor!=null)
            {
                AddLamp(floor,new Vector2(.046f,.325f));AddLamp(floor,new Vector2(.048f,.64f));
                AddLamp(floor,new Vector2(.956f,.327f));AddLamp(floor,new Vector2(.96f,.646f));
            }
            floor=owner.rooms[i].transform.Find("Workshop Background")?.GetComponent<SpriteRenderer>();
            if(floor==null)continue;
            AddLamp(floor,new Vector2(335f/1586,68f/992));
            AddLamp(floor,new Vector2(1233f/1586,63f/992));
            AddLamp(floor,new Vector2(338f/1586,891f/992));
            AddLamp(floor,new Vector2(1225f/1586,894f/992));
        }
        UpdateLamps();
    }
    void AddLamp(SpriteRenderer floor,Vector2 sourcePoint)
    {
        var lamp=new GameObject("Lamp Glow").AddComponent<SpriteRenderer>();
        lamp.transform.SetParent(floor.transform.parent,false);lamp.sprite=glow;lamp.sortingOrder=2;
        var b=floor.sprite.bounds;
        lamp.transform.position=floor.transform.TransformPoint(b.center+new Vector3((sourcePoint.x-.5f)*b.size.x,(.5f-sourcePoint.y)*b.size.y,0));
        lamp.transform.localScale=Vector3.one*2.8f;lamps.Add(lamp);
    }
    void LateUpdate(){Tick(Time.deltaTime);}
    public void Tick(float dt)
    {
        if(level==null || (level.pausePanel!=null && level.pausePanel.activeSelf))return;
        clock+=dt;UpdateLamps();
        for(int i=0;i<marks.Count;i++)
        {
            var m=marks[i];if(!m.visual.gameObject.activeSelf)continue;
            m.age+=dt;
            float alpha=trackOpacity*(1-Mathf.SmoothStep(0,1,Mathf.Clamp01((m.age-.8f)/Mathf.Max(.1f,trackLifetime-.8f))));
            m.visual.color=new Color(1,1,1,alpha);
            if(m.age>=trackLifetime)m.visual.gameObject.SetActive(false);
        }
        Vector2 current=level.player.position;
        if(room!=level.CurrentRoom)
        {
            room=level.CurrentRoom;ClearMarks();previous=current;return;
        }
        Vector2 delta=current-previous;float distance=delta.magnitude;
        if(!level.CanPlay || distance>1.5f){previous=current;remainder=0;return;}
        if(distance<.0001f)return;
        Vector2 direction=delta/distance;
        float travelled=trackSpacing-remainder;
        for(;travelled<=distance;travelled+=trackSpacing)
            Stamp(previous+direction*travelled,direction);
        remainder=(remainder+distance)%trackSpacing;previous=current;
    }
    void Stamp(Vector2 position,Vector2 direction)
    {
        Mark mark;
        if(marks.Count<Capacity)
        {
            mark=new Mark{visual=new GameObject("Tank Track").AddComponent<SpriteRenderer>()};
            mark.visual.sprite=tile;mark.visual.sortingOrder=1;marks.Add(mark);
        }
        else{mark=marks[next];next=(next+1)%Capacity;}
        mark.age=0;mark.visual.transform.SetParent(level.rooms[room].transform,false);
        mark.visual.transform.position=position;
        mark.visual.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
        mark.visual.transform.localScale=new Vector3(.36f/.7f,level.playerRadius*1.55f/1.64f,1);
        mark.visual.color=new Color(1,1,1,trackOpacity);mark.visual.gameObject.SetActive(true);
    }
    void UpdateLamps()
    {
        for(int i=0;i<lamps.Count;i++)
        {
            float phase=clock+i*.9f;
            float strength=.26f*(1+.14f*Mathf.Sin(phase*2.2f)+.04f*Mathf.Sin(phase*7.7f));
            lamps[i].color=new Color(.9804f,.7255f,.0118f,strength);
        }
    }
    void ClearMarks(){foreach(var mark in marks)mark.visual.gameObject.SetActive(false);remainder=0;}
    void OnDestroy()
    {
        foreach(var mark in marks)if(mark.visual!=null)Destroy(mark.visual.gameObject);
        foreach(var lamp in lamps)if(lamp!=null)Destroy(lamp.gameObject);
        if(tile!=null)Destroy(tile);if(glow!=null)Destroy(glow);if(glowTexture!=null)Destroy(glowTexture);
    }
}
