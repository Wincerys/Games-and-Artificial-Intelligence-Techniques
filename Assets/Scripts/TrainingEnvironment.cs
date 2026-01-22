using UnityEngine;
using Unity.MLAgents;

/// <summary>
/// Training environment controller that manages multiple training scenarios
/// and curriculum learning for the Space Invaders agent.
/// </summary>
public class TrainingEnvironment : MonoBehaviour
{
    [Header("Environment Settings")]
    [Tooltip("The ML-Agent that will be trained")]
    public SpaceInvadersAgent agent;
    
    [Tooltip("Reference to the invaders formation")]
    public Invaders invadersFormation;
    
    [Tooltip("Reference to the game manager")]
    public GameManager gameManager;
    
    [Tooltip("All player ship prefabs for variety")]
    public GameObject[] playerShipPrefabs;
    
    [Header("Training Variations")]
    [Tooltip("Randomize starting positions for variety")]
    public bool randomizeStartPositions = true;
    
    [Tooltip("Randomize ship types during training")]
    public bool randomizeShipTypes = true;
    
    [Tooltip("Add random invader formations")]
    public bool randomizeInvaderFormations = true;
    
    [Header("Curriculum Parameters")]
    [SerializeField] private float invaderSpeedMultiplier = 1f;
    [SerializeField] private float missileSpawnRateMultiplier = 1f;
    
    private Vector3 originalPlayerPosition;
    private Vector3 originalInvaderPosition;
    private float originalMissileSpawnRate;

    private void Awake()
    {
        // Store original values for reset
        if (agent.playerShip != null)
            originalPlayerPosition = agent.playerShip.transform.position;
            
        if (invadersFormation != null)
        {
            originalInvaderPosition = invadersFormation.transform.position;
            originalMissileSpawnRate = invadersFormation.missileSpawnRate;
        }
    }

    /// <summary>
    /// Called when the agent begins a new episode
    /// </summary>
    public void OnAgentEpisodeBegin()
    {
        ResetEnvironment();
        ApplyRandomizations();
        ApplyCurriculumSettings();
    }

    /// <summary>
    /// Reset the environment to initial state
    /// </summary>
    private void ResetEnvironment()
    {
        // Clear all projectiles
        ClearAllProjectiles();
        
        // Reset invaders
        if (invadersFormation != null)
        {
            invadersFormation.ResetInvaders();
            invadersFormation.transform.position = originalInvaderPosition;
        }
        
        // Reset bunkers
        Bunker[] bunkers = FindObjectsOfType<Bunker>();
        foreach (Bunker bunker in bunkers)
        {
            bunker.ResetBunker();
        }
        
        // Reset mystery ship
        MysteryShip mysteryShip = FindObjectOfType<MysteryShip>();
        if (mysteryShip != null)
        {
            mysteryShip.gameObject.SetActive(true);
        }
        
        // Reset player
        if (agent.playerShip != null)
        {
            agent.playerShip.transform.position = originalPlayerPosition;
            agent.playerShip.gameObject.SetActive(true);
            agent.playerShip.ResetPlayer();
        }
        
        // Reset game state
        if (gameManager != null)
        {
            // Reset score and lives through reflection or add public reset method
            ResetGameManagerState();
        }
    }

    /// <summary>
    /// Apply randomizations for training variety
    /// </summary>
    private void ApplyRandomizations()
    {
        // Randomize player starting position
        if (randomizeStartPositions && agent.playerShip != null)
        {
            Vector3 pos = agent.playerShip.transform.position;
            pos.x = Random.Range(-6f, 6f); // Adjust range based on your game bounds
            agent.playerShip.transform.position = pos;
        }
        
        // Randomize ship type
        if (randomizeShipTypes && playerShipPrefabs.Length > 0)
        {
            RandomizePlayerShip();
        }
        
        // Randomize invader formation slightly
        if (randomizeInvaderFormations && invadersFormation != null)
        {
            Vector3 pos = invadersFormation.transform.position;
            pos.x += Random.Range(-1f, 1f);
            pos.y += Random.Range(-0.5f, 0.5f);
            invadersFormation.transform.position = pos;
        }
    }

    /// <summary>
    /// Apply curriculum learning settings
    /// </summary>
    private void ApplyCurriculumSettings()
    {
        // Apply speed multiplier to invaders
        if (invadersFormation != null)
        {
            // You might need to modify the Invaders script to support speed multipliers
            // or use reflection to access private fields
            invadersFormation.missileSpawnRate = originalMissileSpawnRate * missileSpawnRateMultiplier;
        }
        
        // Apply other curriculum parameters as needed
        // This could include adjusting invader health, projectile speed, etc.
    }

    /// <summary>
    /// Randomize the player ship type for training variety
    /// </summary>
    private void RandomizePlayerShip()
    {
        if (playerShipPrefabs.Length == 0) return;
        
        // Destroy current player ship
        if (agent.playerShip != null)
        {
            Vector3 currentPos = agent.playerShip.transform.position;
            Destroy(agent.playerShip.gameObject);
            
            // Instantiate random ship type
            int randomIndex = Random.Range(0, playerShipPrefabs.Length);
            GameObject newShip = Instantiate(playerShipPrefabs[randomIndex], currentPos, Quaternion.identity);
            agent.playerShip = newShip.GetComponent<Player>();
        }
    }

    /// <summary>
    /// Clear all projectiles from the scene
    /// </summary>
    private void ClearAllProjectiles()
    {
        // Clear regular projectiles
        Projectile[] projectiles = FindObjectsOfType<Projectile>();
        foreach (Projectile projectile in projectiles)
        {
            Destroy(projectile.gameObject);
        }
        
        // Clear special projectiles
        ArcShot[] arcShots = FindObjectsOfType<ArcShot>();
        foreach (ArcShot arcShot in arcShots)
        {
            Destroy(arcShot.gameObject);
        }
        
        Rocket[] rockets = FindObjectsOfType<Rocket>();
        foreach (Rocket rocket in rockets)
        {
            Destroy(rocket.gameObject);
        }
        
        HomingMissile[] homingMissiles = FindObjectsOfType<HomingMissile>();
        foreach (HomingMissile missile in homingMissiles)
        {
            Destroy(missile.gameObject);
        }
        
        BossInvaderMissile[] bossMissiles = FindObjectsOfType<BossInvaderMissile>();
        foreach (BossInvaderMissile missile in bossMissiles)
        {
            Destroy(missile.gameObject);
        }
        
        StrongInvaderMissile[] strongMissiles = FindObjectsOfType<StrongInvaderMissile>();
        foreach (StrongInvaderMissile missile in strongMissiles)
        {
            Destroy(missile.gameObject);
        }
    }

    /// <summary>
    /// Reset GameManager state (you may need to modify GameManager to make this work)
    /// </summary>
    private void ResetGameManagerState()
    {
        if (gameManager == null) return;
        
        // Use reflection to reset private fields, or add public reset methods to GameManager
        var scoreField = typeof(GameManager).GetField("score", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var livesField = typeof(GameManager).GetField("lives", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
        if (scoreField != null)
            scoreField.SetValue(gameManager, 0);
            
        if (livesField != null)
            livesField.SetValue(gameManager, 3);
            
        // Update UI
        gameManager.GetType().GetMethod("SetScore", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.Invoke(gameManager, new object[] { 0 });
            
        gameManager.GetType().GetMethod("SetLives", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.Invoke(gameManager, new object[] { 3 });
    }

    /// <summary>
    /// Set curriculum parameters (called by ML-Agents during curriculum learning)
    /// </summary>
    public void SetCurriculumParameter(string parameterName, float value)
    {
        switch (parameterName)
        {
            case "invader_speed_multiplier":
                invaderSpeedMultiplier = value;
                break;
            case "missile_spawn_rate_multiplier":
                missileSpawnRateMultiplier = value;
                break;
            default:
                Debug.LogWarning($"Unknown curriculum parameter: {parameterName}");
                break;
        }
    }

    /// <summary>
    /// Get current performance metrics for curriculum progression
    /// </summary>
    public float GetPerformanceMetric(string metricName)
    {
        switch (metricName)
        {
            case "average_score":
                return gameManager != null ? gameManager.score : 0f;
            case "survival_time":
                return Time.time; // Or implement proper survival time tracking
            case "invaders_killed":
                int totalInvaders = invadersFormation != null ? 55 : 0; // Adjust based on your grid
                int aliveInvaders = invadersFormation != null ? invadersFormation.GetAliveCount() : 0;
                return totalInvaders - aliveInvaders;
            default:
                return 0f;
        }
    }

    private void OnValidate()
    {
        // Clamp multipliers to reasonable ranges
        invaderSpeedMultiplier = Mathf.Clamp(invaderSpeedMultiplier, 0.1f, 3f);
        missileSpawnRateMultiplier = Mathf.Clamp(missileSpawnRateMultiplier, 0.1f, 3f);
    }
}