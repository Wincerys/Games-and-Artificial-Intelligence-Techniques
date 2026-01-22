using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

/// <summary>
/// ML-Agents reinforcement learning agent for Space Invaders.
/// This agent learns to play the game by observing the environment and taking actions.
/// </summary>
public class SpaceInvadersAgent : Agent
{
    [Header("Agent References")]
    [Tooltip("The player ship this agent will control")]
    public Player playerShip;
    
    [Tooltip("The invaders formation")]
    public Invaders invadersFormation;
    
    [Tooltip("The mystery ship")]
    public MysteryShip mysteryShip;
    
    [Tooltip("All bunkers in the scene")]
    public Bunker[] bunkers;
    
    [Tooltip("The game manager")]
    public GameManager gameManager;
    
    [Header("Training Parameters")]
    [Tooltip("Reward for destroying an invader")]
    public float invaderKillReward = 1f;
    
    [Tooltip("Reward for destroying mystery ship")]
    public float mysteryShipReward = 3f;
    
    [Tooltip("Penalty for dying")]
    public float deathPenalty = -10f;
    
    [Tooltip("Small reward for surviving each frame")]
    public float survivalReward = 0.01f;
    
    [Tooltip("Penalty for shooting when laser already exists")]
    public float wasteShotPenalty = -0.1f;
    
    [Tooltip("Reward for clearing all invaders (winning)")]
    public float winReward = 50f;
    
    [Header("Observation Settings")]
    [Tooltip("Grid resolution for environmental observations")]
    public int observationGridWidth = 32;
    public int observationGridHeight = 24;
    
    [Tooltip("Camera bounds for normalizing positions")]
    public Camera gameCamera;
    
    // Internal state tracking
    private int previousScore = 0;
    private int previousLives = 3;
    private int previousInvaderCount = 0;
    private Vector3 cameraBottomLeft;
    private Vector3 cameraTopRight;
    private bool playerWasAlive = true;
    
    // Action tracking
    private float horizontalMovement = 0f;
    private bool shouldShoot = false;
    private float lastAgentShotTime = Mathf.NegativeInfinity; // Add cooldown tracking
    
    public float parrySuccessReward = 5f;

    [Header("Parry Cooldown")]
    [Tooltip("Seconds between allowed parries for Agent.")]
    public float parryCooldown = 45f;

    // track when the Agent last parried
    private float lastAgentParryTime = Mathf.NegativeInfinity;


    public override void Initialize()
    {
        // Get camera bounds for normalization
        if (gameCamera == null)
            gameCamera = Camera.main;

        UpdateCameraBounds();

        // Find components if not assigned
        // Note: playerShip will be found dynamically after ship selection

        if (invadersFormation == null)
            invadersFormation = FindObjectOfType<Invaders>();

        if (mysteryShip == null)
            mysteryShip = FindObjectOfType<MysteryShip>();

        if (bunkers == null || bunkers.Length == 0)
            bunkers = FindObjectsOfType<Bunker>();

        if (gameManager == null)
            gameManager = GameManager.Instance;
    }

    public override void OnEpisodeBegin()
    {
        Debug.Log("=== Episode Begin ===");
        
        // Ensure time scale is normal (in case previous episode ended with pause)
        Time.timeScale = 1f;
        
        // Reset game state first
        ResetGameState();
        
        // Force ship 2 selection for consistent training
        if (gameManager != null)
        {
            Debug.Log("Forcing ship selection...");
            gameManager.ForceShipSelection(2);
            
            // Wait a frame for ship to be instantiated, then find it
            StartCoroutine(FindPlayerShipNextFrame());
        }
        
        // Initialize tracking variables
        previousScore = 0;
        previousLives = 3;
        previousInvaderCount = invadersFormation != null ? invadersFormation.GetAliveCount() : 0;
        playerWasAlive = true;
        lastAgentShotTime = Mathf.NegativeInfinity; // Reset shot cooldown
        
        UpdateCameraBounds();
        
        Debug.Log($"Episode started: Lives={previousLives}, Invaders={previousInvaderCount}");
    }
    
    private System.Collections.IEnumerator FindPlayerShipNextFrame()
    {
        yield return null; // Wait one frame
        yield return null; // Wait another frame to be safe
        
        // Find the newly instantiated player ship
        if (playerShip == null || !playerShip.gameObject.activeInHierarchy)
        {
            playerShip = FindObjectOfType<Player>();
            if (playerShip != null)
            {
                Debug.Log($"Found player ship: {playerShip.name} at position: {playerShip.transform.position}");
            }
            else
            {
                Debug.LogWarning("Could not find player ship after ship selection!");
            }
        }
    }
    
    private Vector3 FindNearestThreat()
    {
        if (playerShip == null) return Vector3.zero;
        Vector3 playerPos = playerShip.transform.position;

        float bestDist = float.MaxValue;
        Vector3 bestPos = playerPos;

        foreach (var proj in FindObjectsOfType<Projectile>())
        {
            if (proj.direction.y < 0f)  // only enemy shots
            {
                float d = Vector3.Distance(playerPos, proj.transform.position);
                if (d < bestDist)
                {
                    bestDist = d;
                    bestPos = proj.transform.position;
                }
            }
        }
        return bestPos;
    }

    public override void CollectObservations(VectorSensor sensor)
    {

        // Player observations (4 values)
        if (playerShip != null && playerShip.gameObject.activeInHierarchy)
        {
            Vector2 normalizedPlayerPos = NormalizePosition(playerShip.transform.position);
            sensor.AddObservation(normalizedPlayerPos.x);
            sensor.AddObservation(normalizedPlayerPos.y);
            sensor.AddObservation(1f);
            sensor.AddObservation(playerShip.shipType == Player.ShipType.Balanced ? 0f :
                                playerShip.shipType == Player.ShipType.Fast ? 1f : 2f);
        }
        else
        {
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
        }

        // Game state observations (4 values)
        sensor.AddObservation(gameManager != null ? gameManager.score / 1000f : 0f);
        sensor.AddObservation(gameManager != null ? gameManager.lives / 3f : 0f);
        sensor.AddObservation(invadersFormation != null ? invadersFormation.GetAliveCount() / 55f : 0f);
        sensor.AddObservation(mysteryShip != null && mysteryShip.gameObject.activeInHierarchy ? 1f : 0f);


        // 1) Is currently in a parry window?
        sensor.AddObservation(playerShip != null && playerShip.IsParrying ? 1f : 0f);

        // 2) Direction to the nearest incoming projectile
        Vector3 threatPos = FindNearestThreat();              // returns world‐space position or Vector3.zero
        Vector2 normDir = NormalizePosition(threatPos);     // reuse your existing normalization
        sensor.AddObservation(normDir.x);
        sensor.AddObservation(normDir.y);

        // 3) Agent’s remaining parry‐cooldown [0..1]
        float elapsed = Time.time - lastAgentParryTime;
        float remNorm = Mathf.Clamp01((parryCooldown - elapsed) / parryCooldown);
        sensor.AddObservation(remNorm);


        // --- back to your existing grid observations ---
        CollectGridObservations(sensor);
    }

    private void CollectGridObservations(VectorSensor sensor)
    {
        // Create a grid-based observation of the game environment
        float cellWidth = (cameraTopRight.x - cameraBottomLeft.x) / observationGridWidth;
        float cellHeight = (cameraTopRight.y - cameraBottomLeft.y) / observationGridHeight;
        
        for (int y = 0; y < observationGridHeight; y++)
        {
            for (int x = 0; x < observationGridWidth; x++)
            {
                Vector3 cellCenter = new Vector3(
                    cameraBottomLeft.x + (x + 0.5f) * cellWidth,
                    cameraBottomLeft.y + (y + 0.5f) * cellHeight,
                    0f
                );
                
                float cellValue = 0f;
                
                // Check for invaders in this cell
                if (invadersFormation != null)
                {
                    foreach (Transform invader in invadersFormation.transform)
                    {
                        if (invader.gameObject.activeInHierarchy && 
                            Vector3.Distance(invader.position, cellCenter) < cellWidth * 0.7f)
                        {
                            cellValue = 0.8f; // Invader present
                            break;
                        }
                    }
                }
                
                // Check for bunkers in this cell
                if (cellValue == 0f && bunkers != null)
                {
                    foreach (Bunker bunker in bunkers)
                    {
                        if (bunker.gameObject.activeInHierarchy && 
                            Vector3.Distance(bunker.transform.position, cellCenter) < cellWidth * 0.7f)
                        {
                            cellValue = 0.3f; // Bunker present
                            break;
                        }
                    }
                }
                
                // Check for projectiles in this cell
                if (cellValue == 0f)
                {
                    Projectile[] projectiles = FindObjectsOfType<Projectile>();
                    foreach (Projectile projectile in projectiles)
                    {
                        if (Vector3.Distance(projectile.transform.position, cellCenter) < cellWidth * 0.5f)
                        {
                            cellValue = projectile.direction.y > 0 ? 0.5f : -0.5f; // Player vs enemy projectile
                            break;
                        }
                    }
                }
                
                // Check for mystery ship in this cell
                if (cellValue == 0f && mysteryShip != null && mysteryShip.gameObject.activeInHierarchy)
                {
                    if (Vector3.Distance(mysteryShip.transform.position, cellCenter) < cellWidth * 0.7f)
                    {
                        cellValue = 0.9f; // Mystery ship present
                    }
                }
                
                sensor.AddObservation(cellValue);
            }
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Update player reference if needed (in case it changed due to respawn)
        if (playerShip == null || !playerShip.gameObject.activeInHierarchy)
        {
            Player newPlayer = FindObjectOfType<Player>();
            if (newPlayer != null && newPlayer != playerShip)
            {
                playerShip = newPlayer;
                Debug.Log("Updated player reference: " + playerShip.name);
            }
        }
        
        // Get actions from the neural network
        int moveAction = actionBuffers.DiscreteActions[0]; // 0=left, 1=stay, 2=right
        int shootAction = actionBuffers.DiscreteActions[1]; // 0=don't shoot, 1=shoot
        int parryAction  = actionBuffers.DiscreteActions[2]; // 0=no-parry,1=parry

        if (parryAction == 1)
        {
            TryParry();
        }

        
        
        // Debug shooting actions
        if (shootAction == 1)
        {
            Debug.Log($"Agent wants to shoot! Move: {moveAction}, Shoot: {shootAction}");
        }
        
        // Convert to movement values
        horizontalMovement = moveAction == 0 ? -1f : (moveAction == 2 ? 1f : 0f);
        shouldShoot = shootAction == 1;
        
        // Apply actions if player is alive
        if (playerShip != null && playerShip.gameObject.activeInHierarchy)
        {
            ApplyMovement();
            ApplyShoot();
        }
        else
        {
            // No player available - don't spam the console, just skip actions
            // Debug.Log("No player ship available for actions");
        }
        
        // Calculate rewards
        CalculateRewards();
        
        // Check for episode end conditions
        CheckEpisodeEnd();
    }
    
    private void TryParry()
    {
        float since = Time.time - lastAgentParryTime;
        if (since < parryCooldown)
        {
            float rem = parryCooldown - since;
            Debug.Log($"Agent parry on cooldown: {rem:F1}s remaining");
            return;
        }
        // stamp the time and pay the small cost
        lastAgentParryTime = Time.time;
        AddReward(-0.01f);
        ActivateParryWindow();
    }

    private void ActivateParryWindow()
    {
        if (playerShip != null)
        {
            playerShip.StartParry();
        }
    }


    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // Manual control for testing
        var discreteActionsOut = actionsOut.DiscreteActions;

        discreteActionsOut[2] = Input.GetKey(KeyCode.P) ? 1 : 0;
        // Debug: Check if we have enough space for actions
        if (discreteActionsOut.Length < 2)
        {
            Debug.LogError($"Action space too small! Expected 2, got {discreteActionsOut.Length}");
            return;
        }

        // Movement: A/D or Arrow keys
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            discreteActionsOut[0] = 0; // Left
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            discreteActionsOut[0] = 2; // Right
        else
            discreteActionsOut[0] = 1; // Stay

        // Shooting: Space or Mouse
        discreteActionsOut[1] = (Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0)) ? 1 : 0;
    }

    private void ApplyMovement()
    {
        if (playerShip == null) return;
        
        Vector3 newPosition = playerShip.transform.position;
        newPosition.x += horizontalMovement * playerShip.speed * Time.fixedDeltaTime;
        
        // Clamp to screen bounds
        newPosition.x = Mathf.Clamp(newPosition.x, cameraBottomLeft.x, cameraTopRight.x);
        playerShip.transform.position = newPosition;
    }

    private void ApplyShoot()
    {
        if (!shouldShoot || playerShip == null) 
        {
            return;
        }
        
        // Check cooldown - use the player's attackCooldown value
        float cooldownTime = playerShip.attackCooldown;
        if (Time.time - lastAgentShotTime < cooldownTime)
        {
            Debug.Log($"Shot on cooldown. Time since last shot: {Time.time - lastAgentShotTime:F2}s, Cooldown: {cooldownTime}s");
            return;
        }
        
        Debug.Log("Attempting to shoot...");
        
        // Check if we can shoot (mimic the player's shooting logic)
        bool canShoot = true;
        
        // For balanced ship, check if laser already exists
        if (playerShip.shipType == Player.ShipType.Balanced)
        {
            Projectile existingLaser = FindObjectOfType<Projectile>();
            if (existingLaser != null && existingLaser.direction.y > 0) // Player projectile
            {
                canShoot = false;
                AddReward(wasteShotPenalty); // Penalty for wasting shots
                Debug.Log("Can't shoot - laser already exists");
            }
        }
        
        if (canShoot)
        {
            Debug.Log("Shooting now!");
            TriggerPlayerShoot();
            lastAgentShotTime = Time.time; // Update last shot time
        }
    }

    private void TriggerPlayerShoot()
    {
        if (playerShip == null) 
        {
            Debug.Log("TriggerPlayerShoot: playerShip is null");
            return;
        }
        
        // Try to use the Player script's existing shooting method through reflection
        var playerComponent = playerShip.GetComponent<Player>();
        if (playerComponent != null)
        {
            // Check if we can access the player's shooting logic
            var lastShotTimeField = typeof(Player).GetField("lastShotTime", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (lastShotTimeField != null)
            {
                float lastShotTime = (float)lastShotTimeField.GetValue(playerComponent);
                if (Time.time - lastShotTime < playerComponent.attackCooldown)
                {
                    Debug.Log("Player shooting on cooldown");
                    return;
                }
            }
        }
        
        Debug.Log($"TriggerPlayerShoot: Ship type is {playerShip.shipType}");
        
        // Use direct projectile creation with proper cooldown
        switch (playerShip.shipType)
        {
            case Player.ShipType.Balanced:
                Debug.Log("Firing balanced shot");
                FireBalancedShot();
                break;
            case Player.ShipType.Fast:
                Debug.Log("Firing arc shot");
                FireArcShot();
                break;
            case Player.ShipType.Heavy:
                Debug.Log("Firing rocket");
                FireRocket();
                break;
        }
    }
    
    private void FireBalancedShot()
    {
        Debug.Log($"FireBalancedShot: laserPrefab = {playerShip.laserPrefab}");
        
        if (playerShip.laserPrefab != null)
        {
            // Check if laser already exists (same logic as Player script)
            Projectile existingLaser = FindObjectOfType<Projectile>();
            if (existingLaser != null && existingLaser.direction.y > 0)
            {
                AddReward(wasteShotPenalty);
                Debug.Log("Laser already exists, not firing");
                return;
            }
            
            Debug.Log("Creating laser projectile");
            Projectile laser = Instantiate(playerShip.laserPrefab, playerShip.transform.position, Quaternion.identity);
            laser.damage = playerShip.shotDamage;
            Debug.Log("Laser created successfully!");
        }
        else
        {
            Debug.LogError("laserPrefab is null!");
        }
    }
    
    private void FireArcShot()
    {
        if (playerShip.arcShotPrefab == null) 
        {
            Debug.LogError("arcShotPrefab is null!");
            return;
        }
        
        // Replicate the arc shot logic from Player script
        float startAngle = -playerShip.arcSpreadAngle / 2f;
        float angleStep = playerShip.arcShotCount > 1 ? playerShip.arcSpreadAngle / (playerShip.arcShotCount - 1) : 0;

        for (int i = 0; i < playerShip.arcShotCount; i++)
        {
            float currentAngle = startAngle + (angleStep * i);
            Vector3 direction = Quaternion.Euler(0, 0, currentAngle) * Vector3.up;
            Vector3 spawnPos = playerShip.transform.position + Vector3.right * (i - playerShip.arcShotCount / 2) * 0.1f;
            
            Projectile arcProjectile = Instantiate(playerShip.arcShotPrefab, spawnPos, Quaternion.identity);
            arcProjectile.direction = direction;
            arcProjectile.damage = playerShip.shotDamage;
        }
        Debug.Log($"Fired {playerShip.arcShotCount} arc shots");
    }
    
    private void FireRocket()
    {
        if (playerShip.rocketPrefab == null) 
        {
            Debug.LogError("rocketPrefab is null!");
            return;
        }
        
        // For rockets, we'd need to check the magazine system
        // This is more complex due to the reload mechanics in the Player script
        // For now, just fire a basic rocket
        Projectile rocket = Instantiate(playerShip.rocketPrefab, playerShip.transform.position, Quaternion.identity);
        rocket.damage = playerShip.shotDamage;
        Debug.Log("Rocket fired!");
    }

    private void CalculateRewards()
    {
        if (gameManager == null) return;
        
        // Check if player respawned after death
        bool playerCurrentlyAlive = playerShip != null && playerShip.gameObject.activeInHierarchy;
        if (!playerWasAlive && playerCurrentlyAlive)
        {
            Debug.Log("Player respawned!");
            playerWasAlive = true;
            
            // Update player reference in case it changed
            if (playerShip == null || !playerShip.gameObject.activeInHierarchy)
            {
                playerShip = FindObjectOfType<Player>();
                if (playerShip != null)
                {
                    Debug.Log("Found new player ship after respawn: " + playerShip.name);
                }
            }
        }
        
        // Survival reward (only if player is alive)
        if (playerCurrentlyAlive)
        {
            AddReward(survivalReward);
        }
        
        // Score-based rewards
        int scoreDelta = gameManager.score - previousScore;
        if (scoreDelta > 0)
        {
            AddReward(scoreDelta / 100f); // Scale score reward
        }
        previousScore = gameManager.score;
        
        // Life loss penalty (but don't end episode unless game over)
        if (gameManager.lives < previousLives)
        {
            AddReward(deathPenalty * 0.5f); // Smaller penalty for losing a life (not game over)
            playerWasAlive = false;
            Debug.Log($"Player lost a life! Lives remaining: {gameManager.lives}");
        }
        previousLives = gameManager.lives;
        
        // Invader kill reward
        int currentInvaderCount = invadersFormation != null ? invadersFormation.GetAliveCount() : 0;
        if (currentInvaderCount < previousInvaderCount)
        {
            int invadersKilled = previousInvaderCount - currentInvaderCount;
            AddReward(invaderKillReward * invadersKilled);
            Debug.Log($"Invader killed! Reward: {invaderKillReward * invadersKilled}, Remaining: {currentInvaderCount}");
        }
        previousInvaderCount = currentInvaderCount;
    }

    private void CheckEpisodeEnd()
    {
        if (gameManager == null) return;
        
        // Debug current state
        bool playerAlive = playerShip != null && playerShip.gameObject.activeInHierarchy;
        int currentLives = gameManager.lives;
        int currentInvaderCount = invadersFormation != null ? invadersFormation.GetAliveCount() : 0;
        
        // Episode ends ONLY if player is dead AND no lives left (game over)
        if (currentLives <= 0)
        {
            Debug.Log($"Episode ended: Game Over! Lives: {currentLives}, Player alive: {playerAlive}");
            Debug.Log("About to call ForceRestart...");
            
            AddReward(deathPenalty);
            
            // Use the simple restart method
            if (gameManager != null)
            {
                Debug.Log("GameManager found, calling ForceRestart...");
                gameManager.ForceRestart();
                Debug.Log("ForceRestart called successfully");
            }
            else
            {
                Debug.LogError("GameManager is null!");
            }
            
            Debug.Log("About to end episode...");
            EndEpisode();
            Debug.Log("Episode ended");
            return;
        }
        
        // Episode ends if all invaders are destroyed (win condition)
        if (currentInvaderCount == 0)
        {
            Debug.Log($"Episode ended: Victory! All invaders destroyed! Invader count: {currentInvaderCount}");
            AddReward(winReward);
            EndEpisode();
            return;
        }
        
        // Optional: End episode after maximum time to prevent infinite episodes
        if (StepCount > MaxStep && MaxStep > 0)
        {
            Debug.Log($"Episode ended: Max steps reached. Steps: {StepCount}/{MaxStep}");
            EndEpisode();
            return;
        }
        
        // Debug info every 500 steps
        if (StepCount % 500 == 0)
        {
            Debug.Log($"Step {StepCount}: Lives={currentLives}, Invaders={currentInvaderCount}, Player={playerAlive}, Score={gameManager.score}");
        }
    }

    private void ResetGameState()
    {
        // Reset the game to initial state
        if (gameManager != null)
        {
            // You might need to add a reset method to GameManager
            // or manually reset components here
        }
        
        // Reset invaders
        if (invadersFormation != null)
        {
            invadersFormation.ResetInvaders();
        }
        
        // Reset bunkers
        if (bunkers != null)
        {
            foreach (Bunker bunker in bunkers)
            {
                bunker.ResetBunker();
            }
        }
        
        // Ensure player is at starting position and alive
        if (playerShip != null)
        {
            playerShip.transform.position = new Vector3(0, -4, 0); // Adjust as needed
            playerShip.gameObject.SetActive(true);
            playerShip.ResetPlayer();
        }
    }

    private Vector2 NormalizePosition(Vector3 worldPos)
    {
        float normalizedX = (worldPos.x - cameraBottomLeft.x) / (cameraTopRight.x - cameraBottomLeft.x);
        float normalizedY = (worldPos.y - cameraBottomLeft.y) / (cameraTopRight.y - cameraBottomLeft.y);
        return new Vector2(normalizedX, normalizedY);
    }

    private void UpdateCameraBounds()
    {
        if (gameCamera != null)
        {
            cameraBottomLeft = gameCamera.ViewportToWorldPoint(new Vector3(0, 0, gameCamera.nearClipPlane));
            cameraTopRight = gameCamera.ViewportToWorldPoint(new Vector3(1, 1, gameCamera.nearClipPlane));
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw observation grid for debugging
        if (observationGridWidth > 0 && observationGridHeight > 0)
        {
            UpdateCameraBounds();
            
            float cellWidth = (cameraTopRight.x - cameraBottomLeft.x) / observationGridWidth;
            float cellHeight = (cameraTopRight.y - cameraBottomLeft.y) / observationGridHeight;
            
            Gizmos.color = Color.yellow;
            for (int x = 0; x <= observationGridWidth; x++)
            {
                Vector3 start = new Vector3(cameraBottomLeft.x + x * cellWidth, cameraBottomLeft.y, 0);
                Vector3 end = new Vector3(cameraBottomLeft.x + x * cellWidth, cameraTopRight.y, 0);
                Gizmos.DrawLine(start, end);
            }
            
            for (int y = 0; y <= observationGridHeight; y++)
            {
                Vector3 start = new Vector3(cameraBottomLeft.x, cameraBottomLeft.y + y * cellHeight, 0);
                Vector3 end = new Vector3(cameraTopRight.x, cameraBottomLeft.y + y * cellHeight, 0);
                Gizmos.DrawLine(start, end);
            }
        }
    }
}