from pathlib import Path
p=Path('Assets/Scripts/FirstLevelShot.cs'); s=p.read_text(encoding='utf-8-sig')
a=s.index('            foreach(var hit in Physics2D.CircleCastAll')
b=s.index('            transform.position=origin+Direction*nearest;',a)
block=s[a:b].replace('hitPlayer=false;','').replace('FindObjectsByType<MirrorRicochetSurface>(FindObjectsSortMode.None)','Object.FindObjectsByType<MirrorRicochetSurface>()').replace('Direction','direction')
header='''using UnityEngine;

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
'''
tail='''        return new Contact{distance=nearest,solid=solid,mirror=mirror,mirrorCollider=mirrorCollider,normal=contactNormal};
    }
}
'''
Path('Assets/Scripts/FirstLevelShotPath.cs').write_text(header+block+tail,encoding='utf-8')
s=s[:a]+'''            var contact=FirstLevelShotPath.Cast(origin,Direction,radius,nearest,lastMirrorCollider,hostile,shooter);
            if(contact.Hit)
            {
                nearest=contact.distance;solid=contact.solid;mirror=contact.mirror;
                contactNormal=contact.normal;mirrorCollider=contact.mirrorCollider;hitPlayer=false;
            }
'''+s[b:]
s=s.replace('const int MaxReflections=5;','const int MaxReflections=FirstLevelShotPath.MaxReflections;').replace('const float Separation=.002f;','const float Separation=FirstLevelShotPath.Separation;')
p.write_text(s,encoding='utf-8')
print('Shared projectile collision query extracted.')
