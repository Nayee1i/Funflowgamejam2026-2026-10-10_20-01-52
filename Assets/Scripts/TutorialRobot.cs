using UnityEngine;

public class TutorialRobot : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Transform turret;
    [SerializeField] private Transform muzzle;
    [Tooltip("Extra barrels fired in the same volley; keep their transforms under the turret rig.")]
    [SerializeField] private Transform[] additionalMuzzles;
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private TutorialLevel level;
    [SerializeField] private int roomIndex = 2;
    [Tooltip("Cooldown after the last bullet of a burst while the player remains visible.")]
    [SerializeField, Min(.01f)] private float shotInterval = 2.1f;
    [SerializeField, Min(1)] private int shotsPerBurst = 1;
    [Tooltip("Seconds between bullets within a burst.")]
    [SerializeField, Min(.01f)] private float burstShotInterval = .25f;
    [Tooltip("Reaction delay on every new sighting, including after leaving cover.")]
    [SerializeField, Min(0f)] private float firstShotDelay = .8f;
    [Tooltip("Extra per-enemy reaction delay added to First Shot Delay.")]
    [SerializeField, Min(0f)] private float reactionOffset;
    [SerializeField] private float sightRange = 16f;
    [Range(0f, 360f)]
    [SerializeField] private float sightAngle = 150f;
    private float nextShot;
    private float nextBurstShot;
    private int burstShotsRemaining;
    private bool hasVisualContact;
    private Collider2D ownCollider;
    private readonly RaycastHit2D[] visibilityHits = new RaycastHit2D[32];
    private Vector2 visibleAim;
    private FirstLevelRooms firstLevel;
    private Sprite firstLevelBall;
    public void ConfigureFirstLevel(FirstLevelRooms owner, Transform head, Transform barrel, Sprite ball, int room)
    {
        firstLevel=owner;player=owner.player;turret=head;muzzle=barrel;firstLevelBall=ball;roomIndex=room;
        sightAngle=360f;ownCollider=GetComponent<Collider2D>();
    }

    private void Awake() => ownCollider = GetComponent<Collider2D>();

    private void Start()
    {
        if (level == null) level = FindAnyObjectByType<TutorialLevel>();
        if (player == null)
        {
            TutorialPlayer tank = FindAnyObjectByType<TutorialPlayer>();
            if (tank != null) player = tank.transform;
        }
        if (level != null && level.TryGetRoomIndex(GetComponent<TutorialTarget>(), out int index))
            roomIndex = index;
    }

    private void OnDisable() => LoseVisualContact();

    private void LoseVisualContact()
    {
        hasVisualContact = false;
        burstShotsRemaining = 0;
    }

    private bool CanSeePlayer()
    {
        Vector2 origin = ownCollider != null ? (Vector2)ownCollider.bounds.center : (Vector2)transform.position;
        Vector2 aim = (Vector2)player.position - origin;
        if (aim.sqrMagnitude > sightRange * sightRange ||
            Vector2.Angle(turret.up, aim) > sightAngle * 0.5f) return false;
        // A visible side of the tank is a valid target even if cover hides its centre.
        float targetRadius=firstLevel!=null ? firstLevel.playerRadius : .4f;
        Vector2 across=new Vector2(-aim.y,aim.x).normalized*targetRadius*.72f;
        if(HasClearShot(origin,player.position)){visibleAim=player.position;return true;}
        if(HasClearShot(origin,(Vector2)player.position+across)){visibleAim=(Vector2)player.position+across;return true;}
        if(HasClearShot(origin,(Vector2)player.position-across)){visibleAim=(Vector2)player.position-across;return true;}
        return false;
    }

    private bool HasClearShot(Vector2 origin,Vector2 destination)
    {
        Vector2 aim=destination-origin;
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(Physics2D.AllLayers);
        filter.useTriggers = false;
        // Check the ball's width too, so a narrow slit doesn't permit a blocked shot.
        int count = Physics2D.CircleCast(origin, firstLevel!=null ? .035f : bulletPrefab.Radius, aim.normalized,
            filter, visibilityHits, aim.magnitude);
        for (int i = 0; i < count; i++)
        {
            Collider2D collider = visibilityHits[i].collider;
            if (collider == ownCollider || collider.GetComponentInParent<TutorialTarget>() != null ||
                collider.GetComponentInParent<BallPassBarrier>() != null) continue;
            return collider.GetComponentInParent<TutorialPlayer>() != null;
        }
        return firstLevel!=null;
    }

    private void Update()
    {
        bool active = firstLevel!=null ? firstLevel.CanPlay && firstLevel.CurrentRoom==roomIndex :
            level!=null && level.CurrentRoom==roomIndex && !level.Finished;
        if (!active || player == null || turret == null || muzzle == null || (firstLevel==null && bulletPrefab==null))
        {
            LoseVisualContact();
            return;
        }
        // Test visibility before aiming, including while the weapon is on cooldown.
        // Losing sight freezes the turret and cancels the current reaction period.
        if (!CanSeePlayer())
        {
            LoseVisualContact();
            return;
        }
        if (!hasVisualContact)
        {
            hasVisualContact = true;
            // Reacquisition always grants a full reaction delay without shortening shot cooldown.
            nextShot = Mathf.Max(nextShot, Time.time + firstShotDelay + reactionOffset);
        }
        Vector2 aim = visibleAim - (Vector2)turret.position;
        turret.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg - 90f);
        if (Time.timeScale <= 0f) return;
        if (burstShotsRemaining > 0)
        {
            if (Time.time < nextBurstShot) return;
        }
        else
        {
            if (Time.time < nextShot) return;
            burstShotsRemaining = Mathf.Max(1, shotsPerBurst);
        }
        nextShot = Time.time + shotInterval;
        Fire(muzzle);
        if (additionalMuzzles != null)
            foreach (Transform extraMuzzle in additionalMuzzles)
                if (extraMuzzle != null) Fire(extraMuzzle);
        burstShotsRemaining--;
        nextBurstShot = Time.time + Mathf.Max(.01f, burstShotInterval);
    }

    private void Fire(Transform barrelMuzzle)
    {
        if(firstLevel!=null)
        {
            Vector2 origin=turret.position;
            var shot=FirstLevelShot.Create(origin,barrelMuzzle.up,firstLevelBall,firstLevel);
            shot.SetEnemyShooter(GetComponent<TutorialTarget>());
            shot.Step(Vector2.Distance(origin,barrelMuzzle.position));
            return;
        }
        Bullet ball = Instantiate(bulletPrefab, barrelMuzzle.position, barrelMuzzle.rotation);
        ball.gameObject.SetActive(true);
        ball.Launch(barrelMuzzle.up, ownCollider, true);
    }
}
