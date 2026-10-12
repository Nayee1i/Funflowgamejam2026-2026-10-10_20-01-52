using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialPlayer : MonoBehaviour
{
    [SerializeField] private TutorialLevel level;
    [SerializeField] private Sprite heartIcon;

    public const int MaxHealth = 3;
    public int HealthHalves { get; private set; } = MaxHealth*2;
    public int Health => (HealthHalves+1)/2;
    public float Hearts => HealthHalves*.5f;
    public Sprite HeartIcon => heartIcon;
    public void ConfigureHeartIcon(Sprite icon) => heartIcon = icon;

    private void Awake()
    {
        if (level == null) level = FindAnyObjectByType<TutorialLevel>();
    }

    public void Hit(int damageHalves=2)
    {
        if (Health <= 0 || (level != null && level.Finished)) return;
        HealthHalves=Mathf.Max(0,HealthHalves-Mathf.Max(0,damageHalves));
        GameAudio.Play(Health > 0 ? "player_hit" : "lose");
        if (Health > 0) return;
        if (level != null) level.Restart();
        else SceneManager.LoadScene(SceneManager.GetActiveScene().path);
    }
}
