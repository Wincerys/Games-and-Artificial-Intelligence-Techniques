// ===== ScatteredFormation.cs =====
using UnityEngine;
using System.Collections.Generic;

public class ScatteredFormation : BaseFormation
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
            formationName = "Scattered",
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
            new Vector2Int(columns / 4, rows / 4),
            new Vector2Int(3 * columns / 4 - 2, rows / 4),
            new Vector2Int(columns / 4, 3 * rows / 4 - 2),
            new Vector2Int(3 * columns / 4 - 2, 3 * rows / 4 - 2),
            new Vector2Int(columns / 2 - 1, rows / 2 - 1),
            new Vector2Int(columns / 6, rows / 2),
            new Vector2Int(5 * columns / 6 - 2, rows / 2)
        };
    }

    protected override List<Vector2Int> GetSpecialPlacementOptions(List<Vector2Int> bossPositions, bool[,] occupied)
    {
        List<Vector2Int> positions = new List<Vector2Int>();
        for (int i = 0; i < 15; i++)
        {
            positions.Add(new Vector2Int(Random.Range(1, columns - 2), Random.Range(1, rows - 2)));
        }
        return positions;
    }

    protected override void GenerateRegularInvaders(bool[,] occupied, FormationData data)
    {
        int clusterCount = Random.Range(4, 8);
        
        for (int cluster = 0; cluster < clusterCount; cluster++)
        {
            Vector2Int clusterCenter = new Vector2Int(Random.Range(2, columns - 2), Random.Range(2, rows - 2));
            InvaderType clusterType = (InvaderType)Random.Range(0, 3);
            int clusterSize = Random.Range(3, 8);
            
            List<Vector2Int> clusterPositions = GetClusterPositions(clusterCenter, clusterSize);
            
            foreach (Vector2Int pos in clusterPositions)
            {
                if (IsValidPosition(pos))
                {
                    switch (clusterType)
                    {
                        case InvaderType.Strong: data.strongPositions.Add(pos); break;
                        case InvaderType.Medium: data.mediumPositions.Add(pos); break;
                        case InvaderType.Weak: data.weakPositions.Add(pos); break;
                    }
                }
            }
        }
    }

    private List<Vector2Int> GetClusterPositions(Vector2Int center, int size)
    {
        List<Vector2Int> positions = new List<Vector2Int>();
        int radius = Mathf.CeilToInt(Mathf.Sqrt(size));
        
        for (int col = center.x - radius; col <= center.x + radius; col++)
        {
            for (int row = center.y - radius; row <= center.y + radius; row++)
            {
                float distance = Vector2Int.Distance(center, new Vector2Int(col, row));
                if (distance <= radius && Random.value < 0.7f)
                {
                    positions.Add(new Vector2Int(col, row));
                    if (positions.Count >= size) break;
                }
            }
            if (positions.Count >= size) break;
        }
        
        return positions;
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