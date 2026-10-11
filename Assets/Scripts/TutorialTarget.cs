using UnityEngine;

public class TutorialTarget : MonoBehaviour
{
    [SerializeField] private bool shielded;
    [SerializeField] private GameObject shieldVisual;
    [SerializeField, Min(1)] private int maxHealth = 5;
    public int MaxHealth => maxHealth;
    public int Health { get; private set; } = 5;
    public bool IsDestroyed { get; private set; }
    public bool IsShielded => shielded;
    private Transform healthBar;
    private Transform healthFill;
    private Collider2D bodyCollider;
    private Sprite healthSprite;

    private void Awake()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        Health = maxHealth;
        if (shieldVisual != null) shieldVisual.SetActive(shielded);
    }

    public void Complete()
    {
        Health = 0;
        IsDestroyed = true;
        gameObject.SetActive(false);
    }

    public void Hit(bool reflected, bool shieldBreaking)
    {
        if (IsDestroyed) return;
        if (shielded)
        {
            if (shieldBreaking)
            {
                shielded = false;
                if (shieldVisual != null) shieldVisual.SetActive(false);
            }
            return;
        }
        Health = Mathf.Max(0, Health - (reflected ? 5 : 1));
        if (Health == 0)
        {
            Complete();
            return;
        }
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        if (healthBar == null)
        {
            bodyCollider = GetComponent<Collider2D>();
            healthSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1),
                new Vector2(.5f, .5f), 1f);
            SpriteRenderer source = GetComponentInChildren<SpriteRenderer>();
            healthBar = new GameObject("Health Bar").transform;
            healthBar.SetParent(transform, false);
            CreateBarSprite("Background", healthBar, new Vector2(1.3f, .16f),
                new Color(.08f, .1f, .14f), source, 20);
            healthFill = CreateBarSprite("Health", healthBar, new Vector2(1.2f, .1f),
                new Color(.3f, .9f, .45f), source, 21);
        }
        float fraction = (float)Health / MaxHealth;
        healthFill.localScale = new Vector3(1.2f * fraction, .1f, 1f);
        healthFill.localPosition = new Vector3(-.6f * (1f - fraction), 0, 0);
        healthBar.gameObject.SetActive(Health > 0 && Health < MaxHealth);
        PositionHealthBar();
    }

    private Transform CreateBarSprite(string objectName, Transform parent, Vector2 size,
        Color color, SpriteRenderer source, int order)
    {
        Transform part = new GameObject(objectName).transform;
        part.SetParent(parent, false);
        part.localScale = new Vector3(size.x, size.y, 1f);
        SpriteRenderer renderer = part.gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = healthSprite;
        renderer.color = color;
        renderer.sortingOrder = order;
        if (source != null)
        {
            renderer.sharedMaterial = source.sharedMaterial;
            renderer.sortingLayerID = source.sortingLayerID;
            renderer.sortingOrder += source.sortingOrder;
        }
        return part;
    }

    private void LateUpdate()
    {
        if (healthBar != null) PositionHealthBar();
    }

    private void PositionHealthBar()
    {
        float top = bodyCollider != null ? bodyCollider.bounds.max.y : transform.position.y + .6f;
        healthBar.SetPositionAndRotation(new Vector3(transform.position.x, top + .3f, 0), Quaternion.identity);
    }

    private void OnDestroy()
    {
        if (healthSprite != null) Destroy(healthSprite);
    }
}
