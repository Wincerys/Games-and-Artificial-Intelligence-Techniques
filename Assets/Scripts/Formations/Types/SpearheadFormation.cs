// ===== SpearheadFormation.cs =====
using UnityEngine;
using System.Collections.Generic;

public class SpearheadFormation : BaseFormation
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
        
        // Generate spearhead structure
        GenerateRegularInvaders(occupied, data);
        
        // Place special invaders
        int specialsSpawned = PlaceSpecialInvaders(data.bossPositions, occupied, parentTransform);
        
        // Place all regular invaders
        int regularsSpawned = PlaceAllRegularInvaders(occupied, data, parentTransform);
        
        return new FormationResult
        {
            formationName = "Spearhead",
            totalInvadersSpawned = bossesSpawned + specialsSpawned + regularsSpawned,
            bossesSpawned = bossesSpawned,
            specialsSpawned = specialsSpawned,
            success = true
        };
    }

    protected override List<Vector2Int> GetBossPlacementOptions()
    {
        int centerCol = columns / 2;
        
        return new List<Vector2Int>
        {
            new Vector2Int(centerCol - 1, rows - 2),        // Back center (command position)
            new Vector2Int(centerCol - 1, 1),               // Front tip (leading charge)
            new Vector2Int(centerCol - 3, rows - 2),        // Back left flank
            new Vector2Int(centerCol + 1, rows - 2),        // Back right flank
            new Vector2Int(centerCol - 1, rows / 2),        // Middle command
            new Vector2Int(centerCol - 2, rows / 3)         // Forward command
        };
    }

    protected override List<Vector2Int> GetSpecialPlacementOptions(List<Vector2Int> bossPositions, bool[,] occupied)
    {
        List<Vector2Int> positions = new List<Vector2Int>();
        int centerCol = columns / 2;
        
        // Flanking positions along the spearhead
        positions.Add(new Vector2Int(centerCol - 4, rows / 4));
        positions.Add(new Vector2Int(centerCol + 2, rows / 4));
        positions.Add(new Vector2Int(centerCol - 6, rows / 2));
        positions.Add(new Vector2Int(centerCol + 4, rows / 2));
        positions.Add(new Vector2Int(centerCol - 3, rows - 3));
        positions.Add(new Vector2Int(centerCol + 1, rows - 3));
        positions.Add(new Vector2Int(centerCol - 5, 3 * rows / 4));
        positions.Add(new Vector2Int(centerCol + 3, 3 * rows / 4));
        positions.Add(new Vector2Int(centerCol - 2, 2));               // Near tip
        positions.Add(new Vector2Int(centerCol, 2));                  // Near tip right
        
        return positions;
    }

    protected override void GenerateRegularInvaders(bool[,] occupied, FormationData data)
    {
        int centerCol = columns / 2;
        
        for (int row = 0; row < rows; row++)
        {
            // Calculate width of spearhead at this row (wider at back, narrower at front)
            float frontRatio = (float)row / (rows - 1);
            int width = Mathf.RoundToInt(Mathf.Lerp(3, columns * 0.9f, frontRatio));
            int startCol = centerCol - width / 2;
            
            for (int col = startCol; col < startCol + width; col++)
            {
                if (IsValidPosition(new Vector2Int(col, row)) && Random.value < 0.85f)
                {
                    // Front third: strongest (tip of spear)
                    if (row < rows / 3)
                        data.strongPositions.Add(new Vector2Int(col, row));
                    // Middle third: medium strength (shaft)
                    else if (row < 2 * rows / 3)
                        data.mediumPositions.Add(new Vector2Int(col, row));
                    // Back third: support troops (base)
                    else
                        data.weakPositions.Add(new Vector2Int(col, row));
                }
            }
            
            // Add extra flanking positions for tactical depth
            int flanksWidth = Mathf.RoundToInt(width * 0.8f);
            int flanksStart = centerCol - flanksWidth / 2;
            
            // Left flank
            if (flanksStart - 2 >= 0 && Random.value < 0.6f)
                data.mediumPositions.Add(new Vector2Int(flanksStart - 2, row));
            
            // Right flank  
            if (flanksStart + flanksWidth + 1 < columns && Random.value < 0.6f)
                data.mediumPositions.Add(new Vector2Int(flanksStart + flanksWidth + 1, row));
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