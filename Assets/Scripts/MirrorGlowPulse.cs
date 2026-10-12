using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class MirrorGlowPulse : MonoBehaviour
{
    public float originalAlpha = 1;
    [Range(0,1)] public float opacity = .75f;
    public float period = 3.6f;
    [Range(0,.5f)] public float variation = .10f;
    SpriteRenderer visual;
    FirstLevelRooms level;
    float elapsed;
    void OnEnable()
    {
        visual = GetComponent<SpriteRenderer>();
        level = FindAnyObjectByType<FirstLevelRooms>();
        elapsed = 0;
        Apply(0);
    }
    void Update()
    {
        if(level != null && level.pausePanel != null && level.pausePanel.activeSelf)return;
        elapsed += Time.deltaTime;
        Apply(elapsed);
    }
    void Apply(float time)
    {
        var color = visual.color;
        color.a = originalAlpha * opacity * (1 + variation * Mathf.Sin(time * Mathf.PI * 2 / Mathf.Max(.1f,period)));
        visual.color = color;
    }
}
