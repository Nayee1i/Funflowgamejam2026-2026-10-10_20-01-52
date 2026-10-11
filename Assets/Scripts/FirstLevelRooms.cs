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
    public int CurrentRoom { get; private set; }
    private bool transitioning;
    private bool paused;
    private static readonly Vector3 Spawn = new Vector3(-4.45f, -.55f, 0);

    private void Start() { ShowRoom(0); SetFade(0); }
    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.escapeKey.wasPressedThisFrame && !transitioning) TogglePause();
        if (paused || transitioning) return;
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
        if (transitioning || paused) return;
        var next = player.position;
        bool doorway = Mathf.Abs(next.y + .55f) < .48f;
        if (doorway && next.x > 6.1f && CurrentRoom < rooms.Length - 1)
            StartCoroutine(Transition(CurrentRoom + 1, Spawn));
        else if (doorway && next.x < -6.1f && CurrentRoom > 0)
            StartCoroutine(Transition(CurrentRoom - 1, new Vector3(4.45f, -.55f, 0)));
    }
    private void ShowRoom(int index)
    {
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
    }
    private IEnumerator Fade(float from, float to)
    {
        float elapsed = 0;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetFade(Mathf.Lerp(from, to, Mathf.SmoothStep(0, 1, elapsed / fadeDuration)));
            yield return null;
        }
        SetFade(to);
    }
    private void SetFade(float alpha) { blackout.color = new Color(0, 0, 0, alpha); blackout.raycastTarget = alpha > 0; }
    public void TogglePause() { if (transitioning) return; paused = !paused; pausePanel.SetActive(paused); }
    public void OpenMainMenu() { SceneManager.LoadScene("MainMenu"); }
}
