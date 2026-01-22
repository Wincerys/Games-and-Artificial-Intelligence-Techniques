using System.Linq;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class MysteryShip : MonoBehaviour
{
    [Header("General Settings")]
    [Tooltip("Seconds between each appearance/despawn.")]
    public float cycleTime = 30f;
    [Tooltip("Number of cycles to wait before the very first spawn.")]
    public int initialDelayCycles = 2;
    [Tooltip("Vertical offset above the highest invader formation.")]
    public float spawnOffsetY = 2f;
    [Tooltip("Points awarded when this mystery ship is shot.")]
    public int score = 300;

    [Header("Movement")]
    [Tooltip("Horizontal patrol speed (units/sec).")]
    public float speed = 5f;

    [Header("Dodge Settings")]
    [Tooltip("Radius within which to detect incoming player lasers.")]
    public float detectionRadius = 2f;
    [Range(0f,1f)]
    [Tooltip("Chance (0–1) to successfully dodge a detected shot.")]
    public float dodgeSuccessRate = 0.6f;
    [Tooltip("Vertical distance to move when dodging.")]
    public float dodgeDistance = 1f;

    // components & state
    private BoxCollider2D boxCollider;
    private Rigidbody2D     rb;
    private SpriteRenderer  spriteRenderer;
    private bool spawned     = false;
    private float leftBound, rightBound, spawnY;
    private int   moveDir    = -1;

    private void Awake()
    {
        // cache components
        boxCollider      = GetComponent<BoxCollider2D>();
        rb               = GetComponent<Rigidbody2D>();
        spriteRenderer   = GetComponent<SpriteRenderer>();

        boxCollider.isTrigger = true;
        rb.isKinematic        = true;

        // hide until first spawn
        spriteRenderer.enabled = false;
        boxCollider.enabled    = false;

        // compute horizontal world bounds
        var cam   = Camera.main;
        var left  = cam.ViewportToWorldPoint(new Vector3(0f,0f,cam.nearClipPlane));
        var right = cam.ViewportToWorldPoint(new Vector3(1f,0f,cam.nearClipPlane));
        leftBound  = left.x  - 1f;
        rightBound = right.x + 1f;
    }

    private void Start()
    {
        // schedule first spawn after N cycles
        float initialDelay = cycleTime * initialDelayCycles;
        Invoke(nameof(Spawn), initialDelay);
    }

    private void Update()
    {
        if (!spawned) return;

        // horizontal patrol
        Vector3 pos = transform.position;
        pos.x += speed * moveDir * Time.deltaTime;
        if (pos.x > rightBound) { pos.x = rightBound; moveDir = -1; }
        if (pos.x < leftBound)  { pos.x = leftBound;  moveDir =  1; }
        transform.position = pos;

        // dodge incoming lasers
        TryDodge();
    }

    private void Spawn()
    {
        // figure out Y: 5 units above highest alive invader
        var invs = FindObjectsOfType<Invader>()
                   .Where(i => i.gameObject.activeInHierarchy);
        if (invs.Any())
        {
            float topY = invs.Max(i => i.transform.position.y);
            spawnY = topY + spawnOffsetY;
        }
        else
        {
            spawnY = Camera.main.ViewportToWorldPoint(new Vector3(0f,0.9f,0f)).y;
        }

        // change to spawn at top‐center:
        float spawnX = Camera.main.ViewportToWorldPoint(
            new Vector3(0.5f, 0f, Camera.main.nearClipPlane)
        ).x;
        transform.position = new Vector3(spawnX, spawnY, 0f);

        // show it & begin moving left
        spriteRenderer.enabled = true;
        boxCollider.enabled    = true;
        spawned                = true;
        moveDir                = -1;

        // schedule the next despawn
        Invoke(nameof(Despawn), cycleTime);
    }

    private void Despawn()
    {
        spawned = false;

        // hide visuals & collider
        spriteRenderer.enabled = false;
        boxCollider.enabled    = false;

        // schedule the next respawn after cycleTime
        Invoke(nameof(Spawn), cycleTime);
    }

    private void TryDodge()
    {
        int layerMask = 1 << LayerMask.NameToLayer("PlayerLaser");
        var hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius, layerMask);
        if (hits.Length == 0 || Random.value > dodgeSuccessRate) 
            return;

        // dodge vertically away from the closest
        var closest = hits.Aggregate((a,b) =>
            (a.transform.position - transform.position).sqrMagnitude 
          < (b.transform.position - transform.position).sqrMagnitude ? a : b);
        float dirY = Mathf.Sign(transform.position.y - closest.transform.position.y);

        Vector3 pos = transform.position;
        pos.y += dirY * dodgeDistance;
        transform.position = pos;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // only react if we’re currently spawned
        if (!spawned) return;

        int laserL   = LayerMask.NameToLayer("Laser");
        int pLaserL  = LayerMask.NameToLayer("PlayerLaser");
        int otherLay = other.gameObject.layer;

        if (otherLay == laserL || otherLay == pLaserL)
        {
            // got hit: award points & hide
            spawned = false;
            spriteRenderer.enabled = false;
            boxCollider.enabled    = false;
            GameManager.Instance.OnMysteryShipKilled(this);

            // schedule next respawn
            Invoke(nameof(Spawn), cycleTime);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}