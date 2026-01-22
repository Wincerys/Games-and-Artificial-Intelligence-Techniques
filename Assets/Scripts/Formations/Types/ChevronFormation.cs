// ===== ChevronFormation.cs =====
using UnityEngine;
using System.Collections.Generic;

public class ChevronFormation : BaseFormation
{
    public override FormationResult Generate(Transform parentTransform)
    {
        bool[,] occupied = new bool[rows, columns];
        FormationData data = new FormationData();
        
        // Place boss strategically
        List<Vector2Int> bossOptions = GetBossPlacementOptions();
        Vector2Int chosenBossPos = bossOptions[Random.Range(0, bossOptions.Count)];
        chosenBossPos = ClampBossPosition(chosenBossPos);
        
        int bossesSpawned = 0;
        if (guaranteeBossSpawn && CanPlaceBossAt(chosenBossPos, occupied))
        {
            PlaceBoss(chosenBossPos, occupied, parentTransform);
            data.bossPositions.Add(chosenBossPos);
            bossesSpawned = 1;
        }
        
        GenerateRegularInvaders(occupied, data);
        int specialsSpawned = PlaceSpecialInvaders(data.bossPositions, occupied, parentTransform);
        int regularsSpawned = PlaceAllRegularInvaders(occupied, data, parentTransform);
        
        return new FormationResult
        {
            formationName = "Chevron",
            totalInvadersSpawned = bossesSpawned + specialsSpawned + regularsSpawned,
            bossesSpawned = bossesSpawned,
            specialsSpawned = specialsSpawned,
            success = true
        };
    }

    protected override List<Vector2Int> GetBossPlacementOptions()
    {
        int centerCol = columns / 2;
        bool invertChevron = Random.value > 0.5f;
        
        return new List<Vector2Int>
        {
            new Vector2Int(centerCol - 1, invertChevron ? 1 : rows - 3),            // Tip of chevron
            new Vector2Int(centerCol - 1, invertChevron ? rows - 3 : 1),            // Base of chevron
            new Vector2Int(centerCol - 1, rows / 2 - 1),                            // Middle
            new Vector2Int(centerCol - 3, invertChevron ? rows / 4 : 3 * rows / 4 - 2), // Left side
            new Vector2Int(centerCol + 1, invertChevron ? rows / 4 : 3 * rows / 4 - 2), // Right side
            new Vector2Int(centerCol - 2, rows / 2),                                // Left of center
            new Vector2Int(centerCol, rows / 2)                                     // Right of center
        };
    }

    protected override List<Vector2Int> GetSpecialPlacementOptions(List<Vector2Int> bossPositions, bool[,] occupied)
    {
        List<Vector2Int> positions = new List<Vector2Int>();
        int centerCol = columns / 2;
        
        // Chevron strategic positions
        positions.Add(new Vector2Int(centerCol - 1, 1));               // Tip
        positions.Add(new Vector2Int(centerCol - 1, rows - 3));        // Base center
        positions.Add(new Vector2Int(centerCol - 4, rows / 4));        // Left mid
        positions.Add(new Vector2Int(centerCol + 2, rows / 4));        // Right mid
        positions.Add(new Vector2Int(centerCol - 6, 3 * rows / 4));    // Left base
        positions.Add(new Vector2Int(centerCol + 4, 3 * rows / 4));    // Right base
        positions.Add(new Vector2Int(centerCol - 1, rows / 2));        // Center mid
        positions.Add(new Vector2Int(centerCol - 3, rows / 2));        // Left center
        positions.Add(new Vector2Int(centerCol + 1, rows / 2));        // Right center
        positions.Add(new Vector2Int(centerCol - 5, rows / 3));        // Left outer
        positions.Add(new Vector2Int(centerCol + 3, rows / 3));        // Right outer
        
        return positions;
    }

    protected override void GenerateRegularInvaders(bool[,] occupied, FormationData data)
    {
        int centerCol = columns / 2;
        bool invertChevron = Random.value > 0.5f;
        
        for (int row = 0; row < rows; row++)
        {
            int actualRow = invertChevron ? rows - 1 - row : row;
            float ratio = (float)row / (rows - 1);
            int spread = Mathf.RoundToInt(ratio * (columns / 2));
            
            for (int offset = -spread; offset <= spread; offset++)
            {
                int col = centerCol + offset;
                if (IsValidPosition(new Vector2Int(col, actualRow)) && Random.value < 0.85f)
                {
                    InvaderType type;
                    
                    // Center spine: strongest
                    if (Mathf.Abs(offset) < spread / 3)
                        type = InvaderType.Strong;
                    // Middle sections: medium
                    else if (Mathf.Abs(offset) < 2 * spread / 3)
                        type = InvaderType.Medium;
                    // Outer edges: weak
                    else
                        type = InvaderType.Weak;
                    
                    AddToFormationData(new Vector2Int(col, actualRow), type, data);
                }
            }
            
            // Add some extra positions at the chevron arms
            if (spread > 3)
            {
                int armOffset = spread + 1;
                if (centerCol - armOffset >= 0 && Random.value < 0.5f)
                    AddToFormationData(new Vector2Int(centerCol - armOffset, actualRow), InvaderType.Medium, data);
                if (centerCol + armOffset < columns && Random.value < 0.5f)
                    AddToFormationData(new Vector2Int(centerCol + armOffset, actualRow), InvaderType.Medium, data);
            }
        }
    }

    private void AddToFormationData(Vector2Int pos, InvaderType type, FormationData data)
    {
        switch (type)
        {
            case InvaderType.Strong: data.strongPositions.Add(pos); break;
            case InvaderType.Medium: data.mediumPositions.Add(pos); break;
            case InvaderType.Weak: data.weakPositions.Add(pos); break;
        }
    }

    private int PlaceSpecialInvaders(List<Vector2Int> bossPositions, bool[,] occupied, Transform parent)
    {
        if (specialPrefab == null) return 0;
        
        List<Vector2Int> specialOptions = GetSpecialPlacementOptions(bossPositions, occupied);
        
        int specialsPlaced = 0;
        foreach (Vector2Int pos in specialOptions)
        {
            if (specialsPlaced >= guaranteedSpecialCount) break;
            
            if (CanPlace2x2At(pos, occupied))
            {
                PlaceSpecial(pos, occupied, parent);
                specialsPlaced++;
            }
        }
        
        return specialsPlaced;
    }

    private int PlaceAllRegularInvaders(bool[,] occupied, FormationData data, Transform parent)
    {
        int placed = 0;
        placed += PlaceInvaderGroup(data.strongPositions, 2, occupied, parent);
        placed += PlaceInvaderGroup(data.mediumPositions, 1, occupied, parent);
        placed += PlaceInvaderGroup(data.weakPositions, 0, occupied, parent);
        return placed;
    }

    private int PlaceInvaderGroup(List<Vector2Int> positions, int invaderType, bool[,] occupied, Transform parent)
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
}