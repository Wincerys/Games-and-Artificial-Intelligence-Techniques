// ===== WingsFormation.cs =====
using UnityEngine;
using System.Collections.Generic;

public class WingsFormation : BaseFormation
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
        
        // Generate wings structure
        GenerateRegularInvaders(occupied, data);
        
        // Place special invaders
        int specialsSpawned = PlaceSpecialInvaders(data.bossPositions, occupied, parentTransform);
        
        // Place all regular invaders
        int regularsSpawned = PlaceAllRegularInvaders(occupied, data, parentTransform);
        
        return new FormationResult
        {
            formationName = "Wings",
            totalInvadersSpawned = bossesSpawned + specialsSpawned + regularsSpawned,
            bossesSpawned = bossesSpawned,
            specialsSpawned = specialsSpawned,
            success = true
        };
    }

    protected override List<Vector2Int> GetBossPlacementOptions()
    {
        int centerCol = columns / 2;
        int wingWidth = columns / 4;
        
        return new List<Vector2Int>
        {
            new Vector2Int(centerCol - 1, rows - 2),        // Back center (command position)
            new Vector2Int(2, rows / 2),                    // Left wing command
            new Vector2Int(columns - 4, rows / 2),          // Right wing command
            new Vector2Int(centerCol - 1, rows / 2),        // Center middle
            new Vector2Int(wingWidth / 2, rows - 2),        // Left wing back
            new Vector2Int(columns - wingWidth / 2 - 2, rows - 2) // Right wing back
        };
    }

    protected override List<Vector2Int> GetSpecialPlacementOptions(List<Vector2Int> bossPositions, bool[,] occupied)
    {
        List<Vector2Int> positions = new List<Vector2Int>();
        int wingWidth = columns / 4;
        
        // Key strategic positions for wings formation
        positions.Add(new Vector2Int(2, rows / 2));                    // Left wing center
        positions.Add(new Vector2Int(columns - 4, rows / 2));          // Right wing center
        positions.Add(new Vector2Int(wingWidth / 2, 1));               // Left wing front
        positions.Add(new Vector2Int(columns - wingWidth / 2 - 2, 1)); // Right wing front
        positions.Add(new Vector2Int(2, rows - 3));                    // Left wing back
        positions.Add(new Vector2Int(columns - 4, rows - 3));          // Right wing back
        positions.Add(new Vector2Int(wingWidth - 1, rows / 4));        // Left wing mid-front
        positions.Add(new Vector2Int(columns - wingWidth - 1, rows / 4)); // Right wing mid-front
        positions.Add(new Vector2Int(1, 3 * rows / 4));                // Left wing mid-back
        positions.Add(new Vector2Int(columns - 3, 3 * rows / 4));      // Right wing mid-back
        
        return positions;
    }

    protected override void GenerateRegularInvaders(bool[,] occupied, FormationData data)
    {
        int centerCol = columns / 2;
        int wingWidth = columns / 4;
        
        for (int row = 0; row < rows; row++)
        {
            // Left wing - strong flanks
            for (int col = 0; col < wingWidth; col++)
            {
                if (Random.value < 0.9f)
                    data.strongPositions.Add(new Vector2Int(col, row));
            }
            
            // Right wing - strong flanks
            for (int col = columns - wingWidth; col < columns; col++)
            {
                if (Random.value < 0.9f)
                    data.strongPositions.Add(new Vector2Int(col, row));
            }
            
            // Center - weaker but still defended
            for (int col = wingWidth + 1; col < columns - wingWidth - 1; col++)
            {
                if (Random.value < 0.6f)
                    data.weakPositions.Add(new Vector2Int(col, row));
            }
            
            // Transition zones - medium strength
            if (wingWidth < centerCol - 1)
            {
                if (Random.value < 0.7f)
                    data.mediumPositions.Add(new Vector2Int(wingWidth, row));
                if (Random.value < 0.7f)
                    data.mediumPositions.Add(new Vector2Int(columns - wingWidth - 1, row));
            }
        }
    }

    private int PlaceSpecialInvaders(List<Vector2Int> bossPositions, bool[,] occupied, Transform parent)
    {
        if (specialPrefab == null) return 0;
        
        List<Vector2Int> specialOptions = GetSpecialPlacementOptions(bossPositions, occupied);
        
        // Shuffle for variety
        for (int i = 0; i < specialOptions.Count; i++)
        {
            Vector2Int temp = specialOptions[i];
            int randomIndex = Random.Range(i, specialOptions.Count);
            specialOptions[i] = specialOptions[randomIndex];
            specialOptions[randomIndex] = temp;
        }
        
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