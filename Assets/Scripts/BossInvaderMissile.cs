using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class BossInvaderMissile : MonoBehaviour
{
    private BoxCollider2D boxCollider;
    public Vector3 direction = Vector3.down;
    public float speed = 15f; // Slower but more intimidating
    
    [HideInInspector]
    public int damage = 3; // High damage
    
    [Header("Visual Effects")]
    public bool hasTrail = true;
    public Color trailColor = Color.red;
    private TrailRenderer trail;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        
        // Add trail effect if enabled
        if (hasTrail)
        {
            trail = gameObject.AddComponent<TrailRenderer>();
            trail.time = 0.5f;
            trail.startWidth = 0.3f;
            trail.endWidth = 0.1f;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            
            // Set trail color using gradient
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(trailColor, 0.0f), new GradientColorKey(trailColor, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
            );
            trail.colorGradient = gradient;
        }
    }

    private void Update()
    {
        transform.position += speed * Time.deltaTime * direction;
        
        // Rotate sprite to face movement direction
        if (direction != Vector3.zero)
        {
            float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(-angle, Vector3.forward);
        }
        
        // Clean up projectiles that go off-screen to prevent memory leaks
        CheckAndDestroyIfOffScreen();
    }
    
    private void CheckAndDestroyIfOffScreen()
    {
        // Get screen bounds in world coordinates
        Vector3 screenBottomLeft = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, Camera.main.nearClipPlane));
        Vector3 screenTopRight = Camera.main.ViewportToWorldPoint(new Vector3(1, 1, Camera.main.nearClipPlane));
        
        // Add some buffer distance beyond screen edges
        float buffer = 2f;
        
        // Check if projectile is completely off-screen
        if (transform.position.x < screenBottomLeft.x - buffer ||
            transform.position.x > screenTopRight.x + buffer ||
            transform.position.y < screenBottomLeft.y - buffer ||
            transform.position.y > screenTopRight.y + buffer)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleCollision(other);
    }

    private void HandleCollision(Collider2D other)
    {
        int playerLayer = LayerMask.NameToLayer("Player");
        int bunkerLayer = LayerMask.NameToLayer("Bunker");
        int invaderLayer = LayerMask.NameToLayer("Invader");
        
        // Ignore collisions with invaders (so missiles don't hit the invader that fired them)
        if (other.gameObject.layer == invaderLayer)
        {
            return;
        }
        
        if (other.gameObject.layer == playerLayer)
        {
            Destroy(gameObject);
            return;
        }

        if (other.gameObject.layer == bunkerLayer)
        {
            Bunker bunker = other.gameObject.GetComponent<Bunker>();
            if (bunker == null || bunker.CheckCollision(boxCollider, transform.position))
            {
                Destroy(gameObject);
            }
        }
    }
}
