// ===== DiamondFormation.cs =====
using UnityEngine;
using System.Collections.Generic;

public class DiamondFormation : BaseFormation
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
            formationName = "Diamond",
            totalInvadersSpawned = bossesSpawned + specialsSpawned + regularsSpawned,
            bossesSpawned = bossesSpawned,
            specialsSpawned = specialsSpawned,
            success = true
        };
    }

    protected override List<Vector2Int> GetBossPlacementOptions()
    {
        Vector2Int center = new Vector2Int(columns / 2, rows / 2);
        
        return new List<Vector2Int>
        {
            center,                                         // Perfect center
            new Vector2Int(center.x - 1, center.y),        // Left of center
            new Vector2Int(center.x + 1, center.y),        // Right of center
            new Vector2Int(center.x, center.y - 1),        // Below center
            new Vector2Int(center.x, center.y + 1),        // Above center
            new Vector2Int(center.x - 2, center.y),        // Further left
            new Vector2Int(center.x + 2, center.y)         // Further right
        };
    }

    protected override List<Vector2Int> GetSpecialPlacementOptions(List<Vector2Int> bossPositions, bool[,] occupied)
    {
        List<Vector2Int> positions = new List<Vector2Int>();
        Vector2Int center = new Vector2Int(columns / 2, rows / 2);
        
        // Diamond cardinal and diagonal positions
        positions.Add(new Vector2Int(center.x - 3, center.y));         // West
        positions.Add(new Vector2Int(center.x + 3, center.y));         // East
        positions.Add(new Vector2Int(center.x, center.y - 3));         // South
        positions.Add(new Vector2Int(center.x, center.y + 3));         // North
        positions.Add(new Vector2Int(center.x - 2, center.y - 2));     // Southwest
        positions.Add(new Vector2Int(center.x + 2, center.y - 2));     // Southeast
        positions.Add(new Vector2Int(center.x - 2, center.y + 2));     // Northwest
        positions.Add(new Vector2Int(center.x + 2, center.y + 2));     // Northeast
        positions.Add(new Vector2Int(center.x - 4, center.y - 1));     // Extended positions
        positions.Add(new Vector2Int(center.x + 4, center.y + 1));
        positions.Add(new Vector2Int(center.x - 1, center.y - 4));
        positions.Add(new Vector2Int(center.x + 1, center.y + 4));
        
        return positions;
    }

    protected override void GenerateRegularInvaders(bool[,] occupied, FormationData data)
    {
        Vector2Int center = new Vector2Int(columns / 2, rows / 2);
        int maxRadius = Mathf.Min(columns, rows) / 2;
        
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                // Use Manhattan distance for diamond shape
                int distance = Mathf.Abs(col - center.x) + Mathf.Abs(row - center.y);
                
                if (distance <= maxRadius && Random.value < 0.8f)
                {
                    InvaderType type;
                    
                    // Inner diamond: strong
                    if (distance < maxRadius / 3)
                        type = InvaderType.Strong;
                    // Middle diamond: medium
                    else if (distance < 2 * maxRadius / 3)
                        type = InvaderType.Medium;
                    // Outer diamond: weak
                    else
                        type = InvaderType.Weak;
                    
                    AddToFormationData(new Vector2Int(col, row), type, data);
                }
                
                // Add some outer diamond spikes for variety
                if (distance == maxRadius + 1 && Random.value < 0.3f)
                {
                    AddToFormationData(new Vector2Int(col, row), InvaderType.Weak, data);
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