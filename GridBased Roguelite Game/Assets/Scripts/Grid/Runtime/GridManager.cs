using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int _gridWidth = 50;
    [SerializeField] private int _gridHeight = 50;
    
    [Header("Generation Settings")]
    [SerializeField] private DungeonDefinition _dungeonToGenerate;
    
    [Header("Components")]
    [SerializeField] private GridVisualizer _visualizer;
    [SerializeField] private EffectPool _effectPool;
    
    [Header("Debug")]
    [SerializeField] private bool _regenerateOnStart = true;
    
    [SerializeField] private GridData _gridData;
    [SerializeField] private GameObject _itemParent;
    [SerializeField] private GameObject _enemyParent;
    [SerializeField] private int _currentFloor;
    private GridGenerator _generator;
    public GridData Data => _gridData;
    public List<Room> Rooms => _gridData?.Rooms;
    
    public System.Action OnStairsUsed;
    public System.Action<EffectData> OnEffectTriggered;

    void Start()
    {
        if (_regenerateOnStart)
        {
            _currentFloor = 1;
            GenerateNewDungeon();
        }
    }

    public void GoToNextFloor()
    {
        if (_dungeonToGenerate == null) return;
    
        // Increment floor
        if (_currentFloor < _dungeonToGenerate.FloorCount)
        {
            _currentFloor++;
        
            // Regenerate dungeon for next floor
            GenerateNewDungeon();
        
            Debug.Log($"Descended to floor {_currentFloor}");
        }
        else
        {
            // Reached the bottom - handle boss or victory
            Debug.Log("Reached the final floor!");
            SpawnBoss();
        }
    }
    
    public void GenerateNewDungeon()
    {
        // Generate the dungeon data
        _generator = new GridGenerator();
        _gridData = _generator.Generate(_gridWidth, _gridHeight, _dungeonToGenerate.GenerationSettings);

        // Reset items
        foreach (Transform child in _itemParent.transform)
        {
            Destroy(child.gameObject);
        }
        
        foreach (Transform child in _enemyParent.transform)
        {
            Destroy(child.gameObject);
        }
        
        // Place special tiles
        PlaceStairs();
        PlaceEffectTiles();
        
        // Visualize
        if (_visualizer != null)
        {
            _visualizer.Visualize(_gridData, _dungeonToGenerate.DungeonVisuals);
        }
        
        // Place items using this dungeon's loot table
        if (_dungeonToGenerate.LootTable != null)
        {
            PlaceItems(_dungeonToGenerate.LootTable);
        }
        
        // Position player
        PositionPlayer(GameObject.Find("Player"));
        SpawnEnemies(_dungeonToGenerate.EnemyPrefabs, _dungeonToGenerate.EnemiesPerFloor);
        
        Debug.Log($"Dungeon generated with {Rooms.Count} rooms");
    }
    
    private void PlaceItems(DungeonLootTable lootTable)
    {
        if (lootTable == null)
        {
            Debug.LogWarning("No loot table assigned!");
            return;
        }
        
        if (!lootTable.IsValid()) return;
        
        int totalItemsPlaced = 0;
        
        foreach (var room in _gridData.Rooms)
        {
            // Skip rooms with no floor tiles
            if (room.FloorTiles.Count == 0) continue;
            
            // Get available tiles in this room
            List<Vector2Int> availableTiles = GetAvailableTilesInRoom(room);
            
            if (availableTiles.Count == 0) continue;
            
            // Generate items for this room
            List<ItemData> itemDataList = lootTable.GenerateRoomItems();
            
            if (itemDataList.Count == 0) continue;
            
            // Place as many items as we can (limited by available tiles)
            int itemsToPlace = Mathf.Min(itemDataList.Count, availableTiles.Count);
            
            for (int i = 0; i < itemsToPlace; i++)
            {
                Vector2Int tilePos = availableTiles[i];
                ItemData itemData = itemDataList[i];
                
                // Create the item GameObject in the world
                Item worldItem = CreateWorldItem(itemData, tilePos);
                
                // Assign to tile
                _gridData.Tiles[tilePos.x, tilePos.y].Item = worldItem;
                
                totalItemsPlaced++;
            }
        }
        
        Debug.Log($"Placed {totalItemsPlaced} items in dungeon using {lootTable.name}");
    }
    
    private List<Vector2Int> GetAvailableTilesInRoom(Room room)
    {
        List<Vector2Int> available = new List<Vector2Int>();
        
        foreach (var tilePos in room.FloorTiles)
        {
            var tile = _gridData.Tiles[tilePos.x, tilePos.y];
            
            // Valid placement tile:
            // - No item already
            // - No occupant (player/enemy)
            // - Is floor type (not stairs/effect/wall)
            if (!tile.HasItem && 
                tile.Occupant == null && 
                tile.Type == TileType.Floor)
            {
                available.Add(tilePos);
            }
        }
        
        // Shuffle for random placement
        ShuffleList(available);
        
        return available;
    }
    
    private Item CreateWorldItem(ItemData itemData, Vector2Int position)
    {
        // Create visual representation in the world
        GameObject itemObj = new GameObject(itemData.ItemName)
        {
            transform =
            {
                position = new Vector3(position.x, position.y, 0),
                parent = _itemParent.transform
            }
        };

        // Add sprite renderer if you have icons
        if (itemData.Icon != null)
        {
            SpriteRenderer renderer = itemObj.AddComponent<SpriteRenderer>();
            renderer.sprite = itemData.Icon;
            renderer.sortingOrder = 1;
        }
    
        // Add collider for interaction
        BoxCollider2D collider = itemObj.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
    
        // Add the Item component
        Item item = itemObj.AddComponent<Item>();
    
        // Calculate stack size based on item's Min/Max Stack settings
        int stackSize = 1;
        if (itemData.Stackable)
        {
            stackSize = Random.Range(itemData.MinSpawnStack, itemData.MaxSpawnStack + 1);
        }
    
        // Initialize with the ItemData and stack size
        item.Initialize(itemData, stackSize);
    
        return item;
    }
    
    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rand = Random.Range(i, list.Count);
            (list[i], list[rand]) = (list[rand], list[i]);
        }
    }

    private void PlaceStairs()
    {
        if (Rooms.Count == 0) return;

        Room stairsRoom = Rooms[Random.Range(0, Rooms.Count)];
        if (stairsRoom.FloorTiles.Count > 0)
        {
            Vector2Int stairsPos = stairsRoom.FloorTiles[Random.Range(0, stairsRoom.FloorTiles.Count)];
            _gridData.Tiles[stairsPos.x, stairsPos.y].Type = TileType.Stairs;
            _gridData.StairsPosition = stairsPos;
            _visualizer?.UpdateTileVisual(stairsPos.x, stairsPos.y, TileType.Stairs);
        }
    }

    private void PlaceEffectTiles()
    {
        if (_effectPool == null) return;
    
        foreach (Room room in Rooms)
        {
            List<Vector2Int> availableTiles = new List<Vector2Int>(room.FloorTiles);
            
            if (_gridData.StairsPosition.HasValue && room.Contains(_gridData.StairsPosition.Value))
            {
                availableTiles.Remove(_gridData.StairsPosition.Value);
            }
        
            int effectCount = Mathf.Min(
                Random.Range(0, 4), 
                availableTiles.Count
            );
        
            for (int i = 0; i < effectCount; i++)
            {
                int randomIndex = Random.Range(0, availableTiles.Count);
                Vector2Int pos = availableTiles[randomIndex];
                availableTiles.RemoveAt(randomIndex);
            
                _gridData.Tiles[pos.x, pos.y].Type = TileType.Effect;
                _gridData.Tiles[pos.x, pos.y].GroundEffect = _effectPool.GetRandomEffect();
                _visualizer?.UpdateTileVisual(pos.x, pos.y, TileType.Effect);
            }
        }
    }

    // Public methods for runtime queries/modifications
    public Tile GetTile(int x, int y) => 
        _gridData?.IsInBounds(x, y) == true ? _gridData.Tiles[x, y] : null;

    public bool IsWalkable(int x, int y)
    {
        Tile tile = GetTile(x, y);
        return tile != null && tile.IsWalkable && tile.Occupant == null;
    }

    public void SetOccupant(int x, int y, GameObject occupant)
    {
        Tile tile = GetTile(x, y);
        if (tile != null) tile.Occupant = occupant;
    }

    public GameObject GetOccupant(int x, int y) => GetTile(x, y)?.Occupant;

    // Position player, spawn enemies, etc.
    public void PositionPlayer(GameObject playerObject)
    {
        if (playerObject == null)
        {
            Debug.LogError("No player object to position!");
            return;
        }
    
        Vector2Int? spawnPos = GetRandomSpawnPosition();
    
        if (!spawnPos.HasValue)
        {
            Debug.LogError("No valid spawn position found!");
            return;
        }
    
        // Remove player from old grid position if any
        Unit playerUnit = playerObject.GetComponent<Unit>();
        if (playerUnit != null)
        {
            var oldPos = playerUnit.GetGridPosition();
            if (oldPos.HasValue)
            {
                SetOccupant(oldPos.Value.x, oldPos.Value.y, null);
            }
        }
    
        // Move player to new position
        playerObject.transform.position = new Vector3(spawnPos.Value.x, spawnPos.Value.y, 0);
    
        // Set up unit for dungeon if needed
        if (playerUnit != null)
        {
            playerUnit.EnterDungeon(this);
        }
    
        // Register on grid
        SetOccupant(spawnPos.Value.x, spawnPos.Value.y, playerObject);
    
        Debug.Log($"Player positioned at {spawnPos.Value}");
    }
    
    public void SpawnEnemies(List<GameObject> enemyPrefabs, int count)
    {
        if (enemyPrefabs == null || enemyPrefabs.Count == 0) return;

        int spawned = 0;
        int attempts = 0;
        int maxAttempts = 100;

        // Track current floor number - you'll need to pass this in or track it globally
        int currentFloor = _currentFloor; // You'll need to implement this

        while (spawned < count && attempts < maxAttempts)
        {
            Vector2Int? spawnPos = GetRandomSpawnPosition();
            attempts++;
        
            if (!spawnPos.HasValue) continue;
        
            // Pick random enemy prefab
            GameObject enemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
        
            // Get the enemy's base stats
            Unit enemyUnitPrefab = enemyPrefab.GetComponent<Unit>();
            if (enemyUnitPrefab == null || enemyUnitPrefab.BaseStats == null)
            {
                Debug.LogError($"Enemy prefab {enemyPrefab.name} missing Unit component or BaseStats!");
                continue;
            }
        
            // Calculate level for this enemy based on current floor
            int enemyLevel = GetEnemyLevelForFloor(currentFloor);
        
            // Spawn enemy
            GameObject enemy = Instantiate(enemyPrefab,
                new Vector3(spawnPos.Value.x, spawnPos.Value.y, 0), 
                Quaternion.identity, _enemyParent.transform);
        
            // Set up unit
            Unit enemyUnit = enemy.GetComponent<Unit>();
            if (enemyUnit != null)
            {
                enemyUnit.SetUnitType(UnitType.Monster);
                enemyUnit.EnterDungeon(this);
            
                // CRITICAL: Set the enemy's level!
                var expSystem = enemyUnit.Experience;
                if (expSystem != null)
                {
                    expSystem.SetLevel(enemyLevel);
                    Debug.Log($"Spawned {enemyUnit.UnitName} at level {enemyLevel} on floor {currentFloor}");
                }
            }
        
            // Register on grid
            SetOccupant(spawnPos.Value.x, spawnPos.Value.y, enemy);
            
            EnemyAI ai = enemy.GetComponent<EnemyAI>();
            if (ai != null)
            {
                ai.Initialize(this);
            }
        
            spawned++;
        }
    
        Debug.Log($"Spawned {spawned} enemies on floor {currentFloor}");
    }

    private int GetEnemyLevelForFloor(int floorNumber)
    {
        if (_dungeonToGenerate == null) return 1;

        // Check if we have floor ranges defined
        if (_dungeonToGenerate.FloorRanges == null || _dungeonToGenerate.FloorRanges.Length == 0)
        {
            Debug.LogWarning("No floor ranges set in dungeon definition! Using default level 1.");
            return 1;
        }
    
        // Find which range this floor belongs to
        foreach (var floorRange in _dungeonToGenerate.FloorRanges)
        {
            if (floorNumber >= floorRange.StartFloorNumber && floorNumber <= floorRange.EndFloorNumber)
            {
                int level = Random.Range(floorRange.MinLevel, floorRange.MaxLevel + 1);
                Debug.Log($"Floor {floorNumber} using range {floorRange.StartFloorNumber}-{floorRange.EndFloorNumber}, spawned level {level}");
                return level;
            }
        }
    
        // Fallback to first range if floor not found
        Debug.LogWarning($"Floor {floorNumber} not found in any range, using first range");
        var firstRange = _dungeonToGenerate.FloorRanges[0];
        return Random.Range(firstRange.MinLevel, firstRange.MaxLevel + 1);
    }
    
    public Vector2Int? GetRandomSpawnPosition(bool avoidStairs = true)
    {
        if (Rooms == null || Rooms.Count == 0) return null;

        // Try a few times to find a valid position
        int attempts = 0;
        int maxAttempts = 50;
    
        while (attempts < maxAttempts)
        {
            // Pick random room
            Room room = Rooms[Random.Range(0, Rooms.Count)];
        
            if (room.FloorTiles.Count == 0) 
            {
                attempts++;
                continue;
            }
        
            // Pick random tile in that room
            Vector2Int pos = room.FloorTiles[Random.Range(0, room.FloorTiles.Count)];
        
            // Check if valid
            if (Data.Tiles[pos.x, pos.y].Occupant == null)
            {
                if (avoidStairs && Data.Tiles[pos.x, pos.y].Type == TileType.Stairs)
                {
                    attempts++;
                    continue;
                }
            
                return pos;
            }
        
            attempts++;
        }
    
        // Fallback - linear search through all tiles
        foreach (var room in Rooms)
        {
            foreach (Vector2Int pos in room.FloorTiles)
            {
                if (Data.Tiles[pos.x, pos.y].Occupant == null)
                {
                    if (avoidStairs && Data.Tiles[pos.x, pos.y].Type == TileType.Stairs)
                        continue;
                    return pos;
                }
            }
        }
    
        return null;
    }
    
    private void SpawnBoss()
    {
        if (_dungeonToGenerate.BossPrefab == null)
        {
            Debug.LogWarning("No boss prefab assigned for this dungeon!");
            return;
        }
    
        // Find a suitable position for the boss
        Vector2Int? bossPos = GetRandomSpawnPosition(true);
        if (bossPos.HasValue)
        {
            GameObject boss = Instantiate(_dungeonToGenerate.BossPrefab, 
                new Vector3(bossPos.Value.x, bossPos.Value.y, 0), 
                Quaternion.identity);
        
            // Set up boss unit
            Unit bossUnit = boss.GetComponent<Unit>();
            if (bossUnit != null)
            {
                bossUnit.SetUnitType(UnitType.Monster);
                bossUnit.EnterDungeon(this);
            }
        
            SetOccupant(bossPos.Value.x, bossPos.Value.y, boss);
            Debug.Log("Boss spawned!");
        }
    }
    
    void OnDrawGizmos()
    {
        if (_gridData?.Tiles == null) return;
        
        for (int x = 0; x < _gridData.Width; x++)
        {
            for (int y = 0; y < _gridData.Height; y++)
            {
                Vector3 pos = new Vector3(x, y, 0);
                
                // Base color by tile type
                Color color = _gridData.Tiles[x, y].Type switch
                {
                    TileType.Wall => Color.red,
                    TileType.Floor => Color.green,
                    TileType.Effect => Color.yellow,
                    TileType.Stairs => Color.cyan,
                    _ => Color.white
                };
                
                // Draw tile
                Gizmos.color = color;
                Gizmos.DrawCube(pos, Vector3.one * 0.9f);
                
                // Draw item indicator
                if (_gridData.Tiles[x, y].HasItem)
                {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawSphere(pos, 0.2f);
                }
            }
        }
    }
}