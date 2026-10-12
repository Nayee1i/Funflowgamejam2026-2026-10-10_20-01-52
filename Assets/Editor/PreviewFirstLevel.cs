using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PreviewFirstLevel
{
    private static bool showBoundaries;
    private static bool showRicochet;
    private static int previewRoom = -1;
    public static void RenderRoom(int room) { previewRoom=room; try { Render(); } finally { previewRoom=-1; } }
    [MenuItem("Bounce Club/Preview Ricochet Route")]
    public static void RenderRicochet() { previewRoom=2; showRicochet=true; try {Render();} finally {previewRoom=-1;showRicochet=false;} }
    [MenuItem("Bounce Club/Preview Room Boundaries")]
    public static void RenderBoundaries() { showBoundaries = true; try { Render(); } finally { showBoundaries = false; } }
    [MenuItem("Bounce Club/Preview First Level")]
    public static void Render()
    {
        EditorSceneManager.OpenScene(BuildFirstLevelRooms.ScenePath);
        if(previewRoom>=0)
        {
            var level=Object.FindAnyObjectByType<FirstLevelRooms>();
            for(int i=0;i<level.rooms.Length;i++) level.rooms[i].SetActive(i==previewRoom);
        }
        if (showBoundaries)
        {
            foreach(var collider in Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None))
            {
                Vector2[] points = collider is EdgeCollider2D edge ? edge.points : collider is PolygonCollider2D polygon ? polygon.points : null;
                if (points == null) continue;
                var line = new GameObject("Boundary Preview").AddComponent<LineRenderer>();
                line.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
                line.startColor = line.endColor = Color.cyan;
                line.startWidth = line.endWidth = .018f;
                line.sortingOrder = 100;
                line.loop = collider is PolygonCollider2D;
                line.positionCount = points.Length;
                for(int i=0;i<points.Length;i++) line.SetPosition(i, collider.transform.TransformPoint(points[i]));
            }
        }
        if(showRicochet)
        {
            var route=Object.FindAnyObjectByType<FirstLevelRicochetRoute>();
            Vector2[] points={route.shotOrigin,route.redMirror.ReflectionPoint,route.blueMirror.ReflectionPoint,route.futureBossPosition};
            Color[] colors={Color.yellow,new Color(1,.3f,.2f),Color.cyan};
            for(int i=0;i<3;i++)
            {
                var line=new GameObject("Trajectory Preview").AddComponent<LineRenderer>();
                line.sharedMaterial=new Material(Shader.Find("Sprites/Default"));
                line.startColor=line.endColor=colors[i];line.startWidth=line.endWidth=.025f;
                line.sortingOrder=100;line.positionCount=2;
                line.SetPosition(0,points[i]);line.SetPosition(1,points[i+1]);
            }
        }
        var camera = Object.FindAnyObjectByType<Camera>();
        var canvas = Object.FindAnyObjectByType<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 1;
        var target = new RenderTexture(1920, 1080, 24);
        camera.targetTexture = target;
        camera.GetComponent<FirstLevelCameraFit>().Apply();
        Canvas.ForceUpdateCanvases();
        camera.Render();
        var previous = RenderTexture.active;
        RenderTexture.active = target;
        var image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
        image.Apply();
        string output=showRicochet ? "../Level1-ricochet.png" : previewRoom>=0 ? "../Level1-room"+(previewRoom+1)+".png" : showBoundaries ? "../Level1-boundaries.png" : "../Level1-preview.png";
        System.IO.File.WriteAllBytes(System.IO.Path.GetFullPath(output), image.EncodeToPNG());
        camera.targetTexture = null;
        RenderTexture.active = previous;
        Object.DestroyImmediate(target);
        Object.DestroyImmediate(image);
        EditorSceneManager.OpenScene(BuildFirstLevelRooms.ScenePath);
    }
}
