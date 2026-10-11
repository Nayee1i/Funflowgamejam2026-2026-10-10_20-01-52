using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class TutorialLevel : MonoBehaviour
{
    [System.Serializable]
    public class Room
    {
        public string instruction;
        public Vector2 center;
        public Vector2 size = new Vector2(18, 10);
        public GameObject gate;
        public TutorialTarget[] targets;
    }

    [SerializeField] private Transform player;
    [SerializeField] private Camera gameCamera;
    [SerializeField] private Room[] rooms;
    [SerializeField] private string levelSelectionScene;
    [SerializeField] private string completionMessage = "Обучение пройдено!";
    public int CurrentRoom { get; private set; }
    public bool Finished { get; private set; }
    private bool restarting;
    private GUIStyle textStyle;
    private float finishedAt;
    private bool Cleared(int index)
    {
        foreach (TutorialTarget target in rooms[index].targets)
            if (target != null && !target.IsDestroyed) return false;
        return true;
    }

    public bool TryGetRoomIndex(TutorialTarget target, out int index)
    {
        if (target != null && rooms != null)
            for (int i = 0; i < rooms.Length; i++)
                if (rooms[i].targets != null && System.Array.IndexOf(rooms[i].targets, target) >= 0)
                {
                    index = i;
                    return true;
                }
        index = -1;
        return false;
    }

    private void Update()
    {
        if (Finished)
        {
            if (Time.time >= finishedAt + 1.5f) OpenLevelSelection();
            return;
        }
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            Restart();
            return;
        }
        if (restarting || rooms == null || rooms.Length == 0 || player == null) return;
        Room current = rooms[CurrentRoom];
        bool cleared = Cleared(CurrentRoom);
        if (cleared && current.gate != null) current.gate.SetActive(false);
        if (CurrentRoom == rooms.Length - 1 && cleared)
        {
            Finished = true;
            finishedAt = Time.time;
            LevelProgression.Complete(SceneManager.GetActiveScene().name);
            if (player.TryGetComponent<TankMovement>(out var movement)) movement.enabled = false;
            foreach (Bullet ball in FindObjectsByType<Bullet>())
                Destroy(ball.gameObject);
            return;
        }
        if (cleared && CurrentRoom < rooms.Length - 1)
        {
            Room next = rooms[CurrentRoom + 1];
            Vector2 offset = (Vector2)player.position - next.center;
            if (Mathf.Abs(offset.x) < next.size.x * .5f && Mathf.Abs(offset.y) < next.size.y * .5f)
                CurrentRoom++;
        }
    }

    private void LateUpdate()
    {
        if (gameCamera == null || rooms == null || rooms.Length == 0) return;
        gameCamera.orthographicSize = Mathf.Max(7f, 10.5f / gameCamera.aspect);
    }

    public void Restart()
    {
        if (restarting || Finished) return;
        restarting = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnGUI()
    {
        if (rooms == null || rooms.Length == 0) return;
        if (textStyle == null)
        {
            textStyle = new GUIStyle(GUI.skin.box) { fontSize = 18, alignment = TextAnchor.MiddleCenter, wordWrap = true };
        }
        float healthHudWidth = gameCamera != null
            ? 160f * Mathf.Min(gameCamera.pixelWidth / 1280f, gameCamera.pixelHeight / 720f) : 160f;
        GUI.Box(new Rect(healthHudWidth, 10, Mathf.Max(1f, Screen.width - healthHudWidth - 10), 76),
            Finished ? completionMessage : $"Комната {CurrentRoom + 1}/{rooms.Length}\n{rooms[CurrentRoom].instruction}", textStyle);
        GUI.Box(new Rect(10, Screen.height - 42, Screen.width - 20, 32),
            "WASD / стрелки — движение    Мышь — прицел    ЛКМ — огонь    R — заново", textStyle);
        if (!Finished) return;
        float x = Screen.width * .5f - 140;
        GUI.Box(new Rect(x - 80, Screen.height * .5f - 70, 440, 56), completionMessage, textStyle);
        if (GUI.Button(new Rect(x, Screen.height * .5f, 280, 48), "Выбор уровня"))
            OpenLevelSelection();
    }

    private void OpenLevelSelection()
    {
        if (levelSelectionScene == "MainMenu")
        {
            MainMenuWindows.ReturnToLevelSelection();
            return;
        }
        if (!string.IsNullOrEmpty(levelSelectionScene) && Application.CanStreamedLevelBeLoaded(levelSelectionScene))
            SceneManager.LoadScene(levelSelectionScene);
    }
}
