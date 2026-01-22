using UnityEngine;
using System.Collections.Generic;

public class Invaders : MonoBehaviour
{
    [Header("Formation System")]
    [SerializeField] private FormationManager formationManager;
    
    [Header("Invader Prefabs")]
    [Tooltip("Array of regular invader prefabs: [0]=Common, [1]=Buffed, [2]=MegaBuffed")]
    public Invader[] prefabs = new Invader[3]; // 0=Common, 1=Buffed, 2=MegaBuffed
    
    [Tooltip("Special 2x2 invader prefab")]
    public Invader specialPrefab;
    
    [Tooltip("Boss 3x3 invader prefab")]
    public Invader bossPrefab;
    
    [Header("Formation Settings")]
    public bool isStationary = true;
    public float formationWidth = 20f;
    public float formationHeight = 10f;
    public int rows = 6;
    public int columns = 17;
    
    [Header("Formation Controls")]
    public FormationManager.FormationType formationType = FormationManager.FormationType.Random;
    public bool guaranteeBossSpawn = true;
    [Range(2, 8)]
    public int guaranteedSpecialCount = 4;
    [Range(0.3f, 1f)]
    public float formationDensity = 0.75f;
    [Range(0f, 1f)]
    public float clusteringStrength = 0.6f;
    
    [Header("Movement / Speed Curve (if not stationary)")]
    public AnimationCurve speed = new AnimationCurve();
    private Vector3 direction = Vector3.right;
    private Vector3 initialPosition;

    [Header("Missiles / Shooting")]
    public float missileSpawnRate = 1f;

    private void Awake()
    {
        initialPosition = transform.position;
        SetupFormationManager();
    }

    private void Start()
    {
        // Generate formation after everything is properly initialized
        GenerateNewFormation();
        InvokeRepeating(nameof(MissileAttack), missileSpawnRate, missileSpawnRate);
    }

    private void SetupFormationManager()
    {
        // Create formation manager if it doesn't exist
        if (formationManager == null)
        {
            GameObject managerObject = new GameObject("FormationManager");
            managerObject.transform.SetParent(transform);
            formationManager = managerObject.AddComponent<FormationManager>();
        }
        
        // Configure formation settings
        FormationSettings settings = new FormationSettings
        {
            rows = rows,
            columns = columns,
            formationWidth = formationWidth,
            formationHeight = formationHeight,
            guaranteeBossSpawn = guaranteeBossSpawn,
            guaranteedSpecialCount = guaranteedSpecialCount,
            bossPrefab = bossPrefab,
            specialPrefab = specialPrefab,
            regularPrefabs = prefabs,
            formationDensity = formationDensity,
            clusteringStrength = clusteringStrength
        };
        
        formationManager.settings = settings;
        formationManager.formationType = formationType;
        
        // Now initialize the formation manager with proper settings
        formationManager.Initialize();
    }

    public void GenerateNewFormation()
    {
        // Add debugging
        Debug.Log("GenerateNewFormation called");
        
        if (formationManager == null)
        {
            Debug.LogError("FormationManager is null!");
            return;
        }
        
        // Update settings in case they changed in inspector
        UpdateFormationSettings();
        
        // Clear existing invaders first
        ClearExistingInvaders();
        
        // Generate the formation
        FormationResult result = formationManager.GenerateFormation(transform);
        
        if (result.success)
        {
            Debug.Log($"Successfully generated {result.formationName} formation: " +
                     $"{result.totalInvadersSpawned} total invaders " +
                     $"({result.bossesSpawned} bosses, {result.specialsSpawned} specials)");
        }
        else
        {
            Debug.LogError($"Formation generation failed: {result.errorMessage}");
        }
    }

    private void ClearExistingInvaders()
    {
        // Clear all existing invader children
        for (int c = transform.childCount - 1; c >= 0; c--)
        {
            Transform child = transform.GetChild(c);
            if (child.name != "FormationManager") // Don't destroy the formation manager
            {
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }
    }

    private void UpdateFormationSettings()
    {
        if (formationManager != null)
        {
            FormationSettings settings = new FormationSettings
            {
                rows = rows,
                columns = columns,
                formationWidth = formationWidth,
                formationHeight = formationHeight,
                guaranteeBossSpawn = guaranteeBossSpawn,
                guaranteedSpecialCount = guaranteedSpecialCount,
                bossPrefab = bossPrefab,
                specialPrefab = specialPrefab,
                regularPrefabs = prefabs,
                formationDensity = formationDensity,
                clusteringStrength = clusteringStrength
            };
            
            formationManager.UpdateSettings(settings);
            formationManager.formationType = formationType;
        }
    }

    #region Original Invaders Functionality

    private void MissileAttack()
    {
        var regularInvaders = GetRegularInvaders();
        int amountAlive = regularInvaders.Count;
        
        if (amountAlive == 0) return;

        foreach (Transform invaderTransform in regularInvaders)
        {
            if (!invaderTransform.gameObject.activeInHierarchy) continue;

            if (Random.value < (1f / amountAlive))
            {
                Invader invaderComponent = invaderTransform.GetComponent<Invader>();
                if (invaderComponent != null)
                {
                    Vector3 spawnPos = invaderTransform.position + Vector3.down * 0.5f;
                    Quaternion rot = Quaternion.identity;
                    invaderComponent.TryShoot(spawnPos, rot);
                }
                break;
            }
        }
    }

    private List<Transform> GetRegularInvaders()
    {
        var regularInvaders = new List<Transform>();
        
        foreach (Transform invaderTransform in transform)
        {
            if (!invaderTransform.gameObject.activeInHierarchy) continue;
                
            Invader invaderComponent = invaderTransform.GetComponent<Invader>();
            if (invaderComponent != null && !invaderComponent.independentAttacker)
            {
                regularInvaders.Add(invaderTransform);
            }
        }
        
        return regularInvaders;
    }

    private void Update()
    {
        if (!isStationary)
        {
            HandleMovement();
        }
    }

    private void HandleMovement()
    {
        int totalCells = rows * columns;
        int amountAlive = GetAliveCount();
        int amountKilled = totalCells - amountAlive;
        float percentKilled = amountKilled / (float)totalCells;

        float currentSpeed = speed.Evaluate(percentKilled);
        transform.position += direction * Time.deltaTime * currentSpeed;

        Vector3 leftEdge = Camera.main.ViewportToWorldPoint(Vector3.zero);
        Vector3 rightEdge = Camera.main.ViewportToWorldPoint(Vector3.one);

        foreach (Transform invaderTransform in transform)
        {
            if (!invaderTransform.gameObject.activeInHierarchy) continue;

            if (direction == Vector3.right && invaderTransform.position.x >= (rightEdge.x - 1f))
            {
                AdvanceRow();
                break;
            }
            else if (direction == Vector3.left && invaderTransform.position.x <= (leftEdge.x + 1f))
            {
                AdvanceRow();
                break;
            }
        }
    }

    private void AdvanceRow()
    {
        direction = new Vector3(-direction.x, 0f, 0f);
        transform.position += Vector3.down * 1f;
    }

    public void ResetInvaders()
    {
        direction = Vector3.right;
        transform.position = initialPosition;
        
        // Always generate new formation for now
        // Both AI and normal gameplay will get new formations
        GenerateNewFormation();
    }

    public int GetAliveCount()
    {
        int count = 0;
        foreach (Transform invaderTransform in transform)
        {
            if (invaderTransform.gameObject.activeSelf)
                count++;
        }
        return count;
    }

    #endregion

    #region Editor Utilities

    [ContextMenu("Generate New Formation")]
    public void EditorGenerateFormation()
    {
        if (Application.isPlaying)
        {
            GenerateNewFormation();
        }
        else
        {
            Debug.Log("Formation generation only works in Play Mode");
        }
    }

    [ContextMenu("Test All Formations")]
    public void EditorTestAllFormations()
    {
        if (!Application.isPlaying)
        {
            Debug.Log("Formation testing only works in Play Mode");
            return;
        }

        StartCoroutine(TestAllFormationsCoroutine());
    }

    private System.Collections.IEnumerator TestAllFormationsCoroutine()
    {
        FormationManager.FormationType[] allTypes = {
            FormationManager.FormationType.Fortress,
            FormationManager.FormationType.Wings,
            FormationManager.FormationType.Spearhead,
            FormationManager.FormationType.Castle,
            FormationManager.FormationType.Scattered,
            FormationManager.FormationType.Wall,
            FormationManager.FormationType.Diamond,
            FormationManager.FormationType.Chevron
        };

        FormationManager.FormationType originalType = formationType;

        foreach (var type in allTypes)
        {
            formationType = type;
            formationManager.formationType = type;
            GenerateNewFormation();
            yield return new WaitForSeconds(2f);
        }

        formationType = originalType;
        formationManager.formationType = originalType;
    }

    #endregion
}