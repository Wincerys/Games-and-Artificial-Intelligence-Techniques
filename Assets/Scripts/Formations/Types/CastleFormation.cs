// ===== CastleFormation.cs =====
using UnityEngine;
using System.Collections.Generic;

public class CastleFormation : BaseFormation
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
        
        // Generate castle structure
        GenerateRegularInvaders(occupied, data);
        
        // Place special invaders
        int specialsSpawned = PlaceSpecialInvaders(data.bossPositions, occupied, parentTransform);
        
        // Place all regular invaders
        int regularsSpawned = PlaceAllRegularInvaders(occupied, data, parentTransform);
        
        return new FormationResult
        {
            formationName = "Castle",
            totalInvadersSpawned = bossesSpawned + specialsSpawned + regularsSpawned,
            bossesSpawned = bossesSpawned,
            specialsSpawned = specialsSpawned,
            success = true
        };
    }

    protected override List<Vector2Int> GetBossPlacementOptions()
    {
        return new List<Vector2Int>
        {
            new Vector2Int(columns / 2 - 1, rows / 2 - 1),  // Center courtyard (keep)
            new Vector2Int(2, 2),                           // Northwest tower
            new Vector2Int(columns - 4, 2),                 // Northeast tower
            new Vector2Int(2, rows - 4),                    // Southwest tower
            new Vector2Int(columns - 4, rows - 4),          // Southeast tower
            new Vector2Int(columns / 2 - 1, 2),             // North gatehouse
            new Vector2Int(columns / 2 - 1, rows - 4)       // South gatehouse
        };
    }

    protected override List<Vector2Int> GetSpecialPlacementOptions(List<Vector2Int> bossPositions, bool[,] occupied)
    {
        List<Vector2Int> positions = new List<Vector2Int>();
        
        // Strategic castle defensive positions
        positions.Add(new Vector2Int(columns / 4, rows / 4));           // Inner keep positions
        positions.Add(new Vector2Int(3 * columns / 4 - 2, rows / 4));
        positions.Add(new Vector2Int(columns / 4, 3 * rows / 4 - 2));
        positions.Add(new Vector2Int(3 * columns / 4 - 2, 3 * rows / 4 - 2));
        
        // Gatehouse positions
        positions.Add(new Vector2Int(columns / 2 - 1, 1));              // North gate
        positions.Add(new Vector2Int(columns / 2 - 1, rows - 3));       // South gate
        positions.Add(new Vector2Int(1, rows / 2 - 1));                 // West gate  
        positions.Add(new Vector2Int(columns - 3, rows / 2 - 1));       // East gate
        
        // Corner tower support positions
        positions.Add(new Vector2Int(4, 4));                            // Near NE tower
        positions.Add(new Vector2Int(columns - 6, 4));                  // Near NW tower
        positions.Add(new Vector2Int(4, rows - 6));                     // Near SE tower
        positions.Add(new Vector2Int(columns - 6, rows - 6));           // Near SW tower
        
        // Wall intersections
        positions.Add(new Vector2Int(columns / 2 - 3, rows / 4));       // West inner wall
        positions.Add(new Vector2Int(columns / 2 + 1, rows / 4));       // East inner wall
        positions.Add(new Vector2Int(columns / 2 - 3, 3 * rows / 4));   // West outer wall
        positions.Add(new Vector2Int(columns / 2 + 1, 3 * rows / 4));   // East outer wall
        
        return positions;
    }

    protected override void GenerateRegularInvaders(bool[,] occupied, FormationData data)
    {
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                bool isEdge = (row == 0 || row == rows - 1 || col == 0 || col == columns - 1);
                bool isCorner = (row == 0 || row == rows - 1) && (col == 0 || col == columns - 1);
                bool isNearCorner = IsNearCorner(col, row);
                bool isGatePosition = IsGatePosition(col, row);
                bool isInnerKeep = IsInnerKeep(col, row);
                
                // Corner towers - strongest positions
                if (isCorner && Random.value < 0.95f)
                {
                    data.strongPositions.Add(new Vector2Int(col, row));
                }
                // Near corners - strong support
                else if (isNearCorner && Random.value < 0.85f)
                {
                    data.strongPositions.Add(new Vector2Int(col, row));
                }
                // Gate positions - heavily defended
                else if (isGatePosition && Random.value < 0.9f)
                {
                    data.strongPositions.Add(new Vector2Int(col, row));
                }
                // Outer walls - medium strength
                else if (isEdge && Random.value < 0.85f)
                {
                    data.mediumPositions.Add(new Vector2Int(col, row));
                }
                // Inner keep - medium to weak (protected by walls)
                else if (isInnerKeep && Random.value < 0.6f)
                {
                    data.mediumPositions.Add(new Vector2Int(col, row));
                }
                // Courtyard - weak garrison
                else if (Random.value < 0.5f)
                {
                    data.weakPositions.Add(new Vector2Int(col, row));
                }
            }
        }
    }

    private bool IsNearCorner(int col, int row)
    {
        int cornerRange = 3;
        return (col < cornerRange || col >= columns - cornerRange) && 
               (row < cornerRange || row >= rows - cornerRange);
    }

    private bool IsGatePosition(int col, int row)
    {
        int centerCol = columns / 2;
        int centerRow = rows / 2;
        int gateWidth = 2;
        
        // North/South gates
        bool isNorthSouthGate = (row == 0 || row == rows - 1) && 
                               (col >= centerCol - gateWidth && col <= centerCol + gateWidth);
        
        // East/West gates  
        bool isEastWestGate = (col == 0 || col == columns - 1) && 
                             (row >= centerRow - gateWidth && row <= centerRow + gateWidth);
        
        return isNorthSouthGate || isEastWestGate;
    }

    private bool IsInnerKeep(int col, int row)
    {
        int keepMargin = Mathf.Min(columns, rows) / 4;
        return col >= keepMargin && col < columns - keepMargin && 
               row >= keepMargin && row < rows - keepMargin;
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