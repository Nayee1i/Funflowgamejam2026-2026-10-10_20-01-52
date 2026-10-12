using UnityEngine;

public class TutorialTarget : MonoBehaviour
{
    [SerializeField] private bool shielded;
    [SerializeField] private GameObject shieldVisual;
    [SerializeField, Min(1)] private int maxHealth = 2;
    [SerializeField] private bool isBoss;
    [SerializeField] private bool redRobot;
    public bool IsBoss => isBoss;
    public bool IsRedRobot => redRobot;
    public int MaxHealth => maxHealth;
    public int Health { get; private set; } = 2;
    public bool IsDestroyed { get; private set; }
    public bool IsShielded => shielded;
    private Transform healthBar;
    private Transform healthFill;
    private Collider2D bodyCollider;
    private Sprite healthSprite;
    public Collider2D ShieldCollider { get; private set; }

    public void AlignBodyHitbox()
    {
        var body=transform.Find("Body")?.GetComponent<SpriteRenderer>();
        var circle=GetComponent<CircleCollider2D>();
        if(body==null || body.sprite==null || circle==null)return;
        var bounds=body.sprite.bounds;
        var points=new System.Collections.Generic.List<Vector2>();bool found=false;
        for(int i=0;i<body.sprite.GetPhysicsShapeCount();i++)
        {
            points.Clear();body.sprite.GetPhysicsShape(i,points);
            foreach(var p in points){if(!found){bounds=new Bounds(p,Vector3.zero);found=true;}else bounds.Encapsulate(p);}
        }
        Vector3 centre=body.transform.TransformPoint(bounds.center);
        circle.offset=transform.InverseTransformPoint(centre);
        var turret=transform.Find("Turret");
        if(turret!=null && Vector2.Distance(turret.position,centre)>.05f)
        {
            // Reposition the aiming pivot without moving the authored turret artwork.
            var head=turret.Find("Turret Artwork");Vector3 headPosition=head!=null ? head.position : Vector3.zero;
            turret.position=centre;if(head!=null)head.position=headPosition;
        }
    }

    public void SetCombatHealth(int health,bool red)
    {
        maxHealth=Mathf.Max(1,health);redRobot=red;Health=maxHealth;
    }

    void EnsureShieldHitbox()
    {
        if(shieldVisual==null)return;
        var visual=shieldVisual.GetComponentInChildren<SpriteRenderer>();
        if(visual==null || visual.sprite==null)return;
        var hitbox=visual.GetComponent<PolygonCollider2D>();
        if(hitbox==null)hitbox=visual.gameObject.AddComponent<PolygonCollider2D>();
        hitbox.isTrigger=true;
        var bounds=visual.sprite.bounds;
        var points=new System.Collections.Generic.List<Vector2>();
        bool found=false;
        for(int shape=0;shape<visual.sprite.GetPhysicsShapeCount();shape++)
        {
            points.Clear();visual.sprite.GetPhysicsShape(shape,points);
            foreach(var point in points)
            {
                if(!found){bounds=new Bounds(point,Vector3.zero);found=true;}
                else bounds.Encapsulate(point);
            }
        }
        var ellipse=new Vector2[48];
        for(int i=0;i<ellipse.Length;i++)
        {
            float angle=i*Mathf.PI*2/ellipse.Length;
            ellipse[i]=(Vector2)bounds.center+new Vector2(Mathf.Cos(angle)*bounds.extents.x,Mathf.Sin(angle)*bounds.extents.y);
        }
        hitbox.pathCount=1;hitbox.points=ellipse;ShieldCollider=hitbox;
    }

    public void Configure(bool hasShield, GameObject visual, int health = 2, bool boss = false, bool red = false)
    {
        shielded = hasShield;
        shieldVisual = visual;
        maxHealth = Mathf.Max(1, health);
        isBoss = boss;
        redRobot = red;
        Health = maxHealth;
        IsDestroyed = false;
        EnsureShieldHitbox();
        if (shieldVisual != null) shieldVisual.SetActive(shielded);
    }

    private void Awake()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        Health = maxHealth;
        EnsureShieldHitbox();
        if (shieldVisual != null) shieldVisual.SetActive(shielded);
    }

    public void Complete()
    {
        if (!IsDestroyed) GameAudio.Play("enemy_die");
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
                GetComponent<CastleBoss>()?.OnShieldBroken();
            }
            return;
        }
        Health = Mathf.Max(0, Health - (reflected ? 2 : 1));
        if (Health == 0)
        {
            Complete();
            return;
        }
        UpdateHealthBar();
    }

    public void RestoreShield()
    {
        if (IsDestroyed || shieldVisual == null) return;
        shielded = true;
        shieldVisual.SetActive(true);
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
