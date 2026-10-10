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
        public Transform checkpoint;
    }

    [SerializeField] private Transform player;
    [SerializeField] private Camera gameCamera;
    [SerializeField] private Room[] rooms;
    [SerializeField] private string levelSelectionScene;
    public int CurrentRoom { get; private set; }
    public bool Finished { get; private set; }
    private bool restarting;
    private GUIStyle textStyle;
    private float finishedAt;
    private static int resumeRoom = -1;
    private static string resumeScene;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetResumeState()
    {
        resumeRoom = -1;
        resumeScene = null;
    }

    private void Start()
    {
        if (resumeRoom < 0 || resumeScene != SceneManager.GetActiveScene().path) return;
        CurrentRoom = Mathf.Clamp(resumeRoom, 0, rooms.Length - 1);
        resumeRoom = -1;
        resumeScene = null;
        // A fresh scene resets the current encounter, while completed rooms stay cleared.
        for (int i = 0; i < CurrentRoom; i++)
        {
            foreach (TutorialTarget target in rooms[i].targets)
                if (target != null) target.Complete();
            if (rooms[i].gate != null) rooms[i].gate.SetActive(false);
        }
        Transform checkpoint = rooms[CurrentRoom].checkpoint;
        Vector3 position = checkpoint != null ? checkpoint.position : (Vector3)rooms[CurrentRoom].center;
        Quaternion rotation = checkpoint != null ? checkpoint.rotation : Quaternion.identity;
        player.SetPositionAndRotation(position, rotation);
        if (player.TryGetComponent<Rigidbody2D>(out var body))
        {
            body.position = position;
            body.rotation = rotation.eulerAngles.z;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }
        Physics2D.SyncTransforms();
        if (gameCamera != null)
        {
            gameCamera.transform.position = new Vector3(position.x, position.y, gameCamera.transform.position.z);
            if (gameCamera.TryGetComponent<CameraDeadZone>(out var follow)) follow.ResetSmoothing();
        }
    }

    private bool Cleared(int index)
    {
        foreach (TutorialTarget target in rooms[index].targets)
            if (target != null && !target.IsDestroyed) return false;
        return true;
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            Restart();
            return;
        }
        if (Finished)
        {
            if (Time.time >= finishedAt + 1.5f) OpenLevelSelection();
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
        if (restarting) return;
        restarting = true;
        resumeRoom = CurrentRoom;
        resumeScene = SceneManager.GetActiveScene().path;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnGUI()
    {
        if (rooms == null || rooms.Length == 0) return;
        if (textStyle == null)
        {
            textStyle = new GUIStyle(GUI.skin.box) { fontSize = 18, alignment = TextAnchor.MiddleCenter, wordWrap = true };
        }
        GUI.Box(new Rect(10, 10, Screen.width - 20, 76),
            Finished ? "Обучение пройдено!" : $"Комната {CurrentRoom + 1}/5\n{rooms[CurrentRoom].instruction}", textStyle);
        GUI.Box(new Rect(10, Screen.height - 42, Screen.width - 20, 32),
            "WASD / стрелки — движение    Мышь — прицел    ЛКМ — огонь    R — чекпоинт", textStyle);
        if (!Finished) return;
        float x = Screen.width * .5f - 140;
        if (GUI.Button(new Rect(x, Screen.height * .5f, 280, 48), "Выбор уровня"))
            OpenLevelSelection();
    }

    private void OpenLevelSelection()
    {
        if (!string.IsNullOrEmpty(levelSelectionScene) && Application.CanStreamedLevelBeLoaded(levelSelectionScene))
            SceneManager.LoadScene(levelSelectionScene);
    }
}
