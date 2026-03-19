using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "Dungeon", menuName = "Dungeon/Dungeon Definition")]
public class DungeonDefinition : ScriptableObject
{
    public string DungeonName;
    public int FloorCount = 5;
    public GenerationSettings GenerationSettings;
    public DungeonLootTable LootTable; // <-- The loot table for this dungeon
    public GameObject BossPrefab;
    
    public List<GameObject> EnemyPrefabs;
    public int EnemiesPerFloor; // Maybe make this a running spawner to not overwhelm players.
    public FloorLevelRange[] FloorRanges;

    [Header("Visual Settings")]
    public DungeonVisuals DungeonVisuals; // <-- Visuals for this dungeon
}

[System.Serializable]
public class DungeonVisuals
{
    [Header("Tilemap Visualization")] 
    public Tilemap FloorTilemap;
    public Tilemap WallTilemap;
    public Tilemap EffectTilemap;

    [Header("Tiles")] public TileBase FloorTile;
    public TileBase WallTile;
    public TileBase EffectTile;
    public TileBase StairsTile;
}

[System.Serializable]
public class FloorLevelRange
{
    public int StartFloorNumber;
    public int EndFloorNumber;
    public int MinLevel;
    public int MaxLevel;
}
