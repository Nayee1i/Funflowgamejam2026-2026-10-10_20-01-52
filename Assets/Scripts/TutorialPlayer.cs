using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialPlayer : MonoBehaviour
{
    [SerializeField] private TutorialLevel level;
    [SerializeField] private Sprite heartIcon;

    public const int MaxHealth = 3;
    public int Health { get; private set; } = MaxHealth;
    public Sprite HeartIcon => heartIcon;
    public void ConfigureHeartIcon(Sprite icon) => heartIcon = icon;

    private void Awake()
    {
        if (level == null) level = FindAnyObjectByType<TutorialLevel>();
    }

    public void Hit()
    {
        if (Health <= 0 || (level != null && level.Finished)) return;
        Health--;
        if (Health > 0) return;
        if (level != null) level.Restart();
        else SceneManager.LoadScene(SceneManager.GetActiveScene().path);
    }
}
