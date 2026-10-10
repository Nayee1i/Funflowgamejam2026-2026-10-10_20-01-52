using UnityEngine;

// Sweeping the whole travel distance prevents fast shots from crossing thin walls.
public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float radius = 0.15f;
    [SerializeField] private float lifetime = 8f;

    private Vector2 direction;
    private bool bounced;
    private float age;
    private bool hostile;
    private bool shieldBreaking;
    private Collider2D owner;
    private readonly RaycastHit2D[] hits = new RaycastHit2D[32];
    private const float Separation = 0.002f;
    public float Radius => radius;

    public void Launch(Vector2 aim, Collider2D shooter, bool enemyShot = false)
    {
        direction = aim.normalized;
        bounced = false;
        age = 0f;
        hostile = enemyShot;
        shieldBreaking = false;
        owner = shooter;
        if (hostile) GetComponent<SpriteRenderer>().color = new Color(1f, 0.3f, 0.25f);
        // Place the shot before any obstacle between the tank and its muzzle.
        // This works for arbitrary tutorial rooms, rather than fixed arena bounds.
        if (shooter != null)
        {
            Vector2 origin = shooter.bounds.center;
            Vector2 offset = (Vector2)transform.position - origin;
            if (offset.sqrMagnitude > 0f)
            {
                RaycastHit2D obstruction = Cast(origin, offset.normalized, offset.magnitude);
                if (obstruction.collider != null)
                    transform.position = obstruction.centroid + obstruction.normal * Separation;
            }
        }
    }

    private void FixedUpdate()
    {
        age += Time.fixedDeltaTime;
        if (age >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        float remaining = speed * Time.fixedDeltaTime;
        // Ordinary walls absorb shots. Only a blue mirror permits one reflection.
        for (int contact = 0; contact < 2 && remaining > 0f; contact++)
        {
            Vector2 position = transform.position;
            RaycastHit2D hit = Cast(position, direction, remaining);
            if (hit.collider == null)
            {
                transform.position = position + direction * remaining;
                return;
            }

            TutorialPlayer player = hit.collider.GetComponentInParent<TutorialPlayer>();
            if (hostile && player != null)
            {
                player.Hit();
                Destroy(gameObject);
                return;
            }
            TutorialTarget target = hit.collider.GetComponentInParent<TutorialTarget>();
            if (!hostile && target != null)
            {
                target.Hit(bounced, shieldBreaking);
                Destroy(gameObject);
                return;
            }
            bool red = hit.collider.GetComponentInParent<RedMirror>() != null;
            if (bounced || (!red && hit.collider.GetComponentInParent<BlueMirror>() == null))
            {
                Destroy(gameObject);
                return;
            }

            transform.position = hit.centroid + hit.normal * Separation;
            remaining -= hit.distance;
            direction = Vector2.Reflect(direction, hit.normal).normalized;
            bounced = true;
            shieldBreaking = red;
            GetComponent<SpriteRenderer>().color = red ? new Color(1f, 0.15f, 0.2f) : new Color(1f, 0.48f, 0.22f);
        }
    }

    private RaycastHit2D Cast(Vector2 origin, Vector2 aim, float distance)
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(Physics2D.AllLayers);
        filter.useTriggers = false;
        int count = Physics2D.CircleCast(origin, radius, aim, filter, hits, distance);
        for (int i = 0; i < count; i++)
        {
            Collider2D collider = hits[i].collider;
            if (collider == owner) continue;
            if (collider.GetComponentInParent<BallPassBarrier>() != null) continue;
            if (!hostile && collider.GetComponentInParent<TutorialPlayer>() != null) continue;
            if (hostile && collider.GetComponentInParent<TutorialTarget>() != null) continue;
            return hits[i];
        }
        return default;
    }
}
