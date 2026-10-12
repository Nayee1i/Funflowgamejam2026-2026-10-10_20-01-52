using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class InstallFirstLevelEnemies
{
    [MenuItem("Bounce Club/Install First Level Enemies")]
    public static void Install()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Stop Play Mode first.");
        AssetDatabase.Refresh();
        var scene=EditorSceneManager.OpenScene(BuildFirstLevelRooms.ScenePath);
        var level=Object.FindAnyObjectByType<FirstLevelRooms>();
        FirstLevelEnemies.Place(level,true);
        EditorSceneManager.SaveScene(scene);
        Verify();
        for(int i=0;i<3;i++)PreviewFirstLevel.RenderRoom(i);
        Debug.Log("FIRST_LEVEL_ENEMIES: three stationary Figma robots installed; combat checks passed.");
    }
    static void Require(bool condition,string message){if(!condition)throw new System.Exception(message);}
    public static void Build()
    {
        Install();
        var options=new BuildPlayerOptions {
            scenes=System.Array.ConvertAll(System.Array.FindAll(EditorBuildSettings.scenes,s=>s.enabled),s=>s.path),
            locationPathName="Builds/FunflowGameJam.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None
        };
        var report=BuildPipeline.BuildPlayer(options);
        if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new System.Exception("Enemy build failed: "+report.summary.result);
        Debug.Log("FIRST_LEVEL_ENEMIES_BUILD: Succeeded");
    }
    public static void Verify()
    {
        var scene=EditorSceneManager.OpenScene(BuildFirstLevelRooms.ScenePath);
        var level=Object.FindAnyObjectByType<FirstLevelRooms>();
        FirstLevelEnemies.Place(level,true);
        var ball=level.player.GetComponent<FirstLevelTankAim>().projectileSprite;
        for(int i=0;i<3;i++)
        {
            foreach(var room in level.rooms)room.SetActive(false);
            level.rooms[i].SetActive(true);
            var target=level.rooms[i].GetComponentInChildren<TutorialTarget>();
            Require(target!=null && target.GetComponent<TutorialRobot>()!=null,"Missing robot in room "+i);
            Require(!FirstLevelEnemies.Cleared(level.rooms[i]),"Room unlocks before target dies.");
            target.Hit(false,false);
            Require(target.Health==(i==2 ? 2 : 1),"Direct hit/shield rule differs from Vlad mechanics.");
            if(i==2){target.Hit(true,true);Require(!target.IsShielded && target.Health==2,"Charged hit must remove the shield first.");}
            target.Hit(true,false);
            Require(target.IsDestroyed && FirstLevelEnemies.Cleared(level.rooms[i]),"Reflected shot must clear the room.");
        }
        // Verify the actual player projectile bridges to Vlad's target, not just the Hit method.
        FirstLevelEnemies.Place(level,true);
        foreach(var room in level.rooms)room.SetActive(false);
        level.rooms[0].SetActive(true);
        var robot=level.rooms[0].GetComponentInChildren<TutorialTarget>();
        Vector2 origin=(Vector2)robot.transform.position+Vector2.left;
        var shot=FirstLevelShot.Create(origin,Vector2.right,ball,level);
        shot.Step(1);
        Require(shot==null || shot.Finished,"Projectile must end on enemy.");
        Require(robot.Health==1,"Player shot passes through robot.");
        FirstLevelShot.ClearAll();
        FirstLevelEnemies.Place(level,true);
        foreach(var room in level.rooms)room.SetActive(false);
        level.rooms[0].SetActive(true);
        var shooter=level.rooms[0].GetComponentInChildren<TutorialTarget>();
        level.player.position=shooter.transform.position+Vector3.left*2;
        int hp=level.player.GetComponent<TutorialPlayer>().HealthHalves;
        var hostile=FirstLevelShot.Create(shooter.transform.position,Vector2.left,ball,level);
        hostile.SetEnemyShooter(shooter);hostile.Step(2);
        Require(level.player.GetComponent<TutorialPlayer>().HealthHalves==hp-1,"Black enemy ball must take half a heart.");
        FirstLevelShot.ClearAll();
        var vision=typeof(TutorialRobot).GetMethod("CanSeePlayer",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        level.player.position=shooter.transform.position+Vector3.down*1.5f;
        Physics2D.SyncTransforms();
        Require((bool)vision.Invoke(shooter.GetComponent<TutorialRobot>(),null),"Robot cannot see player in open space.");
        var cover=new GameObject("Temporary cover");cover.transform.position=shooter.transform.position+Vector3.down*.75f;
        cover.AddComponent<BoxCollider2D>().size=new Vector2(1.2f,1);
        Physics2D.SyncTransforms();
        Require(!(bool)vision.Invoke(shooter.GetComponent<TutorialRobot>(),null),"Robot sees through cover.");
        Object.DestroyImmediate(cover);
        foreach(var room in level.rooms)room.SetActive(false);
        level.rooms[2].SetActive(true);
        var boss=level.rooms[2].GetComponentInChildren<TutorialTarget>();
        var route=level.rooms[2].GetComponent<FirstLevelRicochetRoute>();
        // Own ricochets now hurt: exercise the old route while sidestepping its
        // returning segment, just as the player must do during gameplay.
        Vector2 launchOrigin=route.shotOrigin;
        level.player.position=launchOrigin;Physics2D.SyncTransforms();
        var charged=FirstLevelShot.Create(launchOrigin,Vector2.down,ball,level);charged.Step(1);
        level.player.position=launchOrigin+Vector2.left;Physics2D.SyncTransforms();charged.Step(19);
        Require(!boss.IsShielded && boss.Health==2,"Red-blue route misses the shielded robot.");
        level.player.position=launchOrigin;Physics2D.SyncTransforms();
        charged=FirstLevelShot.Create(launchOrigin,Vector2.down,ball,level);charged.Step(1);
        level.player.position=launchOrigin+Vector2.left;Physics2D.SyncTransforms();charged.Step(19);
        Require(boss.IsDestroyed,"Second red-blue shot must defeat the robot.");
        FirstLevelShot.ClearAll();
        EditorSceneManager.OpenScene(BuildFirstLevelRooms.ScenePath);
        Debug.Log("FIRST_LEVEL_ENEMY_TESTS_PASSED");
    }
}

