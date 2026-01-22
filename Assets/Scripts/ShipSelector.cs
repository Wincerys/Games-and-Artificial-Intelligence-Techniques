using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Listens for the 1, 2, or 3 key at startup. As soon as one is pressed,
/// it spawns the corresponding player‐ship prefab at the configured spawn
/// point, hides the selection UI, and unpauses the game.
/// </summary>
public class ShipSelector : MonoBehaviour
{
    [Header("Player‐Ship Prefabs")]
    [Tooltip("Assign the prefab for Ship 1 (Key 1)")]
    public GameObject playerShip1Prefab;

    [Tooltip("Assign the prefab for Ship 2 (Key 2)")]
    public GameObject playerShip2Prefab;

    [Tooltip("Assign the prefab for Ship 3 (Key 3)")]
    public GameObject playerShip3Prefab;

    [Header("Spawn Settings")]
    [Tooltip("Drag in the Transform where the chosen ship will spawn")]
    public Transform playerSpawnPoint;

    [Header("Optional: A UI Text that says \"Press 1/2/3 to Choose\"")]
    public GameObject promptUI; // e.g. a Canvas/Text that prompts the user

    private bool hasChosen = false;

    private void Start()
    {
        // Pause the entire game until a ship is chosen
        Time.timeScale = 0f;

        // If you provided a promptUI (e.g. a Canvas/Text), ensure it’s active
        if (promptUI != null)
            promptUI.SetActive(true);
    }

    private void Update()
    {
        if (hasChosen)
            return;

        // Listen for the digit keys 1, 2, 3
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ChooseShip(playerShip1Prefab);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ChooseShip(playerShip2Prefab);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            ChooseShip(playerShip3Prefab);
        }
    }

    private void ChooseShip(GameObject shipPrefab)
    {
        if (hasChosen)
            return;

        if (shipPrefab == null)
        {
            Debug.LogError("[ShipSelector] You forgot to assign one of the ship prefabs in the Inspector!");
            return;
        }

        // 1) Instantiate the chosen ship at the spawn point
        Instantiate(shipPrefab, playerSpawnPoint.position, Quaternion.identity);

        // 2) Hide the prompt UI (if any)
        if (promptUI != null)
            promptUI.SetActive(false);

        // 3) Unpause the game
        Time.timeScale = 1f;
        hasChosen = true;
    }
}
