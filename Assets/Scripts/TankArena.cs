using UnityEngine;
using UnityEngine.SceneManagement;

public class TankArena : MonoBehaviour
{
    public const float HalfWidth = 8f;
    public const float HalfHeight = 4.5f;
    [SerializeField] private Shader spriteShader;
    [SerializeField] private bool generateOnPlay = true;
    private Sprite square;
    private Sprite circle;
    private Texture2D squareTexture;
    private Texture2D circleTexture;
    private Material spriteMaterial;
    private GUIStyle titleStyle;
    private GUIStyle controlsStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartSampleScene()
    {
        // The template scene also works immediately when the editor is already on it.
        if (SceneManager.GetActiveScene().name == "SampleScene" &&
            FindAnyObjectByType<TankArena>() == null)
            new GameObject("Tank Arena").AddComponent<TankArena>();
    }

    private void Awake()
    {
        if (!generateOnPlay) return;
        squareTexture = CreateTexture(false);
        circleTexture = CreateTexture(true);
        square = Sprite.Create(squareTexture, new Rect(0, 0, 64, 64),
            new Vector2(0.5f, 0.5f), 64f);
        circle = Sprite.Create(circleTexture, new Rect(0, 0, 64, 64),
            new Vector2(0.5f, 0.5f), 64f);
        spriteMaterial = new Material(spriteShader != null ? spriteShader :
            Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));

        Camera camera = Camera.main;
        if (camera == null)
        {
            camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.gameObject.AddComponent<AudioListener>();
        }
        camera.transform.SetPositionAndRotation(new Vector3(0f, 0f, -10f), Quaternion.identity);
        camera.orthographic = true;
        camera.orthographicSize = 6f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.045f, 0.065f, 0.10f);
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;
        camera.gameObject.AddComponent<ArenaCameraFit>();

        Shape("Floor", transform, Vector2.zero, new Vector2(16f, 9f),
            new Color(0.075f, 0.105f, 0.15f), false, -2);
        Color gridColor = new Color(0.10f, 0.14f, 0.19f);
        for (int x = -7; x <= 7; x++)
            Shape("Grid", transform, new Vector2(x, 0), new Vector2(0.015f, 9), gridColor, false, -1);
        for (int y = -4; y <= 4; y++)
            Shape("Grid", transform, new Vector2(0, y), new Vector2(16, 0.015f), gridColor, false, -1);

        Wall("Top Wall", new Vector2(0, HalfHeight + 0.25f), new Vector2(17, 0.5f));
        Wall("Bottom Wall", new Vector2(0, -HalfHeight - 0.25f), new Vector2(17, 0.5f));
        Wall("Left Wall", new Vector2(-HalfWidth - 0.25f, 0), new Vector2(0.5f, 9));
        Wall("Right Wall", new Vector2(HalfWidth + 0.25f, 0), new Vector2(0.5f, 9));

        Mirror("Top Blue Mirror", new Vector2(-2, 4.28f), new Vector2(4, 0.2f));
        Mirror("Bottom Blue Mirror", new Vector2(2, -4.28f), new Vector2(4, 0.2f));

        GameObject tank = new GameObject("Tank");
        tank.transform.SetParent(transform, false);
        tank.layer = LayerMask.NameToLayer("Ignore Raycast");
        BoxCollider2D collider = tank.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.95f, 1.1f);
        tank.AddComponent<Rigidbody2D>();
        Shape("Left Track", tank.transform, new Vector2(-0.36f, 0), new Vector2(0.23f, 1.1f),
            new Color(0.19f, 0.25f, 0.31f), false, 2);
        Shape("Right Track", tank.transform, new Vector2(0.36f, 0), new Vector2(0.23f, 1.1f),
            new Color(0.19f, 0.25f, 0.31f), false, 2);
        Shape("Body", tank.transform, Vector2.zero, new Vector2(0.65f, 0.95f),
            new Color(0.26f, 0.73f, 0.58f), false, 3);
        GameObject turret = new GameObject("Turret");
        turret.transform.SetParent(tank.transform, false);
        Shape("Turret Circle", turret.transform, Vector2.zero, new Vector2(0.48f, 0.48f),
            new Color(0.43f, 0.94f, 0.73f), true, 5);
        Shape("Barrel", turret.transform, new Vector2(0, 0.38f), new Vector2(0.16f, 0.65f),
            new Color(0.61f, 1f, 0.84f), false, 4);
        Transform muzzle = new GameObject("Muzzle").transform;
        muzzle.SetParent(turret.transform, false);
        muzzle.localPosition = new Vector3(0, 0.82f, 0);

        GameObject projectile = Shape("Ball Template", transform, Vector2.zero, new Vector2(0.3f, 0.3f),
            new Color(1f, 0.86f, 0.35f), true, 6);
        Bullet bullet = projectile.AddComponent<Bullet>();
        projectile.SetActive(false);
        tank.AddComponent<TankMovement>().Configure(turret.transform, muzzle, bullet, camera);
    }

    private void Wall(string wallName, Vector2 position, Vector2 size)
    {
        GameObject wall = Shape(wallName, transform, position, size,
            new Color(0.37f, 0.48f, 0.61f), false, 1);
        wall.AddComponent<BoxCollider2D>();
    }

    private void Mirror(string mirrorName, Vector2 position, Vector2 size)
    {
        GameObject mirror = Shape(mirrorName, transform, position, size,
            new Color(0.12f, 0.52f, 1f), false, 2);
        mirror.AddComponent<BoxCollider2D>();
        mirror.AddComponent<BlueMirror>();
    }

    private GameObject Shape(string shapeName, Transform parent, Vector2 position,
        Vector2 size, Color color, bool round, int order)
    {
        GameObject shape = new GameObject(shapeName);
        shape.transform.SetParent(parent, false);
        shape.transform.localPosition = position;
        shape.transform.localScale = new Vector3(size.x, size.y, 1f);
        SpriteRenderer renderer = shape.AddComponent<SpriteRenderer>();
        renderer.sprite = round ? circle : square;
        renderer.color = color;
        renderer.sortingOrder = order;
        renderer.sharedMaterial = spriteMaterial;
        return shape;
    }

    private static Texture2D CreateTexture(bool round)
    {
        Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        Color[] pixels = new Color[64 * 64];
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(32, 32));
                pixels[y * 64 + x] = new Color(1, 1, 1,
                    round ? Mathf.Clamp01(32f - distance) : 1f);
            }
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    private void OnGUI()
    {
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.UpperCenter };
            titleStyle.normal.textColor = new Color(0.61f, 1f, 0.84f);
            controlsStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, alignment = TextAnchor.LowerCenter };
            controlsStyle.normal.textColor = new Color(0.8f, 0.85f, 0.92f);
        }
        GUI.Label(new Rect(0, 12, Screen.width, 34), "ONE BOUNCE", titleStyle);
        GUI.Label(new Rect(0, Screen.height - 40, Screen.width, 30),
            "WASD / Arrows: move     Mouse: aim     Hold left click: shoot     Blue mirror: one bounce. Walls: absorb.", controlsStyle);
    }

    private void OnDestroy()
    {
        Destroy(square);
        Destroy(circle);
        Destroy(squareTexture);
        Destroy(circleTexture);
        Destroy(spriteMaterial);
    }
}
