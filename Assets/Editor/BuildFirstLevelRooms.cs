using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class BuildFirstLevelRooms
{
    public const string ScenePath = "Assets/Scenes/Level1Rooms.unity";
    private const string Art = "Assets/UI/Level1/";
    [MenuItem("Bounce Club/Build First Level Rooms")]
    public static void Build()
    {
        foreach (string path in System.IO.Directory.GetFiles(Art, "*.png"))
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            Level1TextureQuality.Configure(importer,path);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
        camera.tag = "MainCamera";
        camera.transform.position = new Vector3(0, .4315f, -10);
        camera.orthographic = true;
        camera.orthographicSize = 5.15f;
        camera.backgroundColor = new Color(25/255f, 24/255f, 23/255f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.gameObject.AddComponent<FirstLevelCameraFit>();
        var controller = new GameObject("First Level — Rooms 1.1 / 1.2 / 1.3").AddComponent<FirstLevelRooms>();
        controller.rooms = new GameObject[3];
        string[] backgrounds = { "56_3027", "56_3049", "56_3070" };
        for (int i = 0; i < 3; i++)
        {
            var room = new GameObject("Room 1." + (i + 1));
            controller.rooms[i] = room;
            var floor = new GameObject("Figma — Empty Room Background").AddComponent<SpriteRenderer>();
            floor.transform.SetParent(room.transform);
            floor.transform.position = new Vector3(0, -.76f, 0);
            floor.sprite = Sprite(backgrounds[i]);
            floor.sortingOrder = 0;
            AddBoundaries(room.transform);
            FigmaLevelObstacles.Add(room.transform, i);
            room.SetActive(i == 0);
        }
        FigmaLevelObstacles.ApplyBackgroundBlend(controller);
        var tank = new GameObject("Player Tank").AddComponent<SpriteRenderer>();
        tank.sprite = Sprite("56_3036");
        tank.sortingOrder = 10;
        tank.transform.localScale = Vector3.one * .7f;
        tank.transform.position = new Vector3(-4.45f, -.55f, 0);
        controller.player = tank.transform;
        var canvas = new GameObject("Level HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        var root = new GameObject("Figma 1920 × 1080", typeof(RectTransform), typeof(AspectRatioFitter)).GetComponent<RectTransform>();
        root.SetParent(canvas.transform, false);
        root.GetComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        root.GetComponent<AspectRatioFitter>().aspectRatio = 16f / 9f;
        Picture("Health Frame", root, "56_3028", 32, 20, 584, 151);
        Picture("Timer — Figma Placeholder", root, "56_3029", 823, 43.5f, 274, 104);
        var menu = Picture("Menu", root, "56_3035", 1783, 47.5f, 104, 96);
        var menuButton = menu.gameObject.AddComponent<FigmaMenuButton>();
        menu.raycastTarget = true;
        menuButton.Configure(menu.rectTransform, null);
        menuButton.targetGraphic = menu;
        UnityEventTools.AddPersistentListener(menuButton.onClick, controller.TogglePause);
        var temporaryWindows = canvas.AddComponent<MainMenuWindows>();
        controller.pausePanel = BuildFigmaMainMenu.Modal("Exit", root, temporaryWindows);
        // The level uses the existing Figma exit panel and replaces its callbacks.
        foreach (var button in controller.pausePanel.GetComponentsInChildren<Button>(true))
        {
            for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--) UnityEventTools.RemovePersistentListener(button.onClick, i);
            if (button.name.Contains("Hit"))
            {
                string graphicName = button.transform.parent.GetChild(button.transform.GetSiblingIndex() - 1).name;
                if (graphicName.EndsWith("48:2675")) UnityEventTools.AddPersistentListener(button.onClick, controller.OpenMainMenu);
                else UnityEventTools.AddPersistentListener(button.onClick, controller.TogglePause);
            }
        }
        Object.DestroyImmediate(temporaryWindows);
        controller.pausePanel.SetActive(false);
        controller.blackout = new GameObject("Black Transition", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        controller.blackout.transform.SetParent(canvas.transform, false);
        var rect = controller.blackout.rectTransform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        controller.blackout.color = Color.clear;
        controller.blackout.raycastTarget = false;
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        EditorSceneManager.SaveScene(scene, ScenePath);
        var scenes = EditorBuildSettings.scenes.ToList();
        if (!scenes.Any(s => s.path == ScenePath)) scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        EditorSceneManager.OpenScene(BuildFigmaMainMenu.ScenePath);
        var windows = Object.FindAnyObjectByType<MainMenuWindows>();
        foreach (var button in windows.modesWindow.GetComponentsInChildren<Button>(true))
            if (button.transform.GetSiblingIndex() > 0 && button.transform.parent.GetChild(button.transform.GetSiblingIndex() - 1).name.EndsWith("48:2691"))
            {
                for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--) UnityEventTools.RemovePersistentListener(button.onClick, i);
                UnityEventTools.AddPersistentListener(button.onClick, windows.StartLevel1);
            }
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.OpenScene(ScenePath);
        Debug.Log("FIRST_LEVEL_ROOMS_BUILT");
    }
    private static Sprite Sprite(string id) => AssetDatabase.LoadAssetAtPath<Sprite>(Art + id + ".png");
    public static void AddBoundaries(Transform room)
    {
        var root = new GameObject("Room Boundaries").transform;
        root.SetParent(room, false);
        Edge(root, "Top wall", Pixel(218, 218), Pixel(1290, 218));
        Edge(root, "Bottom wall", Pixel(218, 814), Pixel(1290, 814));
        foreach (float x in new[] {-5.3f, 5.3f})
        {
            Edge(root, "Side wall above door", new Vector2(x, .2f), new Vector2(x, 2.3f));
            Edge(root, "Side wall below door", new Vector2(x, -3.85f), new Vector2(x, -1.3f));
            Edge(root, "Door upper edge", new Vector2(x, .2f), new Vector2(Mathf.Sign(x) * 7, .2f));
            Edge(root, "Door lower edge", new Vector2(x, -1.3f), new Vector2(Mathf.Sign(x) * 7, -1.3f));
            Edge(root, "Door end", new Vector2(Mathf.Sign(x) * 7, -1.3f), new Vector2(Mathf.Sign(x) * 7, .2f));
        }
        Shape(root, "Bottom left toys", new[] {Pixel(218,814), Pixel(218,647), Pixel(245,644), Pixel(266,671), Pixel(281,709), Pixel(305,718), Pixel(326,751), Pixel(337,776), Pixel(374,769), Pixel(403,794), Pixel(439,766), Pixel(470,782), Pixel(480,814)});
        Shape(root, "Bottom right toys", new[] {Pixel(956,814), Pixel(973,795), Pixel(1010,787), Pixel(1052,785), Pixel(1072,760), Pixel(1107,746), Pixel(1135,758), Pixel(1153,770), Pixel(1190,743), Pixel(1224,731), Pixel(1231,695), Pixel(1247,642), Pixel(1265,633), Pixel(1287,692), Pixel(1290,814)});
        Shape(root, "Top left toys", new[] {Pixel(218,218), Pixel(218,263), Pixel(245,277), Pixel(279,273), Pixel(322,264), Pixel(358,245), Pixel(377,218)});
        Shape(root, "Top centre toys", new[] {Pixel(377,190), Pixel(377,218), Pixel(410,241), Pixel(456,233), Pixel(499,239), Pixel(532,231), Pixel(559,212), Pixel(581,235), Pixel(629,245), Pixel(675,237), Pixel(684,215), Pixel(958,217), Pixel(979,233), Pixel(1042,243), Pixel(1095,235), Pixel(1145,255), Pixel(1184,242), Pixel(1184,190)});
        Shape(root, "Top right toys", new[] {Pixel(1184,218), Pixel(1205,242), Pixel(1231,247), Pixel(1251,265), Pixel(1276,263), Pixel(1290,292), Pixel(1290,218)});
    }
    // Coordinates measured on the original 1512 × 1008 room export.
    private static Vector2 Pixel(float x, float y) => new Vector2((x - 756) / 100f, (428 - y) / 100f);
    private static void Edge(Transform parent, string name, Vector2 a, Vector2 b)
    {
        var edge = new GameObject(name).AddComponent<EdgeCollider2D>();
        edge.transform.SetParent(parent, false);
        edge.points = new[] {a, b};
    }
    private static void Shape(Transform parent, string name, Vector2[] points)
    {
        var box = new GameObject(name).AddComponent<PolygonCollider2D>();
        box.transform.SetParent(parent, false);
        box.points = points;
    }
    private static Image Picture(string name, Transform parent, string id, float x, float y, float w, float h)
    {
        var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(parent, false);
        image.sprite = Sprite(id);
        image.raycastTarget = false;
        image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(.5f, .5f);
        image.rectTransform.sizeDelta = new Vector2(w, h);
        image.rectTransform.anchoredPosition = new Vector2(x + w/2 - 960, 540 - y - h/2);
        return image;
    }
}
