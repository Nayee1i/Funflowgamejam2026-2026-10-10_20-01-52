using System.Collections.Generic;
using UnityEngine;

// Fixed local routes, planned once on room entry; no chasing or moving level geometry.
[DefaultExecutionOrder(-20)]
public class RobotCoverPatrol : MonoBehaviour
{
    public float speed = 1.15f;
    public float hideSeconds = 1.4f;
    public float exposeSeconds = 2.8f;
    public bool HasCover { get; private set; }
    public bool CanAttack => ready && holding && exposed;
    public IReadOnlyList<Vector2> Route => route;
    public Vector2 HidePoint { get; private set; }
    public Vector2 PeekPoint { get; private set; }
    private FirstLevelRooms level;
    private TutorialTarget target;
    private Collider2D body;
    private int room;
    private float radius, wait, blockedTime;
    private Vector2 centreOffset;
    private bool ready, holding, exposed;
    private readonly List<Vector2> route = new List<Vector2>();
    private int waypoint;
    private readonly RaycastHit2D[] hits = new RaycastHit2D[64];
    private readonly Collider2D[] overlaps = new Collider2D[64];
    private ContactFilter2D filter;

    public void Configure(FirstLevelRooms owner, int index, int ordinal)
    {
        level = owner; room = index;
        target = GetComponent<TutorialTarget>(); body = GetComponent<Collider2D>();
        radius = body is CircleCollider2D circle
            ? circle.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y))
            : Mathf.Max(body.bounds.extents.x, body.bounds.extents.y);
        radius += .04f;
        centreOffset = body is CircleCollider2D centredCircle
            ? (Vector2)transform.TransformVector(centredCircle.offset) : (Vector2)body.bounds.center - (Vector2)transform.position;
        hideSeconds = 1.2f + ordinal % 3 * .35f;
        speed = owner.GetComponent<ThirdLevelCastle>() != null ? 1.3f : 1.05f;
        filter = new ContactFilter2D(); filter.SetLayerMask(Physics2D.AllLayers); filter.useTriggers = false;
    }

    private bool Obstacle(Collider2D collider, bool includeRobots)
    {
        if (collider == null || collider == body || collider.isTrigger) return false;
        if (collider.GetComponentInParent<TutorialPlayer>() != null) return includeRobots;
        if (collider.GetComponentInParent<TutorialTarget>() != null) return includeRobots;
        return true;
    }

    private bool Clear(Vector2 a, Vector2 b, float width, bool includeRobots = false)
    {
        Vector2 delta = b - a;
        if (delta.sqrMagnitude < .000001f) return true;
        int count = Physics2D.CircleCast(a + centreOffset, width, delta.normalized, filter, hits, delta.magnitude);
        for (int i = 0; i < count; i++) if (Obstacle(hits[i].collider, includeRobots)) return false;
        return true;
    }

    private bool Free(Vector2 point)
    {
        int count = Physics2D.OverlapCircle(point + centreOffset, radius, filter, overlaps);
        for (int i = 0; i < count; i++) if (Obstacle(overlaps[i], true)) return false;
        return true;
    }

    public void Plan()
    {
        Physics2D.SyncTransforms();
        const float step = .32f;
        const int extent = 12, side = extent * 2 + 1;
        Vector2 start = transform.position;
        var points = new Vector2[side * side];
        var parents = new int[points.Length];
        for (int i = 0; i < parents.Length; i++) parents[i] = -2;
        int origin = extent * side + extent;
        points[origin] = start; parents[origin] = -1;
        var queue = new Queue<int>(); queue.Enqueue(origin);
        var reachable = new List<int>();
        int[] dx = { 1, -1, 0, 0 }, dy = { 0, 0, 1, -1 };
        while (queue.Count > 0)
        {
            int current = queue.Dequeue(); reachable.Add(current);
            int x = current % side, y = current / side;
            for (int d = 0; d < 4; d++)
            {
                int nx = x + dx[d], ny = y + dy[d];
                if (nx < 0 || ny < 0 || nx >= side || ny >= side) continue;
                int next = ny * side + nx;
                if (parents[next] != -2) continue;
                Vector2 p = start + new Vector2(nx - extent, ny - extent) * step;
                // Keep routes away from exit triggers and within the map's playing floor.
                if (Mathf.Abs(p.x) > 6.9f || p.y < -3.1f || p.y > 3.15f ||
                    !Free(p) || !Clear(points[current], p, radius, true)) continue;
                points[next] = p; parents[next] = current; queue.Enqueue(next);
            }
        }
        Vector2 reference = new Vector2(-level.spawnX, level.doorwayY);
        var visible = new bool[points.Length];
        foreach (int index in reachable) visible[index] = Clear(points[index], reference - centreOffset, .04f);
        int hide = -1, peek = -1; float best = float.PositiveInfinity;
        foreach (int h in reachable)
        {
            if (visible[h]) continue;
            foreach (int p in reachable)
            {
                if (!visible[p]) continue;
                float distance = Vector2.Distance(points[h], points[p]);
                if (distance < .65f || distance > 2.8f) continue;
                float score = Vector2.Distance(start, points[h]) + distance * .7f;
                if (score >= best || !Clear(points[h], points[p], radius, true)) continue;
                best = score; hide = h; peek = p;
            }
        }
        HasCover = hide >= 0;
        if (!HasCover)
        {
            hide = origin;
            // A straight, reachable fallback patrol rather than crossing obstacles.
            float longest = 0;
            foreach (int p in reachable)
            {
                float distance = Vector2.Distance(start, points[p]);
                if (distance < 1f || distance > 2f || distance <= longest || !Clear(start, points[p], radius, true)) continue;
                longest = distance; peek = p;
            }
        }
        HidePoint = points[hide]; PeekPoint = peek >= 0 ? points[peek] : HidePoint;
        route.Clear();
        for (int index = hide; index >= 0; index = parents[index]) route.Add(points[index]);
        route.Reverse(); waypoint = 0; exposed = false; holding = false;
        ready = true; blockedTime = 0;
    }

    public void NotifyVolley()
    {
        if (CanAttack) wait = Mathf.Min(wait, .45f);
    }

    private void Update()
    {
        if (level == null || !level.CanPlay || level.CurrentRoom != room || target.IsDestroyed) return;
        if (!ready) Plan();
        Tick(Time.deltaTime);
    }

    public void Tick(float deltaTime)
    {
        if (!ready || deltaTime <= 0) return;
        if (holding)
        {
            wait -= deltaTime;
            if (wait > 0) return;
            exposed = !exposed; holding = false;
            route.Clear(); route.Add(exposed ? PeekPoint : HidePoint); waypoint = 0;
        }
        Vector2 position = transform.position;
        Vector2 destination = route[waypoint];
        if (Vector2.Distance(position, destination) < .025f)
        {
            if (++waypoint < route.Count) return;
            holding = true; wait = exposed ? exposeSeconds : hideSeconds; return;
        }
        Vector2 next = Vector2.MoveTowards(position, destination, speed * deltaTime);
        // Other robots and the player can temporarily block a route, never be pushed through.
        bool playerBlocks = Vector2.Distance(next + centreOffset, level.player.position) < radius + level.playerRadius + .03f;
        if (playerBlocks || !Clear(position, next, radius, true))
        {
            blockedTime += deltaTime;
            if (blockedTime > 1.5f)
            {
                // Return along the same validated route when another actor blocks the peek.
                if (exposed && Clear(position, HidePoint, radius, true))
                { exposed = false; route.Clear(); route.Add(HidePoint); waypoint = 0; }
                blockedTime = 0;
            }
            return;
        }
        blockedTime = 0;
        transform.position = new Vector3(next.x, next.y, transform.position.z);
        Physics2D.SyncTransforms();
    }

    private void OnDrawGizmosSelected()
    {
        if (!ready) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(HidePoint, PeekPoint);
        Gizmos.DrawWireSphere(HidePoint, radius);
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(PeekPoint, radius);
        for (int i = 1; i < route.Count; i++) Gizmos.DrawLine(route[i - 1], route[i]);
    }
}
