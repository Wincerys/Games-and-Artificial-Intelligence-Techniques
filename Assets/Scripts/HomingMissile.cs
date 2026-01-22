using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class HomingMissile : MonoBehaviour
{
    private BoxCollider2D boxCollider;
    public Vector3 direction = Vector3.down;
    public float speed = 12f;
    public float homingStrength = 2f; // How aggressively it homes
    public float homingDuration = 3f; // How long it homes before going straight
    
    [HideInInspector]
    public int damage = 2;
    
    private Transform player;
    private float homingTimer;
    private bool isHoming = true;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        
        // Find the player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        
        homingTimer = homingDuration;
    }

    private void Update()
    {
        // Handle homing behavior
        if (isHoming && player != null && homingTimer > 0)
        {
            Vector3 targetDirection = (player.position - transform.position).normalized;
            direction = Vector3.Lerp(direction, targetDirection, homingStrength * Time.deltaTime).normalized;
            homingTimer -= Time.deltaTime;
        }
        else
        {
            isHoming = false;
        }

        transform.position += speed * Time.deltaTime * direction;
        
        // Rotate to face movement direction
        if (direction != Vector3.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90f;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
        
        // Destroy if off screen
        if (transform.position.y < -10f || transform.position.x < -15f || transform.position.x > 15f)
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