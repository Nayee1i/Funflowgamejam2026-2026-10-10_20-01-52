using UnityEngine;

// Layout is authored in the scene from Figma 2.1–2.4. Combat remains Vlad's implementation.
public class SecondLevelWorkshop : MonoBehaviour
{
    public void ConfigureCombat(FirstLevelRooms level)
    {
        var aim=level.player.GetComponent<FirstLevelTankAim>();
        var player=level.player.GetComponent<TutorialPlayer>();
        if(player==null)player=level.player.gameObject.AddComponent<TutorialPlayer>();
        var hud=level.GetComponent<FirstLevelHealthHud>();
        if(hud==null)hud=level.gameObject.AddComponent<FirstLevelHealthHud>();
        hud.player=player;
        for(int i=0;i<level.rooms.Length;i++)
        foreach(var robot in level.rooms[i].GetComponentsInChildren<TutorialRobot>(true))
        {
            var rig=robot.transform.Find("Turret");
            robot.ConfigureFirstLevel(level,rig,rig.Find("Muzzle"),aim.projectileSprite,i);
        }
    }
}
