// ===== FortressFormation.cs =====
using UnityEngine;
using System.Collections.Generic;

public class FortressFormation : BaseFormation
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
        
        // Generate fortress ring structure
        GenerateRegularInvaders(occupied, data);
        
        // Place special invaders
        int specialsSpawned = PlaceSpecialInvaders(data.bossPositions, occupied, parentTransform);
        
        // Place all regular invaders
        int regularsSpawned = PlaceAllRegularInvaders(occupied, data, parentTransform);
        
        return new FormationResult
        {
            formationName = "Fortress",
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
            new Vector2Int(center.x - 1, center.y),        // Slightly left
            new Vector2Int(center.x + 1, center.y),        // Slightly right
            new Vector2Int(center.x, center.y - 1),        // Slightly down
            new Vector2Int(center.x, center.y + 1)         // Slightly up
        };
    }

    protected override List<Vector2Int> GetSpecialPlacementOptions(List<Vector2Int> bossPositions, bool[,] occupied)
    {
        List<Vector2Int> positions = new List<Vector2Int>();
        
        foreach (Vector2Int bossPos in bossPositions)
        {
            // Ring around boss - primary defensive positions
            Vector2Int[] offsets = {
                new Vector2Int(-3, 0), new Vector2Int(4, 0),    // Left and right
                new Vector2Int(0, -3), new Vector2Int(0, 4),    // Up and down
                new Vector2Int(-3, -3), new Vector2Int(4, 4),   // Diagonals
                new Vector2Int(-3, 4), new Vector2Int(4, -3),
                new Vector2Int(-5, 0), new Vector2Int(6, 0),    // Extended positions
                new Vector2Int(0, -5), new Vector2Int(0, 6)
            };
            
            foreach (Vector2Int offset in offsets)
            {
                Vector2Int pos = bossPos + offset;
                if (IsValidPosition(pos) && pos.x >= 0 && pos.y >= 0 && 
                    pos.x < columns - 1 && pos.y < rows - 1)
                {
                    positions.Add(pos);
                }
            }
        }
        
        return positions;
    }

    protected override void GenerateRegularInvaders(bool[,] occupied, FormationData data)
    {
        Vector2Int center = new Vector2Int(columns / 2, rows / 2);
        
        // Create concentric rings of defenses
        for (int ring = 1; ring <= Mathf.Max(rows, columns) / 2; ring++)
        {
            List<Vector2Int> ringPositions = GetRingPositions(center, ring);
            
            foreach (Vector2Int pos in ringPositions)
            {
                if (IsValidPosition(pos) && !occupied[pos.y, pos.x])
                {
                    // Closer rings get stronger invaders
                    if (ring <= 2)
                        data.strongPositions.Add(pos);
                    else if (ring <= 4)
                        data.mediumPositions.Add(pos);
                    else
                        data.weakPositions.Add(pos);
                }
            }
        }
    }

    private List<Vector2Int> GetRingPositions(Vector2Int center, int radius)
    {
        List<Vector2Int> positions = new List<Vector2Int>();
        
        for (int col = center.x - radius; col <= center.x + radius; col++)
        {
            for (int row = center.y - radius; row <= center.y + radius; row++)
            {
                int distance = Mathf.Max(Mathf.Abs(col - center.x), Mathf.Abs(row - center.y));
                if (distance == radius)
                {
                    positions.Add(new Vector2Int(col, row));
                }
            }
        }
        
        return positions;
    }

    private int PlaceSpecialInvaders(List<Vector2Int> bossPositions, bool[,] occupied, Transform parent)
    {
        if (!guaranteeBossSpawn || specialPrefab == null) return 0;
        
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
        // Use the base class method that includes gap filling
        FillRemainingGaps(occupied, data, parent, 0.95f); // High density for fortress
        
        // Count total placed
        int placed = 0;
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                if (occupied[row, col]) placed++;
            }
        }
        
        return placed;
    }
    
    protected override int EstimateInvaderType(Vector2Int pos)
    {
        // Fortress-specific logic: distance from center determines strength
        Vector2Int center = new Vector2Int(columns / 2, rows / 2);
        int distance = Mathf.Max(Mathf.Abs(pos.x - center.x), Mathf.Abs(pos.y - center.y));
        
        if (distance <= 2) return 2; // Strong (inner keep)
        if (distance <= 4) return 1; // Medium (middle ring)
        return 0; // Weak (outer ring)
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