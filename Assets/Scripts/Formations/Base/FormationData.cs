// ===== FormationData.cs =====
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class FormationData
{
    public List<Vector2Int> bossPositions = new List<Vector2Int>();
    public List<Vector2Int> specialPositions = new List<Vector2Int>();
    public List<Vector2Int> strongPositions = new List<Vector2Int>();
    public List<Vector2Int> mediumPositions = new List<Vector2Int>();
    public List<Vector2Int> weakPositions = new List<Vector2Int>();
}

[System.Serializable]
public class FormationSettings
{
    public int rows;
    public int columns;
    public float formationWidth;
    public float formationHeight;
    public bool guaranteeBossSpawn;
    public int guaranteedSpecialCount;
    public Invader bossPrefab;
    public Invader specialPrefab;
    public Invader[] regularPrefabs;
    public float formationDensity = 0.75f;
    public float clusteringStrength = 0.6f;
}

[System.Serializable]
public class FormationResult
{
    public string formationName;
    public int totalInvadersSpawned;
    public int bossesSpawned;
    public int specialsSpawned;
    public bool success;
    public string errorMessage;
}

public enum InvaderType { Weak = 0, Medium = 1, Strong = 2 }

