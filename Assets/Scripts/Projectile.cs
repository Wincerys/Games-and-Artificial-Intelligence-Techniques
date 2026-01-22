using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class Projectile : MonoBehaviour
{
    private BoxCollider2D boxCollider;
    public Vector3 direction = Vector3.up;
    public float speed = 20f;

    // ← Hide this from the Inspector; only set via script
    [HideInInspector]
    public int damage = 1;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
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

    private void HandleCollision(Collider2D other)
    {
        // Only handle specific layers that this projectile should interact with
        int invaderLayer = LayerMask.NameToLayer("Invader");
        int playerLayer = LayerMask.NameToLayer("Player");
        int bunkerLayer = LayerMask.NameToLayer("Bunker");
        
        // Handle Invader collisions
        if (other.gameObject.layer == invaderLayer)
        {
            Invader inv = other.gameObject.GetComponent<Invader>();
            if (inv != null)
            {
                inv.TakeDamagePublic(damage);
                Destroy(gameObject);
                return;
            }
        }

        // Handle Player collisions - just destroy the projectile
        // Let the Player script handle the death logic
        if (other.gameObject.layer == playerLayer)
        {
            Destroy(gameObject);
            return;
        }

        // Handle Bunker collisions
        if (other.gameObject.layer == bunkerLayer)
        {
            Bunker bunker = other.gameObject.GetComponent<Bunker>();
            if (bunker == null || bunker.CheckCollision(boxCollider, transform.position))
            {
                Destroy(gameObject);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other) => HandleCollision(other);
}