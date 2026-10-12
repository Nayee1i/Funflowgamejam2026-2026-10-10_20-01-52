using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FirstLevelRooms : MonoBehaviour
{
    public GameObject[] rooms;
    public Transform player;
    public Image blackout;
    public GameObject pausePanel;
    public float speed = 3.5f;
    public float fadeDuration = .45f;
    public float playerRadius = .54f;
    public bool emptySandbox;
    [Tooltip("Existing levels retain their original doorway and spawn defaults.")]
    public float doorwayY = -.55f;
    public float exitX = 6.1f;
    public float doorwayHalfHeight = .48f;
    public float spawnX = 4.45f;
    public int CurrentRoom { get; private set; }
    private bool transitioning;
    private bool paused;
    public bool ModalOpen { get; private set; }
    public bool Finished { get; private set; }
    int modalClosedFrame=-1;
    public bool AllowMouseShotThisFrame => Time.frameCount!=modalClosedFrame;
    public bool CanPlay => !paused && !transitioning && !ModalOpen && !Finished;
    public void SetModal(bool open){ModalOpen=open;if(!open)modalClosedFrame=Time.frameCount;}
    public void CompleteLevel()
    {
        if(Finished || emptySandbox || !CanPlay || CurrentRoom!=rooms.Length-1 ||
            !FirstLevelEnemies.Cleared(rooms[CurrentRoom]) ||
            player.position.x<=exitX || Mathf.Abs(player.position.y-doorwayY)>=doorwayHalfHeight ||
            FirstLevelEffects.HasPendingImpacts(this))return;
        Finished=true;LevelProgression.Complete(SceneManager.GetActiveScene().name);FirstLevelShot.ClearAll();
        GetComponent<FirstLevelPresentation>().ShowVictory();
    }
    private Vector3 Spawn => new Vector3(-spawnX, doorwayY, 0);

    private void Awake()
    {
        FigmaPauseMenu.Configure(this);
        transitioning = true;
        SetFade(1);
    }
    private IEnumerator Start()
    {
        if(!emptySandbox)
        {
            var workshop=GetComponent<SecondLevelWorkshop>();
            if(workshop!=null)workshop.ConfigureCombat(this);
            else FirstLevelEnemies.Place(this);
        }
        ShowRoom(0);
        var presentation = emptySandbox ? null : GetComponent<FirstLevelPresentation>();
        if(presentation!=null)presentation.EnsureUI();
        int blackoutOrder = blackout.transform.GetSiblingIndex();
        blackout.transform.SetAsLastSibling();
        yield return Fade(1, 0, .5f);
        blackout.transform.SetSiblingIndex(blackoutOrder);
        if(presentation!=null)presentation.ShowHint(0);
        transitioning = false;
    }
    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.escapeKey.wasPressedThisFrame && !transitioning && !ModalOpen && !Finished) TogglePause();
        if (!CanPlay) return;
        ResolveBoundaryOverlap();
        Vector2 direction = Vector2.zero;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) direction.y++;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) direction.y--;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) direction.x++;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) direction.x--;
        direction = direction.normalized;
        Vector2 step = direction * speed * Time.deltaTime;
        MoveAxis(new Vector2(step.x, 0));
        MoveAxis(new Vector2(0, step.y));
        if (direction.sqrMagnitude > 0)
            player.rotation = Quaternion.RotateTowards(player.rotation,
                Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 59f), 360f * Time.deltaTime);
        TryRoomExit();
    }
    private void MoveAxis(Vector2 step)
    {
        float distance = step.magnitude;
        if (distance < .00001f) return;
        Vector2 origin = player.position;
        foreach (var hit in Physics2D.CircleCastAll(origin, playerRadius, step / distance, distance + .002f))
        {
            if (hit.collider == null || hit.collider.isTrigger) continue;
            // A contact at the start must not prevent moving away from that wall.
            Vector2 outward = origin - hit.collider.ClosestPoint(origin);
            if (hit.distance <= .001f && Vector2.Dot(step, outward) > 0) continue;
            distance = Mathf.Min(distance, Mathf.Max(0, hit.distance - .002f));
        }
        player.position += (Vector3)(step.normalized * distance);
        ResolveBoundaryOverlap();
    }
    private void ResolveBoundaryOverlap()
    {
        // Recover from numerical overlap or from boundaries edited during Play Mode.
        for (int pass = 0; pass < 8; pass++)
        {
            bool corrected = false;
            foreach (var boundary in Physics2D.OverlapCircleAll(player.position, playerRadius))
            {
                if (boundary.isTrigger) continue;
                Vector2 position = player.position;
                Vector2 outward = position - boundary.ClosestPoint(position);
                float separation = outward.magnitude;
                if (separation >= playerRadius) continue;
                if (separation < .0001f)
                {
                    player.position = Spawn;
                    return;
                }
                player.position += (Vector3)(outward / separation * (playerRadius - separation + .003f));
                corrected = true;
            }
            if (!corrected) break;
        }
    }
    public void TryRoomExit()
    {
        if(emptySandbox)return;
        if (transitioning || paused) return;
        var next = player.position;
        bool doorway = Mathf.Abs(next.y - doorwayY) < doorwayHalfHeight;
        if (doorway && next.x > exitX && FirstLevelEnemies.Cleared(rooms[CurrentRoom]))
        {
            if(CurrentRoom < rooms.Length-1)StartCoroutine(Transition(CurrentRoom + 1, Spawn));
            else CompleteLevel();
        }
        else if (doorway && next.x < -exitX && CurrentRoom > 0)
            StartCoroutine(Transition(CurrentRoom - 1, new Vector3(spawnX, doorwayY, 0)));
    }
    private void ShowRoom(int index)
    {
        FirstLevelShot.ClearAll();
        FirstLevelEffects.Clear();
        CurrentRoom = index;
        for (int i = 0; i < rooms.Length; i++) rooms[i].SetActive(i == index);
        player.position = Spawn;
    }
    private IEnumerator Transition(int index, Vector3 spawn)
    {
        transitioning = true;
        yield return Fade(0, 1);
        ShowRoom(index);
        player.position = spawn;
        yield return Fade(1, 0);
        transitioning = false;
        GetComponent<FirstLevelPresentation>().ShowHint(index);
    }
    private IEnumerator Fade(float from, float to, float duration = -1)
    {
        if(duration < 0)duration = fadeDuration;
        duration = Mathf.Max(.001f, duration);
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetFade(Mathf.Lerp(from, to, Mathf.SmoothStep(0, 1, elapsed / duration)));
            yield return null;
        }
        SetFade(to);
    }
    private void SetFade(float alpha) { blackout.color = new Color(0, 0, 0, alpha); blackout.raycastTarget = alpha > 0; }
    public void TogglePause() { if (transitioning) return; paused = !paused; pausePanel.SetActive(paused); }
    public void OpenMainMenu() { SceneManager.LoadScene("MainMenu"); }
}
