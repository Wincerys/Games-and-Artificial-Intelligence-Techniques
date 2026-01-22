using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class Rocket : MonoBehaviour
{
    private BoxCollider2D boxCollider;
    public Vector3 direction = Vector3.up;
    public float speed = 15f; // Slower than normal shots
    
    [HideInInspector]
    public int damage = 3; // High damage
    
    [Header("Visual Effects")]
    public bool hasTrail = true;
    public Color trailColor = new Color(1f, 0.5f, 0f, 1f); // Orange color
    private TrailRenderer trail;
    
    [Header("Explosion")]
    public float explosionRadius = 1.5f;
    public bool piercesBunkers = true; // Rockets can punch through bunkers

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        
        // Add trail effect
        if (hasTrail)
        {
            trail = gameObject.AddComponent<TrailRenderer>();
            trail.time = 0.8f;
            trail.startWidth = 0.4f;
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
        
        // Clean up projectiles that go off-screen
        CheckAndDestroyIfOffScreen();
    }
    
    private void CheckAndDestroyIfOffScreen()
    {
        Vector3 screenBottomLeft = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, Camera.main.nearClipPlane));
        Vector3 screenTopRight = Camera.main.ViewportToWorldPoint(new Vector3(1, 1, Camera.main.nearClipPlane));
        
        float buffer = 2f;
        
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
        int invaderLayer = LayerMask.NameToLayer("Invader");
        int bunkerLayer = LayerMask.NameToLayer("Bunker");
        
        // Handle invader collisions with explosion damage
        if (other.gameObject.layer == invaderLayer)
        {
            ExplodeAndDamageInRadius();
            return;
        }

        // Handle bunker collisions
        if (other.gameObject.layer == bunkerLayer)
        {
            Bunker bunker = other.gameObject.GetComponent<Bunker>();
            if (bunker != null)
            {
                // If rocket pierces bunkers, just damage them and continue
                if (piercesBunkers)
                {
                    bunker.CheckCollision(boxCollider, transform.position);
                    // Don't destroy the rocket, let it continue
                }
                else
                {
                    // Normal collision - destroy rocket
                    if (bunker.CheckCollision(boxCollider, transform.position))
                    {
                        Destroy(gameObject);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Creates an explosion that damages all invaders within the explosion radius.
    /// </summary>
    private void ExplodeAndDamageInRadius()
    {
        // Find all invaders within explosion radius
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        
        foreach (Collider2D hitCollider in hitColliders)
        {
            if (hitCollider.gameObject.layer == LayerMask.NameToLayer("Invader"))
            {
                Invader invader = hitCollider.gameObject.GetComponent<Invader>();
                if (invader != null)
                {
                    invader.TakeDamagePublic(damage);
                }
            }
        }
        
        // TODO: Add explosion visual effect here
        // Instantiate(explosionEffect, transform.position, Quaternion.identity);
        
        Destroy(gameObject);
    }
    
    /// <summary>
    /// Draw the explosion radius in the scene view for debugging.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        // Unity doesn't have DrawWireCircle, so we'll draw a wire sphere instead
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
