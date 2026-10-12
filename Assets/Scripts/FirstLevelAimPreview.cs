using System.Collections.Generic;
using UnityEngine;

public class FirstLevelAimPreview : MonoBehaviour
{
    public struct Segment { public Vector2 start,end; }
    readonly List<SpriteRenderer> dashes=new List<SpriteRenderer>();
    readonly List<Segment> path=new List<Segment>();
    static Sprite dashSprite;
    static Material dashMaterial;
    const float DashLength=.2f, DashWidth=.03f, Spacing=.23f;
    public static void Trace(Vector2 origin,Vector2 direction,float radius,float length,List<Segment> output)
    {
        output.Clear();Physics2D.SyncTransforms();Collider2D last=null;
        for(int reflection=0;reflection<=FirstLevelShotPath.MaxReflections && length>0;reflection++)
        {
            var hit=FirstLevelShotPath.Cast(origin,direction,radius,length,last);
            Vector2 end=origin+direction*hit.distance;
            output.Add(new Segment{start=origin,end=end});length-=hit.distance;
            if(hit.mirror==null || reflection==FirstLevelShotPath.MaxReflections)break;
            direction=hit.Reflect(direction);last=hit.mirrorCollider;
            origin=end+direction*FirstLevelShotPath.Separation;length-=FirstLevelShotPath.Separation;
        }
    }
    public void Show(FirstLevelTankAim gun)
    {
        Trace(gun.turret.position,gun.AimDirection,gun.projectileRadius,gun.projectileSpeed*8f+gun.muzzleDistance*Mathf.Abs(gun.transform.lossyScale.x),path);
        int used=0;float traveled=0;
        float muzzle=gun.muzzleDistance*Mathf.Abs(gun.transform.lossyScale.x);
        foreach(var segment in path)
        {
            Vector2 direction=(segment.end-segment.start).normalized;
            float length=Vector2.Distance(segment.start,segment.end);
            float start=Mathf.Max(0,muzzle-traveled);
            for(float position=start;position<length && used<512;position+=Spacing)
            {
                float visible=Mathf.Min(DashLength,length-position);
                if(visible<.005f)continue;
                var dash=GetDash(used++);dash.gameObject.SetActive(true);
                dash.transform.position=segment.start+direction*(position+visible*.5f);
                dash.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
                dash.transform.localScale=new Vector3(visible/dashSprite.bounds.size.x,DashWidth/dashSprite.bounds.size.y,1);
            }
            traveled+=length;
        }
        for(int i=used;i<dashes.Count;i++)dashes[i].gameObject.SetActive(false);
    }
    SpriteRenderer GetDash(int index)
    {
        if(dashSprite==null)
        {
            // Figma 119:80: 20x3px, radius 1px, grey fill and brown border at 50% opacity.
            var texture=new Texture2D(80,12,TextureFormat.RGBA32,false);texture.filterMode=FilterMode.Bilinear;
            for(int y=0;y<12;y++)for(int x=0;x<80;x++)
            {
                bool border=x<4 || x>=76 || y<4 || y>=8;
                float cx=Mathf.Clamp(x,3.5f,75.5f),cy=Mathf.Clamp(y,3.5f,7.5f);
                float alpha=Vector2.Distance(new Vector2(x,y),new Vector2(cx,cy))>4 ? 0 : .5f;
                texture.SetPixel(x,y,border ? new Color(.3875f,.3196875f,.3196875f,alpha) : new Color(.85098f,.85098f,.85098f,alpha));
            }
            texture.Apply();dashSprite=Sprite.Create(texture,new Rect(0,0,80,12),Vector2.one*.5f,400);
            dashMaterial=new Material(Shader.Find("Sprites/Default"));
        }
        if(index>=dashes.Count)
        {
            var dash=new GameObject("Aim Dash").AddComponent<SpriteRenderer>();
            dash.transform.SetParent(transform,false);dash.sprite=dashSprite;dash.sharedMaterial=dashMaterial;dash.sortingOrder=18;dashes.Add(dash);
        }
        return dashes[index];
    }
    public void Hide(){foreach(var dash in dashes)dash.gameObject.SetActive(false);}
}
