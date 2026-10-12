using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class BuildFigmaMainMenu
{
    private const string Art = "Assets/UI/MainMenu/";
    public const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private static bool previewHover;

    private static Material WhiteGlyphMaterial()
    {
        const string path = Art + "WhiteGlyph.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("UI/Figma White Glyph"));
            AssetDatabase.CreateAsset(material, path);
        }
        return material;
    }

    public static void BuildHoverPreview()
    {
        previewHover = true;
        BuildAndPreview();
    }

    [MenuItem("Bounce Club/Rebuild Layout Main Menu")] 
    public static void Build()
    {
        AssetDatabase.Refresh();
        foreach (string file in System.IO.Directory.GetFiles(Art, "*.png"))
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(file);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0, 0, -10);
        var camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.orthographic = true;
        var canvasObject = new GameObject("Main Menu Canvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        var root = canvasObject.GetComponent<RectTransform>();

        // A black surround keeps the complete composition visible at any aspect ratio.
        var surround = Image("Letterbox", root, null);
        Stretch(surround.rectTransform);
        surround.color = Color.black;
        var composition = new GameObject("Composition 1920x1080", typeof(RectTransform), typeof(AspectRatioFitter));
        var comp = composition.GetComponent<RectTransform>();
        comp.SetParent(root, false);
        var fitter = composition.GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = 1920f / 1080f;

        var background = Image("Background and BOUNCE CLUB title", comp, Sprite("Background"));
        Stretch(background.rectTransform);
        var panel = Image("Crown Button Panel", comp, Sprite("ButtonPanel"));
        Place(panel.rectTransform, 576, 524, 768, 463);
        var playButton = Button("Play", comp, "PlayLabel", 899, 718, 122, 40);
        var settingsButton = Button("Settings", comp, "SettingsLabel", 868, 780, 183, 40);
        var exitButton = Button("Exit", comp, "ExitLabel", 908, 842, 102, 40);
        var windows = canvasObject.AddComponent<MainMenuWindows>();
        windows.settingsWindow = Modal("Settings", comp, windows);
        windows.exitWindow = Modal("Exit", comp, windows);
        windows.modesWindow = Modal("Modes", comp, windows);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(playButton.onClick, windows.Play);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(settingsButton.onClick, windows.Settings);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(exitButton.onClick, windows.Exit);

        var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        EditorSceneManager.SaveScene(scene, ScenePath);
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };
        foreach (var entry in EditorBuildSettings.scenes)
            if (entry.path != ScenePath && System.IO.File.Exists(entry.path)) scenes.Add(entry);
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("Figma settings and exit windows saved.");
    }

    public static void BuildAndPreview()
    {
        Build();
        var canvas = Object.FindAnyObjectByType<Canvas>();
        var camera = Camera.main;
        var target = new RenderTexture(1920, 1080, 24);
        camera.targetTexture = target;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 1;
        Canvas.ForceUpdateCanvases();
        if (previewHover)
            foreach (var button in Object.FindObjectsByType<FigmaMenuButton>())
                if (button.name == "Play Button") button.PreviewHover();
        camera.Render();
        var previous = RenderTexture.active;
        RenderTexture.active = target;
        var image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
        image.Apply();
        System.IO.File.WriteAllBytes(previewHover ? "../MainMenu-hover-preview.png" : "../MainMenu-preview.png", image.EncodeToPNG());
        RenderTexture.active = previous;
        camera.targetTexture = null;
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        Object.DestroyImmediate(image);
        Object.DestroyImmediate(target);
        Debug.Log("Main menu preview exported.");
    }

    [System.Serializable] private class Layer
    {
        public string id, asset, modal, name;
        public float x, y, w, h;
    }
    [System.Serializable] private class Layout { public Layer[] layers; }

    public static GameObject Modal(string name, Transform parent, MainMenuWindows windows)
    {
        var overlay = Image(name + " Window", parent, null);
        Stretch(overlay.rectTransform);
        overlay.color = new Color(0, 0, 0, .54f);
        overlay.raycastTarget = true;
        var json = System.IO.File.ReadAllText(Art + "ModalLayout.json").TrimStart('\uFEFF');
        var layout = JsonUtility.FromJson<Layout>("{\"layers\":" + json + "}");
        var soundBars = new System.Collections.Generic.List<Image>();
        var musicBars = new System.Collections.Generic.List<Image>();
        var levelButtons = new System.Collections.Generic.List<UnityEngine.UI.Button>();
        var levelArtwork = new System.Collections.Generic.List<Image>();
        foreach (var layer in layout.layers)
        {
            if (layer.modal != name) continue;
            if (layer.id == "48:2840" || layer.id == "48:2843") continue;
            var graphic = Image(layer.name + " " + layer.id, overlay.transform, Sprite(layer.asset));
            Place(graphic.rectTransform, layer.x, layer.y, layer.w, layer.h);
            if (layer.id == "48:2837" || layer.id == "48:2828" || layer.id == "48:2831") soundBars.Add(graphic);
            if (layer.id == "48:2825" || layer.id == "48:2834") musicBars.Add(graphic);
            UnityEngine.Events.UnityAction action = null;
            if (layer.id == "48:2691") action = windows.StartLevel1;
            if (layer.id == "48:2815") action = windows.SoundUp;
            if (layer.id == "48:2819") action = windows.SoundDown;
            if (layer.id == "48:2820") action = windows.MusicUp;
            if (layer.id == "48:2821") action = windows.MusicDown;
            if (layer.id == "48:2675") action = windows.Quit;
            if (layer.id == "48:2676" || layer.id == "48:2677" || layer.id == "48:2822" || layer.id == "48:2695") action = windows.Close;
            bool modeButton = layer.id == "48:2691" || layer.id == "48:2692" || layer.id == "48:2693" || layer.id == "48:2694";
            if (layer.id == "48:2691") action = windows.Level1;
            if (layer.id == "48:2692") action = windows.Level2;
            if (layer.id == "48:2693") action = windows.Level3;
            if (layer.id == "48:2694") action = windows.Arena;
            if (action != null || modeButton)
            {
                var hit = Image(layer.name + " Hit Area", overlay.transform, null);
                Place(hit.rectTransform, layer.x - 20, layer.y - 14, layer.w + 40, layer.h + 28);
                hit.color = Color.clear;
                hit.canvasRenderer.cullTransparentMesh = false;
                hit.raycastTarget = true;
                var button = (layer.id == "48:2691" || layer.id == "48:2694" || layer.id == "48:2677" || layer.id == "48:2822" || layer.id == "48:2695")
                    ? hit.gameObject.AddComponent<FigmaMenuButton>()
                    : hit.gameObject.AddComponent<UnityEngine.UI.Button>();
                if (button is FigmaMenuButton hover) hover.Configure(graphic.rectTransform, null);
                button.targetGraphic = hit;
                button.transition = Selectable.Transition.None;
                button.interactable = layer.id != "48:2692" && layer.id != "48:2693";
                if (modeButton && layer.id != "48:2694")
                {
                    levelButtons.Add(button);
                    levelArtwork.Add(graphic);
                }
                if (action != null) UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, action);
            }
        }
        if (name == "Modes")
        {
            windows.levelButtons = levelButtons.ToArray();
            windows.levelButtonArtwork = levelArtwork.ToArray();
            windows.unlockedLevelArtwork = Sprite("Modal_48_2691");
        }
        if (name == "Settings")
        {
            var middle = Image("Music middle bar", overlay.transform, Sprite("Modal_48_2828"));
            Place(middle.rectTransform, 992, 709, 82, 67.091f);
            musicBars.Add(middle);
            soundBars.Sort((a,b) => a.rectTransform.anchoredPosition.x.CompareTo(b.rectTransform.anchoredPosition.x));
            musicBars.Sort((a,b) => a.rectTransform.anchoredPosition.x.CompareTo(b.rectTransform.anchoredPosition.x));
            windows.soundBars = soundBars.ToArray();
            windows.musicBars = musicBars.ToArray();
            windows.emptyBar = Sprite("Modal_48_2831");
            windows.filledBar = Sprite("Modal_48_2837");
            for (int i = 0; i < soundBars.Count; i++) soundBars[i].sprite = i < 1 ? windows.filledBar : windows.emptyBar;
            for (int i = 0; i < musicBars.Count; i++) musicBars[i].sprite = i < 2 ? windows.filledBar : windows.emptyBar;
        }
        overlay.gameObject.SetActive(false);
        return overlay.gameObject;
    }

    private static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Art + name + ".png");

    private static Image Image(string name, Transform parent, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        return image;
    }

    private static FigmaMenuButton Button(string name, Transform parent, string label, float x, float y, float width, float height)
    {
        var image = Image(name + " Button", parent, null);
        Place(image.rectTransform, 820, y - 8, 280, 56);
        image.raycastTarget = true;
        var button = image.gameObject.AddComponent<FigmaMenuButton>();
        button.targetGraphic = image;
        image.color = Color.clear;
        image.canvasRenderer.cullTransparentMesh = false;
        button.transition = UnityEngine.UI.Selectable.Transition.None;
        // Exported text preserves the exact Figma typography without an external font dependency.
        var text = Image(name + " Label (Figma)", image.transform, Sprite(label));
        text.rectTransform.anchorMin = text.rectTransform.anchorMax = new Vector2(.5f, .5f);
        // Figma exports glyph render bounds, which are smaller than the text layout box.
        // Match those bounds so the PNG labels are not stretched to the font's line height.
        Vector2 renderSize = name == "Play" ? new Vector2(116.546f, 22.716f) :
            name == "Settings" ? new Vector2(181.217f, 28.332f) : new Vector2(101.409f, 26.136f);
        Vector2 renderPosition = name == "Play" ? new Vector2(-.035f, 3.278f) :
            name == "Settings" ? new Vector2(-.959f, 5.978f) : new Vector2(-.215f, 1.676f);
        text.rectTransform.sizeDelta = renderSize;
        text.rectTransform.anchoredPosition = renderPosition;
        var white = Image(name + " White Glyphs", text.transform, text.sprite);
        Stretch(white.rectTransform);
        white.material = WhiteGlyphMaterial();
        white.color = Color.clear;
        button.Configure(text.rectTransform, white);
        return button;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static void Place(RectTransform rect, float x, float y, float w, float h)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.sizeDelta = new Vector2(w, h);
        rect.anchoredPosition = new Vector2(x + w / 2 - 960, 540 - y - h / 2);
    }
}


