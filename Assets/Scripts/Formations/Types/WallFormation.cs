// ===== WallFormation.cs =====
using UnityEngine;
using System.Collections.Generic;

public class WallFormation : BaseFormation
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
            formationName = "Wall",
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
            new Vector2Int(columns / 2 - 1, rows / 2 - 1),  // Center intersection
            new Vector2Int(columns / 4 - 1, rows / 4 - 1),  // Quarter intersection
            new Vector2Int(3 * columns / 4 - 1, rows / 4 - 1), // Three-quarter intersection
            new Vector2Int(2, rows - 4),                    // Wall endpoint
            new Vector2Int(columns - 4, 2),                 // Wall endpoint
            new Vector2Int(columns - 4, rows - 4),          // Corner position
            new Vector2Int(2, 2)                            // Corner position
        };
    }

    protected override List<Vector2Int> GetSpecialPlacementOptions(List<Vector2Int> bossPositions, bool[,] occupied)
    {
        List<Vector2Int> positions = new List<Vector2Int>();
        
        // Wall intersection and endpoint positions
        positions.Add(new Vector2Int(columns / 4, rows / 4));
        positions.Add(new Vector2Int(3 * columns / 4 - 2, rows / 4));
        positions.Add(new Vector2Int(columns / 4, 3 * rows / 4 - 2));
        positions.Add(new Vector2Int(3 * columns / 4 - 2, 3 * rows / 4 - 2));
        positions.Add(new Vector2Int(columns / 2 - 1, rows / 2 - 1));
        positions.Add(new Vector2Int(2, 2));
        positions.Add(new Vector2Int(columns - 4, rows - 4));
        positions.Add(new Vector2Int(columns / 2 - 1, 2));
        positions.Add(new Vector2Int(columns / 2 - 1, rows - 4));
        positions.Add(new Vector2Int(2, rows / 2 - 1));
        positions.Add(new Vector2Int(columns - 4, rows / 2 - 1));
        
        return positions;
    }

    protected override void GenerateRegularInvaders(bool[,] occupied, FormationData data)
    {
        int wallCount = Random.Range(2, 5);
        
        for (int wall = 0; wall < wallCount; wall++)
        {
            bool isVertical = Random.value > 0.5f;
            InvaderType wallType = (InvaderType)Random.Range(0, 3);
            
            if (isVertical)
            {
                int col = Random.Range(2, columns - 2);
                int startRow = Random.Range(0, rows / 2);
                int endRow = Random.Range(rows / 2, rows);
                
                for (int row = startRow; row < endRow; row++)
                {
                    AddToFormationData(new Vector2Int(col, row), wallType, data);
                    
                    // Add some thickness to walls
                    if (col + 1 < columns && Random.value < 0.6f)
                        AddToFormationData(new Vector2Int(col + 1, row), wallType, data);
                    if (col - 1 >= 0 && Random.value < 0.6f)
                        AddToFormationData(new Vector2Int(col - 1, row), wallType, data);
                }
            }
            else
            {
                int row = Random.Range(2, rows - 2);
                int startCol = Random.Range(0, columns / 2);
                int endCol = Random.Range(columns / 2, columns);
                
                for (int col = startCol; col < endCol; col++)
                {
                    AddToFormationData(new Vector2Int(col, row), wallType, data);
                    
                    // Add some thickness to walls
                    if (row + 1 < rows && Random.value < 0.6f)
                        AddToFormationData(new Vector2Int(col, row + 1), wallType, data);
                    if (row - 1 >= 0 && Random.value < 0.6f)
                        AddToFormationData(new Vector2Int(col, row - 1), wallType, data);
                }
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