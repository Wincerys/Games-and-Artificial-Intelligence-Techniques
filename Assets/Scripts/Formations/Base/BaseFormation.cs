// ===== BaseFormation.cs =====
using UnityEngine;
using System.Collections.Generic;

public abstract class BaseFormation
{
    protected int rows;
    protected int columns;
    protected float formationWidth;
    protected float formationHeight;
    protected bool guaranteeBossSpawn;
    protected int guaranteedSpecialCount;
    protected Invader bossPrefab;
    protected Invader specialPrefab;
    protected Invader[] regularPrefabs;

    public virtual void Initialize(FormationSettings settings)
    {
        rows = settings.rows;
        columns = settings.columns;
        formationWidth = settings.formationWidth;
        formationHeight = settings.formationHeight;
        guaranteeBossSpawn = settings.guaranteeBossSpawn;
        guaranteedSpecialCount = settings.guaranteedSpecialCount;
        bossPrefab = settings.bossPrefab;
        specialPrefab = settings.specialPrefab;
        regularPrefabs = settings.regularPrefabs;
    }

    public abstract FormationResult Generate(Transform parentTransform);
    protected abstract List<Vector2Int> GetBossPlacementOptions();
    protected abstract List<Vector2Int> GetSpecialPlacementOptions(List<Vector2Int> bossPositions, bool[,] occupied);
    protected abstract void GenerateRegularInvaders(bool[,] occupied, FormationData data);

    protected Vector2Int ClampBossPosition(Vector2Int pos)
    {
        return new Vector2Int(
            Mathf.Clamp(pos.x, 0, columns - 3),
            Mathf.Clamp(pos.y, 0, rows - 3)
        );
    }

    protected bool CanPlaceBossAt(Vector2Int pos, bool[,] occupied)
    {
        if (pos.x < 0 || pos.y < 0 || pos.x + 2 >= columns || pos.y + 2 >= rows)
            return false;
            
        for (int dy = 0; dy < 3; dy++)
        {
            for (int dx = 0; dx < 3; dx++)
            {
                if (occupied[pos.y + dy, pos.x + dx])
                    return false;
            }
        }
        return true;
    }

    protected bool CanPlace2x2At(Vector2Int pos, bool[,] occupied)
    {
        if (pos.x < 0 || pos.y < 0 || pos.x + 1 >= columns || pos.y + 1 >= rows)
            return false;
            
        for (int dy = 0; dy < 2; dy++)
        {
            for (int dx = 0; dx < 2; dx++)
            {
                if (occupied[pos.y + dy, pos.x + dx])
                    return false;
            }
        }
        return true;
    }

    protected bool IsValidPosition(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < columns && pos.y >= 0 && pos.y < rows;
    }

    protected int PlaceInvaderGroup(List<Vector2Int> positions, int invaderType, bool[,] occupied, Transform parent)
    {
        int placed = 0;
        foreach (Vector2Int pos in positions)
        {
            if (IsValidPosition(pos) && !occupied[pos.y, pos.x])
            {
                PlaceRegularInvader(pos, invaderType, occupied, parent);
                placed++;
            }
        }
        return placed;
    }

    protected void FillRemainingGaps(bool[,] occupied, FormationData data, Transform parent, float density)
    {
        // First place all planned invaders
        PlaceInvaderGroup(data.strongPositions, 2, occupied, parent);
        PlaceInvaderGroup(data.mediumPositions, 1, occupied, parent);
        PlaceInvaderGroup(data.weakPositions, 0, occupied, parent);
        
        // Then fill remaining gaps based on density
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                if (!occupied[row, col] && Random.value < density)
                {
                    // Choose invader type based on neighbors or use random
                    int invaderType = GetBestInvaderTypeForPosition(new Vector2Int(col, row), occupied);
                    PlaceRegularInvader(new Vector2Int(col, row), invaderType, occupied, parent);
                }
            }
        }
    }
    
    protected int GetBestInvaderTypeForPosition(Vector2Int pos, bool[,] occupied)
    {
        // Check what types of invaders are nearby and use similar type
        Dictionary<int, int> nearbyTypes = new Dictionary<int, int> { {0, 0}, {1, 0}, {2, 0} };
        
        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue;
                
                int checkX = pos.x + dx;
                int checkY = pos.y + dy;
                
                if (checkX >= 0 && checkX < columns && checkY >= 0 && checkY < rows && occupied[checkY, checkX])
                {
                    // Try to determine what type this neighbor might be
                    // For now, we'll use a simple heuristic based on position
                    int estimatedType = EstimateInvaderType(new Vector2Int(checkX, checkY));
                    nearbyTypes[estimatedType]++;
                }
            }
        }
        
        // Return the most common nearby type, or random if no neighbors
        int maxCount = 0;
        int bestType = Random.Range(0, 3);
        
        foreach (var kvp in nearbyTypes)
        {
            if (kvp.Value > maxCount)
            {
                maxCount = kvp.Value;
                bestType = kvp.Key;
            }
        }
        
        return bestType;
    }
    
    protected virtual int EstimateInvaderType(Vector2Int pos)
    {
        // Default estimation - can be overridden by specific formations
        // Front third: strong, middle: medium, back: weak
        if (pos.y < rows / 3) return 2; // Strong
        if (pos.y < 2 * rows / 3) return 1; // Medium
        return 0; // Weak
    }
    
    protected Vector3 GridToWorldPosition(float gridX, float gridY)
    {
        float cellWidth = formationWidth / (columns - 1);
        float cellHeight = formationHeight / (rows - 1);
        
        float worldX = -formationWidth * 0.5f + gridX * cellWidth;
        float worldY = -formationHeight * 0.5f + gridY * cellHeight;
        
        return new Vector3(worldX, worldY, 0f);
    }

    protected void PlaceBoss(Vector2Int pos, bool[,] occupied, Transform parent)
    {
        if (bossPrefab == null) return;
        
        Vector3 worldPos = GridToWorldPosition(pos.x + 1, pos.y + 1);
        Invader boss = Object.Instantiate(bossPrefab, parent);
        boss.transform.localPosition = worldPos;
        
        for (int dy = 0; dy < 3; dy++)
        {
            for (int dx = 0; dx < 3; dx++)
            {
                occupied[pos.y + dy, pos.x + dx] = true;
            }
        }
    }

    protected void PlaceSpecial(Vector2Int pos, bool[,] occupied, Transform parent)
    {
        if (specialPrefab == null) return;
        
        Vector3 worldPos = GridToWorldPosition(pos.x + 0.5f, pos.y + 0.5f);
        Invader special = Object.Instantiate(specialPrefab, parent);
        special.transform.localPosition = worldPos;
        
        for (int dy = 0; dy < 2; dy++)
        {
            for (int dx = 0; dx < 2; dx++)
            {
                occupied[pos.y + dy, pos.x + dx] = true;
            }
        }
    }

    protected void PlaceRegularInvader(Vector2Int pos, int invaderType, bool[,] occupied, Transform parent)
    {
        if (invaderType >= 0 && invaderType < regularPrefabs.Length && regularPrefabs[invaderType] != null)
        {
            Vector3 worldPos = GridToWorldPosition(pos.x, pos.y);
            Invader invader = Object.Instantiate(regularPrefabs[invaderType], parent);
            invader.transform.localPosition = worldPos;
            occupied[pos.y, pos.x] = true;
        }
    }
}