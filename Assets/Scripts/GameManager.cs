using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Player‐Ship Selection")]
    [Tooltip("Assign the prefab for Ship 1 (Key 1)")]
    public GameObject playerShip1Prefab;
    [Tooltip("Assign the prefab for Ship 2 (Key 2)")]
    public GameObject playerShip2Prefab;
    [Tooltip("Assign the prefab for Ship 3 (Key 3)")]
    public GameObject playerShip3Prefab;

    [Header("Spawn Settings")]
    [Tooltip("Where the chosen player ship will appear")]
    public Transform playerSpawnPoint;

    [Header("UI Elements")]
    [Tooltip("UI prompting “Press 1/2/3 to choose your ship”")]
    public GameObject promptUI;
    [Tooltip("Game Over UI panel")]
    public GameObject gameOverUI;
    [Tooltip("UI Text showing current score")]
    public Text scoreText;
    [Tooltip("UI Text showing remaining lives")]
    public Text livesText;

    private GameObject selectedShipPrefab;
    private GameObject currentPlayer;
    private bool hasSelected = false;
    private bool isGameOver = false;

    private Invaders invaders;
    private MysteryShip mysteryShip;
    private Bunker[] bunkers;
    private bool playerDeathInProgress = false;

    public int score { get; private set; } = 0;
    public int lives { get; private set; } = 3;

    private bool skipNextKill = false;



    public void NotifyParrySuccess()
    {
        skipNextKill = true;
    }


    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            DestroyImmediate(gameObject);
            return;
        }
    }

    private void Start()
    {
        // At startup: show only the selection prompt, hide everything else
        if (promptUI != null)
            promptUI.SetActive(true);

        if (gameOverUI != null)
            gameOverUI.SetActive(false);

        if (scoreText != null)
            scoreText.gameObject.SetActive(false);

        if (livesText != null)
            livesText.gameObject.SetActive(false);

        Time.timeScale = 0f; // pause until ship is chosen
    }

    private void Update()
    {
        // If we haven't chosen a ship yet, listen for 1/2/3
        if (!hasSelected)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
                ChooseShip(playerShip1Prefab);
            else if (Input.GetKeyDown(KeyCode.Alpha2))
                ChooseShip(playerShip2Prefab);
            else if (Input.GetKeyDown(KeyCode.Alpha3))
                ChooseShip(playerShip3Prefab);
            return;
        }

        // If we're in GameOver state, listen for Enter to restart
        if (isGameOver)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                // Reload the current scene to start over
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }
    }

    public void ForceShipSelection(int shipNumber)
    {
        GameObject shipPrefab = null;

        switch (shipNumber)
        {
            case 1:
                shipPrefab = playerShip1Prefab;
                break;
            case 2:
                shipPrefab = playerShip2Prefab;
                break;
            case 3:
                shipPrefab = playerShip3Prefab;
                break;
            default:
                Debug.LogError($"Invalid ship number: {shipNumber}. Using ship 1 as fallback.");
                shipPrefab = playerShip1Prefab;
                break;
        }

        if (shipPrefab != null)
        {
            ChooseShip(shipPrefab);
        }
        else
        {
            Debug.LogError($"Ship {shipNumber} prefab is not assigned in GameManager!");
        }
    }


    public void ForceRestart()
    {
        Debug.Log("=== ForceRestart called - AI training mode ===");

        // Only execute if AI agent is present
        SpaceInvadersAgent agent = FindObjectOfType<SpaceInvadersAgent>();
        if (agent == null)
        {
            Debug.Log("No AI agent found - using normal game over");
            return;
        }

        // Reset game state
        isGameOver = false;
        playerDeathInProgress = false;

        // Hide game over UI
        if (gameOverUI != null)
            gameOverUI.SetActive(false);

        // Ensure time scale is normal
        Time.timeScale = 1f;

        // Destroy any existing player
        if (currentPlayer != null)
        {
            Destroy(currentPlayer);
            currentPlayer = null;
        }

        // Reset score and lives
        SetScore(0);
        SetLives(3);

        // Reset the selection state
        hasSelected = false;

        // Force ship selection with a small delay to ensure everything is reset
        StartCoroutine(DelayedShipSelection());

        Debug.Log("=== Auto restart initiated ===");
    }

    private System.Collections.IEnumerator DelayedShipSelection()
    {
        yield return new WaitForSeconds(0.1f); // Small delay

        Debug.Log("Delayed ship selection starting...");
        ForceShipSelection(2);

        // Also call NewRound to reset invaders and bunkers
        yield return new WaitForSeconds(0.1f);
        NewRound();

        Debug.Log("=== Auto restart completed ===");
    }

    private void ChooseShip(GameObject shipPrefab)
    {
        if (hasSelected) return;
        if (shipPrefab == null)
        {
            Debug.LogError("[GameManager] Ship prefab not assigned!");
            return;
        }

        selectedShipPrefab = shipPrefab;

        // Hide the prompt
        if (promptUI != null)
            promptUI.SetActive(false);

        // Show score & lives UI now that gameplay is starting
        if (scoreText != null)
            scoreText.gameObject.SetActive(true);
        if (livesText != null)
            livesText.gameObject.SetActive(true);

        // Unpause and initialize everything
        Time.timeScale = 1f;
        hasSelected = true;
        InitializeGame();
    }

    private void InitializeGame()
    {
        // Spawn the player
        SpawnPlayer();

        // Find other game components
        invaders = FindObjectOfType<Invaders>();
        mysteryShip = FindObjectOfType<MysteryShip>();
        bunkers = FindObjectsOfType<Bunker>();

        // Hide Game Over if it was active
        if (gameOverUI != null)
            gameOverUI.SetActive(false);

        // Reset score & lives
        SetScore(0);
        SetLives(3);

        // Start the first wave
        NewRound();
    }

    private void RespawnPlayer()
    {
        
        SpawnPlayer();
        playerDeathInProgress = false; // Reset the flag
        

        // Reset player state
        if (currentPlayer != null)
        {
            Player playerScript = currentPlayer.GetComponent<Player>();
            if (playerScript != null)
            {
                playerScript.ResetPlayer();
            }
        }
        Debug.Log("Player respawned and death flag reset");
    }

    private void SpawnPlayer()
    {
        // Remove any existing player first
        if (currentPlayer != null)
            Destroy(currentPlayer);

        // Instantiate chosen ship prefab at the correct spawn point
        if (selectedShipPrefab != null && playerSpawnPoint != null)
        {
            Debug.Log($"Spawning player at: {playerSpawnPoint.position}");
            currentPlayer = Instantiate(
                selectedShipPrefab,
                playerSpawnPoint.position,
                Quaternion.identity
            );

            // Reset the player state if it's a respawn
            Player playerScript = currentPlayer.GetComponent<Player>();
            if (playerScript != null)
            {
                playerScript.ResetPlayer();
            }
        }
        else
        {
            Debug.LogError($"Cannot spawn player! selectedShipPrefab: {selectedShipPrefab}, playerSpawnPoint: {playerSpawnPoint}");
        }
    }

    private void NewRound()
    {
        // This is now only used for AI training
        SpaceInvadersAgent agent = FindObjectOfType<SpaceInvadersAgent>();
        if (agent != null)
        {
            // AI training mode - full reset including player position
            // Reset Invaders (this method clears old invaders and spawns a fresh grid)
            if (invaders != null)
            {
                invaders.ResetInvaders();
                invaders.gameObject.SetActive(true);
            }

            // Reset all bunkers
            if (bunkers != null)
            {
                foreach (var bunker in bunkers)
                {
                    bunker.ResetBunker();
                }
            }

            // Reactivate MysteryShip if it exists
            if (mysteryShip != null)
            {
                mysteryShip.gameObject.SetActive(true);
            }

            // For AI training, respawn player at spawn point
            if (currentPlayer != null)
            {
                currentPlayer.SetActive(true);
                if (playerSpawnPoint != null)
                {
                    currentPlayer.transform.position = playerSpawnPoint.position;
                }
            }

            isGameOver = false;
        }
        else
        {
            // Fallback to normal gameplay
            NormalGameplayNewRound();
        }
    }

    private void GameOver()
    {
        isGameOver = true;

        // Check if AI is training
        SpaceInvadersAgent agent = FindObjectOfType<SpaceInvadersAgent>();
        if (agent != null)
        {
            // AI training mode - auto restart after short delay
            Debug.Log("AI training - auto restarting in 1 second");
            Invoke(nameof(ForceRestart), 1f);
            return;
        }

        // Normal gameplay - show Game Over UI and wait for player input
        if (gameOverUI != null)
            gameOverUI.SetActive(true);

        // Optionally hide invaders so the screen isn't cluttered
        if (invaders != null)
            invaders.gameObject.SetActive(false);

        // Pause the game (so invaders stop moving)
        Time.timeScale = 0f;
    }

    private void SetScore(int newScore)
    {
        score = newScore;
        if (scoreText != null)
            scoreText.text = score.ToString().PadLeft(4, '0');
    }

    private void SetLives(int newLives)
    {
        lives = Mathf.Max(newLives, 0);
        if (livesText != null)
            livesText.text = lives.ToString();
    }

    // Called by Player when it dies
    public void OnPlayerKilled(Player player)
    {
        if (skipNextKill)
        {
            Debug.Log("OnPlayerKilled skipped because parry just happened.");
            skipNextKill = false;
            return;
        }
        if (playerDeathInProgress)
        {
            Debug.Log("Death already in progress - ignoring call");
            Destroy(currentPlayer);
            return;
        }
        

        playerDeathInProgress = true;
        Destroy(currentPlayer);

        // 1) Destroy the exact GameObject that died
        if (player != null && player.gameObject != null)
        {
            Destroy(player.gameObject);
            Debug.Log("Player GameObject destroyed");
        }

        Destroy(currentPlayer);

        // 2) Null out the currentPlayer reference
        currentPlayer = null;

        // 3) Decrement lives and update UI
        Debug.Log($"OnPlayerKilled called! Lives before decrement: {lives}");
        SetLives(lives - 1);

        if (lives > 0)
        {
            // Respawn after a short delay
            Invoke(nameof(RespawnPlayer), 1f);
        }
        else
        {
            // No lives left → Game Over
            GameOver();
        }
    }


    // Called by Invader when it dies
    public void OnInvaderKilled(Invader invader)
    {
        if (invader != null && invader.gameObject != null)
            invader.gameObject.SetActive(false);

        // Add that invader's score
        SetScore(score + invader.score);

        // If all invaders are dead, start a new round
        if (invaders != null && invaders.GetAliveCount() == 0)
        {
            // Check if AI is training
            SpaceInvadersAgent agent = FindObjectOfType<SpaceInvadersAgent>();
            if (agent != null)
            {
                // AI training mode - use new round system
                NewRound();
            }
            else
            {
                // Normal gameplay - use traditional progression
                NormalGameplayNewRound();
            }
        }
    }

    private void NormalGameplayNewRound()
    {
        // Reset Invaders using the original simple system
        if (invaders != null)
        {
            // For normal gameplay, use a simpler invader reset
            invaders.ResetInvaders();
            invaders.gameObject.SetActive(true);
        }

        // Reset all bunkers
        if (bunkers != null)
        {
            foreach (var bunker in bunkers)
            {
                bunker.ResetBunker();
            }
        }

        // Reactivate MysteryShip if it exists
        if (mysteryShip != null)
        {
            mysteryShip.gameObject.SetActive(true);
        }

        // Ensure player stays in current position for normal gameplay
        if (currentPlayer != null)
        {
            currentPlayer.SetActive(true);
            // Don't move the player - let them stay where they are
        }

        isGameOver = false;
    }

    // Called by MysteryShip when it is shot
    public void OnMysteryShipKilled(MysteryShip mystery)
    {
        SetScore(score + mystery.score);
    }

    // Called by Invader if it reaches the boundary
    public void OnBoundaryReached()
    {
        if (invaders != null && invaders.gameObject.activeSelf)
        {
            invaders.gameObject.SetActive(false);

            // Only kill player if they're still alive
            if (currentPlayer != null && currentPlayer.activeSelf)
            {
                Player playerScript = currentPlayer.GetComponent<Player>();
                if (playerScript != null && !playerDeathInProgress)
                {
                    OnPlayerKilled(playerScript);
                }
            }
        }
    }
    
    public void AddLife(int amount = 1)
    {
        SetLives(lives + amount);
        Debug.Log($"Lives increased! Now: {lives}");
    }
}