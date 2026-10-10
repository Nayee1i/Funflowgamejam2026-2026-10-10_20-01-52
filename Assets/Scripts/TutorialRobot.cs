using UnityEngine;

public class TutorialRobot : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Transform turret;
    [SerializeField] private Transform muzzle;
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private TutorialLevel level;
    [SerializeField] private int roomIndex = 2;
    [SerializeField] private float shotInterval = 2f;
    [SerializeField] private float firstShotDelay = 1f;
    [SerializeField] private float sightRange = 16f;
    [Range(0f, 360f)]
    [SerializeField] private float sightAngle = 150f;
    private float nextShot;
    private bool activated;
    private Collider2D ownCollider;
    private readonly RaycastHit2D[] visibilityHits = new RaycastHit2D[32];

    private void Awake() => ownCollider = GetComponent<Collider2D>();

    private bool CanSeePlayer()
    {
        Vector2 origin = ownCollider != null ? (Vector2)ownCollider.bounds.center : (Vector2)transform.position;
        Vector2 aim = (Vector2)player.position - origin;
        if (aim.sqrMagnitude > sightRange * sightRange ||
            Vector2.Angle(turret.up, aim) > sightAngle * 0.5f) return false;
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(Physics2D.AllLayers);
        filter.useTriggers = false;
        // Check the ball's width too, so a narrow slit doesn't permit a blocked shot.
        int count = Physics2D.CircleCast(origin, bulletPrefab.Radius, aim.normalized,
            filter, visibilityHits, aim.magnitude);
        for (int i = 0; i < count; i++)
        {
            Collider2D collider = visibilityHits[i].collider;
            if (collider == ownCollider || collider.GetComponentInParent<TutorialTarget>() != null ||
                collider.GetComponentInParent<BallPassBarrier>() != null) continue;
            return collider.GetComponentInParent<TutorialPlayer>() != null;
        }
        return false;
    }

    private void Update()
    {
        if (level == null || level.CurrentRoom != roomIndex || level.Finished ||
            player == null || turret == null || muzzle == null || bulletPrefab == null) return;
        if (!activated)
        {
            if (!CanSeePlayer()) return;
            activated = true;
            nextShot = Time.time + firstShotDelay;
        }
        Vector2 aim = player.position - turret.position;
        turret.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg - 90f);
        if (Time.time < nextShot) return;
        if (!CanSeePlayer()) return;
        nextShot = Time.time + shotInterval;
        Bullet ball = Instantiate(bulletPrefab, muzzle.position, muzzle.rotation);
        ball.gameObject.SetActive(true);
        ball.Launch(muzzle.up, ownCollider, true);
    }
}
