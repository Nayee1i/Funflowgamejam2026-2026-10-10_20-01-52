using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class VerifyFirstLevelBoundaries
{
    [MenuItem("Bounce Club/Verify Room Boundaries — Five Passes")]
    public static void Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before verifying room boundaries.");
        var scene = EditorSceneManager.OpenScene(BuildFirstLevelRooms.ScenePath);
        var level = UnityEngine.Object.FindAnyObjectByType<FirstLevelRooms>();
        var move = typeof(FirstLevelRooms).GetMethod("MoveAxis", BindingFlags.Instance | BindingFlags.NonPublic);
        var recover = typeof(FirstLevelRooms).GetMethod("ResolveBoundaryOverlap", BindingFlags.Instance | BindingFlags.NonPublic);
        int checks = 0;
        Vector2[] directions = {Vector2.up, Vector2.down, Vector2.left, Vector2.right,
            new Vector2(-1,1).normalized, new Vector2(1,1).normalized,
            new Vector2(-1,-1).normalized, new Vector2(1,-1).normalized};
        try
        {
            for (int pass=0; pass<5; pass++)
            for (int room=0; room<3; room++)
            {
                for(int i=0; i<3; i++) level.rooms[i].SetActive(i==room);
                Physics2D.SyncTransforms();
                foreach(var direction in directions)
                {
                    level.player.position = new Vector3(-3,-.55f,0);
                    for(int step=0; step<240; step++)
                    {
                        Move(move, level, direction * (.045f + pass*.006f));
                        AssertClear(level);
                    }
                    Vector3 contact = level.player.position;
                    for(int step=0; step<15; step++) Move(move,level,-direction*.045f);
                    if(Vector3.Distance(contact,level.player.position)<.4f) throw new Exception("Cannot retreat from boundary, room " + room + ", direction " + direction);
                    AssertClear(level);
                    checks++;
                }
                foreach(var corner in new[] {new Vector2(-4.1f,1.1f),new Vector2(4.6f,1.5f),new Vector2(-4.6f,-2.7f),new Vector2(4.7f,-2.6f)})
                {
                    level.player.position=corner;
                    recover.Invoke(level,null);
                    AssertClear(level);
                    checks++;
                }
                foreach(float side in new[] {-1f,1f})
                foreach(float vertical in new[] {-1f,1f})
                {
                    level.player.position=new Vector3(side*4.25f,-.55f,0);
                    for(int step=0;step<100;step++) { Move(move,level,new Vector2(0,vertical*.045f)); AssertClear(level); }
                    Vector3 cornerContact=level.player.position;
                    for(int step=0;step<15;step++) Move(move,level,new Vector2(0,-vertical*.045f));
                    if(Vector3.Distance(cornerContact,level.player.position)<.4f) throw new Exception("Cannot reverse away from corner in room " + room);
                    for(int step=0;step<20;step++) { Move(move,level,new Vector2(-side*.045f,0)); AssertClear(level); }
                    checks++;
                }
                // Doorway must stay traversable in both directions.
                foreach(float side in new[] {-1f,1f})
                {
                    level.player.position=new Vector3(side*4.45f,-.55f,0);
                    for(int step=0;step<45;step++) { Move(move,level,new Vector2(side*.045f,0)); AssertClear(level); }
                    if(Mathf.Abs(level.player.position.x)<6.1f) throw new Exception("Doorway blocked in room " + room);
                    checks++;
                }
                if(room==1)
                {
                    foreach(float routeY in new[]{.85f,-2.32f})
                    {
                    level.player.position=new Vector3(-4.45f,-.55f,0);
                    foreach(var waypoint in new[]{new Vector2(-2,-.55f),new Vector2(-2,routeY),new Vector2(2,routeY),new Vector2(2,-.55f),new Vector2(6.2f,-.55f)})
                    {
                        for(int step=0;step<400 && Vector2.Distance(level.player.position,waypoint)>.025f;step++)
                        {
                            Vector2 delta=waypoint-(Vector2)level.player.position;
                            Move(move,level,Vector2.ClampMagnitude(delta,.035f)); AssertClear(level);
                        }
                        if(Vector2.Distance(level.player.position,waypoint)>.03f) throw new Exception("Room 1.2 route blocked toward " + waypoint + " at " + level.player.position);
                        checks++;
                    }
                    }
                }
            }
            Debug.Log("ROOM_BOUNDARIES_FIVE_PASSES_PASSED: " + checks + " checks across three rooms; walls, corners, retreat, overlap recovery and both doors.");
        }
        finally
        {
            for(int i=0;i<3;i++) level.rooms[i].SetActive(i==0);
            level.player.position=new Vector3(-4.45f,-.55f,0);
            EditorSceneManager.SaveScene(scene);
        }
    }
    private static void Move(MethodInfo method,FirstLevelRooms level,Vector2 delta)
    {
        method.Invoke(level,new object[] {new Vector2(delta.x,0)});
        method.Invoke(level,new object[] {new Vector2(0,delta.y)});
    }
    private static void AssertClear(FirstLevelRooms level)
    {
        foreach(var hit in Physics2D.OverlapCircleAll(level.player.position,level.playerRadius-.004f))
            if(!hit.isTrigger && Vector2.Distance(level.player.position, hit.ClosestPoint(level.player.position)) < level.playerRadius-.008f)
                throw new Exception("Tank overlaps " + hit.name + " at " + level.player.position + "; clearance " + Vector2.Distance(level.player.position, hit.ClosestPoint(level.player.position)));
    }
}
