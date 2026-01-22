using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class Player : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 5f;

    [Header("Ship Type")]
    [Tooltip("Determines the ship's attack pattern and behavior.")]
    public ShipType shipType = ShipType.Balanced;

    [Header("Basic Firing")]
    public Projectile laserPrefab;
    public float attackCooldown = 0.5f;

    [Header("Damage")]
    [Tooltip("How much damage each of this player's shots does.")]
    public int shotDamage = 1;

    [Header("Invincibility Settings")]
    [Tooltip("How long the player is invincible after respawning (in seconds)")]
    public float invincibilityDuration = 2.5f;
    [Tooltip("How fast the player blinks during invincibility")]
    public float blinkRate = 0.15f;

    [Header("Fast Ship - Arc Shot Settings")]
    [Tooltip("Projectile for the fast ship's arc shots.")]
    public Projectile arcShotPrefab;
    [Tooltip("Number of projectiles in the arc (odd numbers work best).")]
    public int arcShotCount = 3;
    [Tooltip("Total spread angle for the arc in degrees.")]
    public float arcSpreadAngle = 30f;

    [Header("Heavy Ship - Rocket Settings")]
    [Tooltip("Rocket projectile for the heavy ship.")]
    public Projectile rocketPrefab;
    [Tooltip("Number of rockets that can be fired before reloading.")]
    public int rocketMagazineSize = 2;
    [Tooltip("Time between individual rockets in a burst.")]
    public float rocketBurstDelay = 0.2f;
    [Tooltip("Reload time after emptying the magazine.")]
    public float rocketReloadTime = 2f;

    [Header("Parry")]
    [Tooltip("Press to start parry window.")]
    public KeyCode parryKey = KeyCode.P;
    [Tooltip("How many frames the parry stays active.")]
    public int parryFrames = 5;
    [Tooltip("Seconds between allowed parries.")]
    public float parryCooldown = 45f;

    [Header("Parry Visual Effects")]
    [Tooltip("Color the ship changes to during parry window")]
    public Color parryColor = Color.cyan;
    [Tooltip("How fast the parry color pulses")]
    public float parryPulseSpeed = 10f;

    [Header("Debug")]
    [Tooltip("When true, parry is always active without input.")]
    public bool debugParryAlwaysOn = false;

    // Exposed for ParryZone
    public bool IsParrying => isParrying;
    public int ShotDamage => shotDamage;

    private float lastShotTime = Mathf.NegativeInfinity;
    private Projectile currentLaser;
    private bool isDead = false;

    // Invincibility system
    private bool isInvincible = false;
    private float invincibilityTimer = 0f;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    // Heavy ship rocket tracking
    private int currentRocketCount;
    private bool isReloading = false;
    private float reloadStartTime;

    // Parry tracking
    private bool isParrying = false;
    private int parryCounter = 0;
    private float lastParryTime = Mathf.NegativeInfinity;

    private BoxCollider2D mainCollider;
    private Collider2D parryCollider;

    public SpaceInvadersAgent agent;

    public enum ShipType
    {
        Balanced,  // Normal single shot
        Fast,      // Arc shots (multiple projectiles in spread)
        Heavy      // Rockets with magazine system
    }

    private void Awake()
    {
        mainCollider = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        // Find the parry-zone trigger on your child
        parryCollider = GetComponentInChildren<CircleCollider2D>();

        if (parryCollider != null)
        {
            // Make the two colliders completely ignore each other
            Physics2D.IgnoreCollision(mainCollider, parryCollider, true);
        }
        else
        {
            Debug.LogError("Player: no ParryZone collider found in children!");
        }
    }

    private void Start()
    {
        if (shipType == ShipType.Heavy)
            currentRocketCount = rocketMagazineSize;

        if (agent == null)
            agent = FindObjectOfType<SpaceInvadersAgent>();
    }

    private void Update()
    {
        if (isDead) return;

        // Handle invincibility timer
        HandleInvincibility();

        // Debug toggle: always enable parry
        if (debugParryAlwaysOn && !isParrying)
        {
            isParrying = true;
            parryCounter = parryFrames;
            SetParryColor();
            Debug.Log("Debug: Parry always ON");
        }
        else
        {
            HandleParryInput();
        }

        if (isParrying)
        {
            parryCounter--;
            if (parryCounter <= 0)
            {
                isParrying = false;
                mainCollider.enabled = true;    // Re-enable hitbox
                
                // Restore original color (unless invincible)
                RestorePlayerColor();
                
                Debug.Log("Parry window ended, color restored");
            }
            else
            {
                // Update parry color pulsing effect
                UpdateParryColor();
            }
        }

        HandleMovement();
        HandleShooting();
    }

    private void HandleInvincibility()
    {
        if (isInvincible)
        {
            invincibilityTimer -= Time.deltaTime;
            
            // Handle blinking effect (only if not parrying)
            if (spriteRenderer != null && !isParrying)
            {
                float blinkPhase = Mathf.Sin(Time.time / blinkRate * Mathf.PI * 2f);
                float alpha = Mathf.Lerp(0.3f, 1f, (blinkPhase + 1f) / 2f);
                spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            }
            
            // End invincibility
            if (invincibilityTimer <= 0f)
            {
                EndInvincibility();
            }
        }
    }

    private void StartInvincibility()
    {
        isInvincible = true;
        invincibilityTimer = invincibilityDuration;
        Debug.Log($"Player invincibility started for {invincibilityDuration} seconds");
    }

    private void EndInvincibility()
    {
        isInvincible = false;
        invincibilityTimer = 0f;
        
        // Restore normal appearance (unless parrying)
        if (spriteRenderer != null && !isParrying)
        {
            spriteRenderer.color = originalColor;
        }
        
        Debug.Log("Player invincibility ended");
    }

    private void HandleParryInput()
    {
        float since = Time.time - lastParryTime;
        if (Input.GetKeyDown(parryKey) && !isParrying)
        {
            if (since < parryCooldown)
            {
                float rem = parryCooldown - since;
                Debug.Log($"Parry on cooldown: {rem:F1}s remaining");
                return;
            }
            // Start parry
            isParrying = true;
            parryCounter = parryFrames;
            lastParryTime = Time.time;
            Debug.Log($"Parry started: {parryCounter} frames");

            // Disable your hitbox so no missile can kill you
            mainCollider.enabled = false;
            
            // Change color to parry color
            SetParryColor();
        }
    }

    private void SetParryColor()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = parryColor;
            Debug.Log($"Player color changed to parry color: {parryColor}");
        }
    }

    private void UpdateParryColor()
    {
        if (spriteRenderer != null)
        {
            // Pulse the parry color for extra visibility
            float pulse = Mathf.Sin(Time.time * parryPulseSpeed) * 0.3f + 0.7f;
            Color pulsedColor = new Color(
                parryColor.r * pulse,
                parryColor.g * pulse, 
                parryColor.b * pulse,
                parryColor.a
            );
            spriteRenderer.color = pulsedColor;
        }
    }

    private void RestorePlayerColor()
    {
        if (spriteRenderer != null)
        {
            if (isInvincible)
            {
                // If still invincible, don't restore completely - let invincibility blinking take over
                Debug.Log("Player color: parry ended but still invincible, will continue blinking");
            }
            else
            {
                spriteRenderer.color = originalColor;
                Debug.Log("Player color restored to original");
            }
        }
    }

    private void HandleMovement()
    {
        Vector3 pos = transform.position;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            pos.x -= speed * Time.deltaTime;
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            pos.x += speed * Time.deltaTime;

        Vector3 leftEdge = Camera.main.ViewportToWorldPoint(Vector3.zero);
        Vector3 rightEdge = Camera.main.ViewportToWorldPoint(Vector3.right);
        pos.x = Mathf.Clamp(pos.x, leftEdge.x, rightEdge.x);

        transform.position = pos;
    }

    private void HandleShooting()
    {
        if (isReloading && Time.time - reloadStartTime >= rocketReloadTime)
        {
            isReloading = false;
            currentRocketCount = rocketMagazineSize;
        }

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            if (Time.time - lastShotTime >= attackCooldown)
            {
                switch (shipType)
                {
                    case ShipType.Balanced: FireBalancedShot(); break;
                    case ShipType.Fast:     FireArcShot();      break;
                    case ShipType.Heavy:    FireRocket();       break;
                }
            }
        }
    }

    private void FireBalancedShot()
    {
        if (currentLaser == null && laserPrefab != null)
        {
            currentLaser = Instantiate(laserPrefab, transform.position, Quaternion.identity);
            currentLaser.damage = shotDamage;
            lastShotTime = Time.time;
        }
    }

    private void FireArcShot()
    {
        if (arcShotPrefab == null) return;

        float startAngle = -arcSpreadAngle / 2f;
        float angleStep = arcShotCount > 1 ? arcSpreadAngle / (arcShotCount - 1) : 0f;

        for (int i = 0; i < arcShotCount; i++)
        {
            float currentAngle = startAngle + angleStep * i;
            Vector3 dir = Quaternion.Euler(0, 0, currentAngle) * Vector3.up;
            Vector3 spawnPos = transform.position + Vector3.right * (i - arcShotCount/2f) * 0.1f;
            Projectile proj = Instantiate(arcShotPrefab, spawnPos, Quaternion.identity);
            proj.direction = dir;
            proj.damage = shotDamage;
        }

        lastShotTime = Time.time;
    }

    private void FireRocket()
    {
        if (rocketPrefab == null || isReloading || currentRocketCount <= 0) return;

        Projectile rocket = Instantiate(rocketPrefab, transform.position, Quaternion.identity);
        rocket.damage = shotDamage;
        currentRocketCount--;
        lastShotTime = Time.time;

        if (currentRocketCount <= 0)
        {
            isReloading = true;
            reloadStartTime = Time.time;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Prevent damage if dead or invincible
        if (isDead || isInvincible) 
            return;

        int missileLayer  = LayerMask.NameToLayer("Missile");
        int invaderLayer  = LayerMask.NameToLayer("Invader");

        // If we're parrying, eat any incoming missile here
        if (other.gameObject.layer == missileLayer && isParrying)
        {
            Debug.Log("Player collider ignored a parried missile");
            Destroy(other.gameObject);

            if (agent != null)
            {
                agent.AddReward(agent.parrySuccessReward);
                Debug.Log($"Agent rewarded {agent.parrySuccessReward} for a successful parry!");
            }
            return;
        }

        // Otherwise if it's still a threat (missile or invader) you die
        bool isThreat = other.gameObject.layer == missileLayer
                    || other.gameObject.layer == invaderLayer;

        if (isThreat)
        {
            isDead = true;
            GetComponent<Collider2D>().enabled = false;
            
            // End any active invincibility (shouldn't happen, but just in case)
            EndInvincibility();
            
            
            GameManager.Instance.OnPlayerKilled(this);
        }
    }

    // Called by ParryZone when a missile is parried
    public void ConsumeParry()
    {
        isParrying = false;
        parryCounter = 0;
        mainCollider.enabled = true; // Re-enable hitbox

        // Restore original color (unless invincible)
        RestorePlayerColor();
        
        Debug.Log("Parry consumed: hitbox re-enabled, color restored");
    }

    public void StartParry()
    {
        float since = Time.time - lastParryTime;
        if (since < parryCooldown)
        {
            float rem = parryCooldown - since;
            Debug.Log($"(Agent) Parry on cooldown: {rem:F1}s remaining");
            return;
        }

        isParrying = true;
        parryCounter = parryFrames;
        lastParryTime = Time.time;
        mainCollider.enabled = false;
        
        // Change color to parry color
        SetParryColor();
        
        Debug.Log("Player: parry window opened by Agent");
    }

    // Called externally to reset state on respawn
    public void ResetPlayer()
    {
        isDead = false;
        GetComponent<Collider2D>().enabled = true;
        
        // Start invincibility period
        StartInvincibility();
        
        if (shipType == ShipType.Heavy)
        {
            currentRocketCount = rocketMagazineSize;
            isReloading = false;
        }
    }

    // For UI: Show rocket ammo or reload status
    public string GetAmmoStatus()
    {
        if (shipType == ShipType.Heavy)
        {
            if (isReloading)
            {
                float progress = (Time.time - reloadStartTime) / rocketReloadTime;
                return $"Reloading... {Mathf.CeilToInt((1f - progress) * rocketReloadTime)}s";
            }
            return $"Rockets: {currentRocketCount}/{rocketMagazineSize}";
        }
        return string.Empty;
    }

    // Public getter to check if player is currently invincible (useful for UI or other systems)
    public bool IsInvincible()
    {
        return isInvincible;
    }

    // Public method to get remaining invincibility time (useful for UI)
    public float GetInvincibilityTimeRemaining()
    {
        return isInvincible ? invincibilityTimer : 0f;
    }
}