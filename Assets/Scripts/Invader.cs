using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class Invader : MonoBehaviour
{
    [Header("Animation")]
    [Tooltip("Sprites to cycle through for animation.")]
    public Sprite[] animationSprites = new Sprite[0];
    [Tooltip("Seconds per animation frame.")]
    public float animationTime = 1f;
    [Tooltip("Points awarded when this invader is destroyed.")]
    public int score = 10;

    [Header("Health")]
    [Tooltip("Maximum health of this invader.")]
    public int maxHealth = 1;
    private int currentHealth;

    [Header("Attack Type")]
    [Tooltip("Determines what kind of attack pattern this invader uses.")]
    public InvaderAttackType attackType = InvaderAttackType.Normal;
    
    [Header("Attack Settings")]
    [Tooltip("Projectile prefab this invader will fire.")]
    public Projectile missilePrefab;
    [Tooltip("Secondary projectile for multi-shot attacks.")]
    public Projectile secondaryMissilePrefab;
    [Tooltip("Seconds between shots.")]
    public float shootCooldown = 1f;
    [Tooltip("Number of projectiles fired in burst (for Strong invaders).")]
    public int burstCount = 3;
    [Tooltip("Time between projectiles in a burst.")]
    public float burstDelay = 0.2f;
    [Tooltip("Spread angle for multi-directional shots (for Boss invaders).")]
    public float spreadAngle = 45f;
    [Tooltip("Size of this invader in tiles (1 for normal, 2 for strong, 3 for boss).")]
    public int invaderSize = 1;
    [Tooltip("If true, this invader attacks independently instead of through the Invaders manager.")]
    public bool independentAttacker = false;
    
    private float lastShotTime = Mathf.NegativeInfinity;
    private SpriteRenderer spriteRenderer;
    private int animationFrame;
    private bool isBursting = false;

    public enum InvaderAttackType
    {
        Normal,    // Single straight shot
        Strong,    // Burst fire (multiple shots in sequence)
        Boss       // Multi-directional spread shot
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = (animationSprites.Length > 0)
                                    ? animationSprites[0]
                                    : null;
        currentHealth = maxHealth;
    }

    private void Start()
    {
        if (animationSprites.Length > 1)
        {
            InvokeRepeating(nameof(AnimateSprite), animationTime, animationTime);
        }
        
        // Start independent attacking for special invaders
        if (independentAttacker && missilePrefab != null)
        {
            // Add some random delay so they don't all fire at once
            float initialDelay = Random.Range(0f, shootCooldown);
            InvokeRepeating(nameof(IndependentAttack), initialDelay, shootCooldown);
        }
    }
    
    /// <summary>
    /// Independent attack method for special invaders (Strong and Boss types).
    /// Called automatically on a timer instead of through the Invaders manager.
    /// </summary>
    private void IndependentAttack()
    {
        // Only attack if the invader is still active and not currently bursting
        if (!gameObject.activeInHierarchy || isBursting)
            return;
            
        // Calculate attack position
        Vector3 attackPosition = transform.position + Vector3.down * 0.5f;
        Quaternion attackRotation = Quaternion.identity;
        
        // Execute the attack based on type
        switch (attackType)
        {
            case InvaderAttackType.Strong:
                StartCoroutine(FireBurstShot(attackPosition, attackRotation));
                break;
                
            case InvaderAttackType.Boss:
                FireSpreadShot(attackPosition, attackRotation);
                break;
                
            case InvaderAttackType.Normal:
                // Normal invaders shouldn't use independent attacking, but just in case
                FireNormalShot(attackPosition, attackRotation);
                break;
        }
    }

    private void AnimateSprite()
    {
        if (animationSprites.Length == 0)
            return;

        animationFrame++;
        if (animationFrame >= animationSprites.Length)
            animationFrame = 0;

        spriteRenderer.sprite = animationSprites[animationFrame];
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        int laserLayer = LayerMask.NameToLayer("Laser");
        int boundaryLayer = LayerMask.NameToLayer("Boundary");

        if (other.gameObject.layer == laserLayer)
        {
            Destroy(other.gameObject);
            TakeDamage(1);
        }
        else if (other.gameObject.layer == boundaryLayer)
        {
            GameManager.Instance.OnBoundaryReached();
        }
    }

    /// <summary>
    /// Attempts to fire based on the invader's attack type.
    /// </summary>
    public void TryShoot(Vector3 position, Quaternion rotation)
    {
        if (missilePrefab == null || isBursting)
            return;

        if (Time.time - lastShotTime < shootCooldown)
            return;

        lastShotTime = Time.time;

        switch (attackType)
        {
            case InvaderAttackType.Normal:
                FireNormalShot(position, rotation);
                break;
                
            case InvaderAttackType.Strong:
                StartCoroutine(FireBurstShot(position, rotation));
                break;
                
            case InvaderAttackType.Boss:
                FireSpreadShot(position, rotation);
                break;
        }
    }

    /// <summary>
    /// Normal attack: Single projectile straight down.
    /// </summary>
    private void FireNormalShot(Vector3 position, Quaternion rotation)
    {
        // Calculate spawn position based on invader size
        float spawnOffset = (invaderSize * 0.5f) + 0.3f; // Half the invader size plus small buffer
        Vector3 spawnPos = position + Vector3.down * spawnOffset;
        Instantiate(missilePrefab, spawnPos, rotation);
    }

    /// <summary>
    /// Strong attack: Burst of multiple projectiles fired in sequence.
    /// </summary>
    private System.Collections.IEnumerator FireBurstShot(Vector3 position, Quaternion rotation)
    {
        isBursting = true;
        float spawnOffset = (invaderSize * 0.5f) + 0.3f;
        
        for (int i = 0; i < burstCount; i++)
        {
            if (this != null && gameObject.activeInHierarchy)
            {
                // Use secondary missile if available, otherwise use primary
                Projectile projectileToUse = secondaryMissilePrefab != null ? secondaryMissilePrefab : missilePrefab;
                Vector3 spawnPos = transform.position + Vector3.down * spawnOffset;
                Instantiate(projectileToUse, spawnPos, rotation);
            }
            
            if (i < burstCount - 1) // Don't wait after the last shot
            {
                yield return new WaitForSeconds(burstDelay);
            }
        }
        
        isBursting = false;
    }

    /// <summary>
    /// Boss attack: Multiple projectiles fired in different directions.
    /// </summary>
    private void FireSpreadShot(Vector3 position, Quaternion rotation)
    {
        // Calculate spawn position based on invader size
        float spawnOffset = (invaderSize * 0.5f) + 0.3f;
        Vector3 baseSpawnPos = position + Vector3.down * spawnOffset;
        
        // For larger invaders, spread the projectiles across their width
        float sideOffset = (invaderSize - 1) * 0.5f;
        
        // Fire center shot
        Instantiate(missilePrefab, baseSpawnPos, rotation);
        
        // Fire angled shots
        float halfSpread = spreadAngle / 2f;
        
        // Left angled shot
        Quaternion leftRotation = rotation * Quaternion.Euler(0, 0, halfSpread);
        Projectile leftProjectile = Instantiate(missilePrefab, baseSpawnPos + Vector3.left * sideOffset, leftRotation);
        leftProjectile.direction = Quaternion.Euler(0, 0, halfSpread) * Vector3.down;
        
        // Right angled shot
        Quaternion rightRotation = rotation * Quaternion.Euler(0, 0, -halfSpread);
        Projectile rightProjectile = Instantiate(missilePrefab, baseSpawnPos + Vector3.right * sideOffset, rightRotation);
        rightProjectile.direction = Quaternion.Euler(0, 0, -halfSpread) * Vector3.down;
        
        // Optional: Fire additional shots if secondary missile is assigned
        if (secondaryMissilePrefab != null)
        {
            // Fire wider spread shots
            float wideSpread = spreadAngle;
            float wideOffset = sideOffset + 0.5f;
            
            Quaternion wideLeftRotation = rotation * Quaternion.Euler(0, 0, wideSpread);
            Projectile wideLeftProjectile = Instantiate(secondaryMissilePrefab, baseSpawnPos + Vector3.left * wideOffset, wideLeftRotation);
            wideLeftProjectile.direction = Quaternion.Euler(0, 0, wideSpread) * Vector3.down;
            
            Quaternion wideRightRotation = rotation * Quaternion.Euler(0, 0, -wideSpread);
            Projectile wideRightProjectile = Instantiate(secondaryMissilePrefab, baseSpawnPos + Vector3.right * wideOffset, wideRightRotation);
            wideRightProjectile.direction = Quaternion.Euler(0, 0, -wideSpread) * Vector3.down;
        }
    }

    public void TakeDamagePublic(int amount)
    {
        TakeDamage(amount);
    }

    private void TakeDamage(int amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // Stop any ongoing burst firing
        StopAllCoroutines();
        isBursting = false;
        
        GameManager.Instance.OnInvaderKilled(this);
        Destroy(gameObject);
    }
}
