using UnityEngine;

// Door collision and the source artwork shutter disappear together after the room is cleared.
public class WorkshopDoor : MonoBehaviour
{
    public FirstLevelRooms level;
    public GameObject room;
    public bool entrance;
    public GameObject shutter;
    public Collider2D barrier;
    public bool IsOpen => entrance ? System.Array.IndexOf(level.rooms,room)>0 : FirstLevelEnemies.Cleared(room);
    public void Refresh()
    {
        bool open=IsOpen;
        if(barrier!=null)barrier.enabled=!open;
        if(shutter!=null)shutter.SetActive(!open);
    }
    void Update()=>Refresh();
}
