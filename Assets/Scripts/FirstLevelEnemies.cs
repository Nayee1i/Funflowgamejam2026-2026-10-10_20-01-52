using UnityEngine;

// The artwork and placements come from Figma groups 56:3039, 56:3061 and 56:3082.
public static class FirstLevelEnemies
{
    public static void Place(FirstLevelRooms level, bool replace = false)
    {
        Vector2[] positions={new Vector2(3.11f,.641f),new Vector2(3.598f,-1.088f),new Vector2(3.848f,-1.948f)};
        var aim=level.player.GetComponent<FirstLevelTankAim>();
        if(level.player.GetComponent<TutorialPlayer>()==null)level.player.gameObject.AddComponent<TutorialPlayer>();
        var hud=level.GetComponent<FirstLevelHealthHud>();
        if(hud==null)hud=level.gameObject.AddComponent<FirstLevelHealthHud>();
        hud.player=level.player.GetComponent<TutorialPlayer>();
        if(level.GetComponent<FirstLevelPresentation>()==null)level.gameObject.AddComponent<FirstLevelPresentation>();
        Sprite body=Resources.Load<Sprite>("FirstLevelEnemies/Body");
        Sprite head=Resources.Load<Sprite>("FirstLevelEnemies/Turret");
        for(int i=0;i<level.rooms.Length;i++)
        {
            var old=level.rooms[i].transform.Find("Figma Robot");
            if(old!=null && !replace)
            {
                var rig=old.Find("Turret");
                old.GetComponent<TutorialRobot>().ConfigureFirstLevel(level,rig,rig.Find("Muzzle"),aim.projectileSprite,i);
                var existingShield=old.Find("Shield");
                if(existingShield!=null)ConfigureShield(existingShield.GetComponent<SpriteRenderer>());
                continue;
            }
            if(old!=null){old.gameObject.SetActive(false);if(Application.isPlaying)Object.Destroy(old.gameObject);else Object.DestroyImmediate(old.gameObject);}
            var robot=new GameObject("Figma Robot");
            robot.transform.SetParent(level.rooms[i].transform,false);
            robot.transform.localPosition=positions[i];
            var renderer=robot.AddComponent<SpriteRenderer>();renderer.sprite=body;renderer.sortingOrder=10;
            // Native source pixels remain intact. World size matches the player's scale.
            robot.transform.localScale=Vector3.one*(i==0 ? .7f : .76f);
            robot.AddComponent<CircleCollider2D>().radius=.68f;
            var turret=new GameObject("Turret").transform;turret.SetParent(robot.transform,false);
            var picture=new GameObject("Figma Turret").AddComponent<SpriteRenderer>();
            picture.transform.SetParent(turret,false);picture.sprite=head;picture.sortingOrder=11;
            picture.transform.localRotation=Quaternion.Euler(0,0,180);
            var muzzle=new GameObject("Muzzle").transform;muzzle.SetParent(turret,false);muzzle.localPosition=new Vector3(0,.76f,0);
            turret.rotation=Quaternion.Euler(0,0,90);
            var shield=new GameObject("Shield").AddComponent<SpriteRenderer>();
            shield.transform.SetParent(robot.transform,false);shield.sprite=Resources.Load<Sprite>("FirstLevelEnemies/Shield");shield.sortingOrder=12;
            ConfigureShield(shield);
            var target=robot.AddComponent<TutorialTarget>();target.Configure(i==2,shield.gameObject);
            robot.AddComponent<TutorialRobot>().ConfigureFirstLevel(level,turret,muzzle,aim.projectileSprite,i);
        }
    }
    public static void ConfigureShield(SpriteRenderer shield)
    {
        shield.sprite=Resources.Load<Sprite>("FirstLevelEnemies/Shield");
        shield.color=new Color(1,1,1,.65f);
        shield.transform.localScale=Vector3.one;
        var material=Resources.Load<Material>("FirstLevelEnemies/ShieldLight");
        if(material!=null)shield.sharedMaterial=material;
    }
    public static bool Cleared(GameObject room)
    {
        foreach(var enemy in room.GetComponentsInChildren<TutorialTarget>(true))
            if(!enemy.IsDestroyed)return false;
        return true;
    }
}
