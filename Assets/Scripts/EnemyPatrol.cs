using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(TutorialTarget))]
public class EnemyPatrol : MonoBehaviour
{
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField, Min(0f)] private float speed = 2f;
    [SerializeField, Min(0f)] private float pauseAtEnds = .3f;
    [Tooltip("Rotate only the hull visual, leaving the turret's aim independent.")]
    [SerializeField] private Transform hull;
    [SerializeField, Min(0f)] private float turnSpeed = 540f;
    [SerializeField] private TutorialLevel level;
    [SerializeField] private int roomIndex;

    private Rigidbody2D body;
    private TutorialTarget target;
    private bool movingToB = true;
    private float pauseRemaining;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        target = GetComponent<TutorialTarget>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void Start()
    {
        if (level == null) level = FindAnyObjectByType<TutorialLevel>();
        if (level != null && level.TryGetRoomIndex(target, out int index))
            roomIndex = index;
    }

    private void FixedUpdate()
    {
        if (pointA == null || pointB == null || target.IsDestroyed ||
            (level != null && (level.Finished || level.CurrentRoom != roomIndex))) return;

        if (pauseRemaining > 0f)
        {
            pauseRemaining = Mathf.Max(0f, pauseRemaining - Time.fixedDeltaTime);
            return;
        }

        Vector2 destination = movingToB ? pointB.position : pointA.position;
        Vector2 direction = destination - body.position;
        if (direction.sqrMagnitude <= .000001f)
        {
            movingToB = !movingToB;
            pauseRemaining = pauseAtEnds;
            return;
        }

        body.MovePosition(Vector2.MoveTowards(body.position, destination, speed * Time.fixedDeltaTime));
        if (hull != null)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            hull.rotation = Quaternion.RotateTowards(hull.rotation,
                Quaternion.Euler(0f, 0f, angle), turnSpeed * Time.fixedDeltaTime);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (pointA == null || pointB == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(pointA.position, pointB.position);
        Gizmos.DrawWireSphere(pointA.position, .2f);
        Gizmos.DrawWireSphere(pointB.position, .2f);
    }
}
