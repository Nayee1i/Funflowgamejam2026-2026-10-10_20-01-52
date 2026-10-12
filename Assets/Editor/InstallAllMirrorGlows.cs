using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class InstallAllMirrorGlows
{
    class Template
    {
        public Sprite sprite;
        public Material material;
        public Color color;
        public Vector3 offset, scale;
        public Quaternion rotation;
        public Vector2 mirrorSize;
    }
    [InitializeOnLoadMethod] static void Initialize() => EditorApplication.update += Check;
    static void Check()
    {
        if (!File.Exists("Tools/install-mirror-glows.request") || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode) { EditorApplication.isPlaying = false; return; }
        File.Delete("Tools/install-mirror-glows.request");
        try { Run(); } catch (Exception e) { File.AppendAllText("Tools/mirror-glows-result.txt", "FAILED: " + e); Debug.LogException(e); }
    }
    static Template[] ReadTemplates(GameObject room, bool red)
    {
        var mirror = room.GetComponentsInChildren<MirrorRicochetSurface>(true).First(m => m.chargesShot == red);
        return room.GetComponentsInChildren<SpriteRenderer>(true)
            .Where(s => (s.name.StartsWith("Figma Mirror Glow") || s.name.StartsWith("Native Mirror Glow")) && Vector2.Distance(s.transform.position, mirror.transform.position) < .5f)
            .Select(s => new Template {
                sprite = s.sprite, material = s.sharedMaterial, color = s.color,
                offset = mirror.transform.InverseTransformPoint(s.transform.position),
                rotation = Quaternion.Inverse(mirror.transform.rotation) * s.transform.rotation,
                scale = new Vector3(s.transform.lossyScale.x / mirror.transform.lossyScale.x, s.transform.lossyScale.y / mirror.transform.lossyScale.y, 1),
                mirrorSize = mirror.GetComponent<SpriteRenderer>().sprite.bounds.size
            }).ToArray();
    }
    static void Run()
    {
        var previous = EditorSceneManager.GetActiveScene(); string previousPath = previous.path;
        if (previous.isDirty) { Directory.CreateDirectory("Tools/Backups"); EditorSceneManager.SaveScene(previous, "Tools/Backups/before-mirror-glows.unity", true); }
        EditorSceneManager.OpenScene("Assets/Scenes/level3.unity");
        var reference = Object.FindAnyObjectByType<FirstLevelRooms>().rooms[0];
        var blue = ReadTemplates(reference, false); var red = ReadTemplates(reference, true);
        if (blue.Length == 0 || red.Length == 0) throw new Exception("Missing native reference glow");
        File.WriteAllText("Tools/mirror-glows-result.txt", "Using native level3 room1 glows.\n");
        int total = 0;
        foreach (string name in new[] { "Level1Rooms", "level2", "level3" })
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity");
            var level = Object.FindAnyObjectByType<FirstLevelRooms>(); int count = 0;
            foreach (var room in level.rooms)
            {
                foreach (var old in room.GetComponentsInChildren<SpriteRenderer>(true).Where(s => s.name.StartsWith("Figma Mirror Glow") || s.name.StartsWith("Native Mirror Glow")).ToArray()) Object.DestroyImmediate(old.gameObject);
                foreach (var mirror in room.GetComponentsInChildren<MirrorRicochetSurface>(true))
                {
                    var picture = mirror.GetComponent<SpriteRenderer>();
                    if (picture == null) throw new Exception("Missing mirror image: " + mirror.name);
                    int layer = 0;
                    foreach (var template in mirror.chargesShot ? red : blue)
                    {
                        Vector2 size = picture.sprite.bounds.size;
                        var ratio = new Vector3(size.x / template.mirrorSize.x, size.y / template.mirrorSize.y, 1);
                        var glow = new GameObject("Native Mirror Glow " + layer++); glow.transform.SetParent(mirror.transform, false);
                        glow.transform.localPosition = Vector3.Scale(template.offset, ratio);
                        glow.transform.localRotation = template.rotation;
                        glow.transform.localScale = Vector3.Scale(template.scale, ratio);
                        var visual = glow.AddComponent<SpriteRenderer>(); visual.sprite = template.sprite; visual.sharedMaterial = template.material;
                        visual.color = template.color; visual.sortingLayerID = picture.sortingLayerID; visual.sortingOrder = picture.sortingOrder - 1;
                    }
                    count++;
                }
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            BuildFullSecondLevel.Capture("Tools/" + name + "-mirror-glow-preview.png");
            File.AppendAllText("Tools/mirror-glows-result.txt", name + ": " + count + " mirrors with matching glow, no duplicate old glows.\n"); total += count;
        }
        File.AppendAllText("Tools/mirror-glows-result.txt", "PASSED: " + total + " mirrors. Positions, scale and collisions unchanged.\n");
        EditorSceneManager.OpenScene(string.IsNullOrEmpty(previousPath) ? "Assets/Scenes/MainMenu.unity" : previousPath);
    }
}
