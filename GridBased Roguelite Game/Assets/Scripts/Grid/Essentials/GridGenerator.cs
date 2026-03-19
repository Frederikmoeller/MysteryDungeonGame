using System.Collections.Generic;
using UnityEngine;

public class GridGenerator
{
    private GridData _data;
    private Dictionary<int, List<int>> _roomConnections = new();
    
    [Header("Room Generation")] 
    private int _numberOfRooms = 10;
    private int _minRoomSize = 3;
    private int _maxRoomSize = 15;
    private int _roomPadding = 2;
    
    [Header("Corridor Settings")]
    private float _extraConnectionChance = 0.6f;
    private int _maxConnectionsPerRoom = 4;

    public GridData Generate(int width, int height, GenerationSettings settings)
    {
        _data = new GridData(width, height);
        ApplySettings(settings);

        GenerateRooms();
        GenerateCorridors();

        return _data;
    }

    private void ApplySettings(GenerationSettings settings)
    {
        _numberOfRooms = settings.NumberOfRooms;
        _minRoomSize = settings.MinRoomSize;
        _maxRoomSize = settings.MaxRoomSize;
        _roomPadding = settings.RoomPadding;
        _extraConnectionChance = settings.ExtraConnectionChance;
        _maxConnectionsPerRoom = settings.MaxConnectionsPerRoom;
    }
    
    private void GenerateRooms()
    {
        _data.Rooms.Clear();
        _roomConnections.Clear();
        int attempts = 0;
        int maxAttempts = 1000;
        
        while (_data.Rooms.Count < _numberOfRooms && attempts < maxAttempts)
        {
            int roomWidth = Random.Range(_minRoomSize, _maxRoomSize + 1);
            int roomHeight = Random.Range(_minRoomSize, _maxRoomSize + 1);

            int x = Random.Range(1, _data.Width - roomWidth - 1);
            int y = Random.Range(1, _data.Height - roomHeight - 1);

            RectInt newRoomRect = new RectInt(x, y, roomWidth, roomHeight);
            
            bool overlaps = false;
            foreach (Room room in _data.Rooms)
            {
                if (RectIntersects(newRoomRect, room.Rect, _roomPadding))
                {
                    overlaps = true;
                    break;
                }
            }

            if (!overlaps)
            {
                Room newRoom = new Room(newRoomRect);
                _data.Rooms.Add(newRoom);
                _roomConnections[newRoom.Id] = new List<int>();
                
                for (int rx = x; rx < x + roomWidth; rx++)
                {
                    for (int ry = y; ry < y + roomHeight; ry++)
                    {
                        _data.Tiles[rx, ry].Type = TileType.Floor;
                    }
                }
            }
            attempts++;
        }
        
        foreach (Room room in _data.Rooms)
        {
            room.FloorTiles = GetFloorTilesInRoom(room);
        }
    }
    
    private void GenerateCorridors()
    {
        if (_data.Rooms.Count < 2) return;
        
        _data.CorridorTiles.Clear();
        
        List<RoomConnection> mstConnections = CreateMinimumSpanningTree();
        
        foreach (var connection in mstConnections)
        {
            CreateCorridorBetweenRooms(_data.Rooms[connection.RoomA], _data.Rooms[connection.RoomB]);
            RegisterConnection(connection.RoomA, connection.RoomB);
        }
        
        AddExtraConnections();
    }
    
    private List<RoomConnection> CreateMinimumSpanningTree()
    {
        // Calculate all possible connections between rooms
        List<RoomConnection> allConnections = new List<RoomConnection>();
        for (int i = 0; i < _data.Rooms.Count; i++)
        {
            for (int j = i + 1; j < _data.Rooms.Count; j++)
            {
                float distance = Vector2Int.Distance(_data.Rooms[i].Center, _data.Rooms[j].Center);
                allConnections.Add(new RoomConnection(i, j, distance));
            }
        }
        
        // Sort by distance
        allConnections.Sort((a, b) => a.Distance.CompareTo(b.Distance));
        
        // Kruskal's algorithm for minimum spanning tree
        DisjointSet ds = new DisjointSet(_data.Rooms.Count);
        List<RoomConnection> mstConnections = new List<RoomConnection>();
        
        foreach (var connection in allConnections)
        {
            if (ds.Find(connection.RoomA) != ds.Find(connection.RoomB))
            {
                ds.Union(connection.RoomA, connection.RoomB);
                mstConnections.Add(connection);
            }
        }
        
        return mstConnections;
    }
    
    private void AddExtraConnections()
    {
        // Create a list of all possible connections that aren't already made
        List<RoomConnection> possibleExtraConnections = new List<RoomConnection>();
        
        for (int i = 0; i < _data.Rooms.Count; i++)
        {
            for (int j = i + 1; j < _data.Rooms.Count; j++)
            {
                // Check if connection doesn't already exist
                if (!_roomConnections[i].Contains(j) && !_roomConnections[j].Contains(i))
                {
                    // Check if both rooms can accept more connections
                    if (_roomConnections[i].Count < _maxConnectionsPerRoom && 
                        _roomConnections[j].Count < _maxConnectionsPerRoom)
                    {
                        float distance = Vector2Int.Distance(_data.Rooms[i].Center, _data.Rooms[j].Center);
                        possibleExtraConnections.Add(new RoomConnection(i, j, distance));
                    }
                }
            }
        }
        
        // Sort by distance (prefer closer connections for extra corridors)
        possibleExtraConnections.Sort((a, b) => a.Distance.CompareTo(b.Distance));
        
        // Try to add extra connections
        foreach (var connection in possibleExtraConnections)
        {
            // Roll the dice for this connection
            if (Random.value < _extraConnectionChance)
            {
                // Check if we can create a valid corridor
                if (CreateCorridorBetweenRooms(_data.Rooms[connection.RoomA], _data.Rooms[connection.RoomB]))
                {
                    RegisterConnection(connection.RoomA, connection.RoomB);
                    
                    // Stop if we've reached a good number of extra connections
                    if (GetTotalConnections() >= _data.Rooms.Count * 1.5f) break;
                }
            }
        }
    }
    
    private bool CreateCorridorBetweenRooms(Room roomA, Room roomB)
    {
        Vector2Int startPoint = GetRandomDoorPosition(roomA);
        Vector2Int endPoint = GetRandomDoorPosition(roomB);
        
        List<Vector2Int> corridorPath = new List<Vector2Int>();
        
        // Try different path patterns
        bool pathCreated = false;
        int attempts = 0;
        int maxAttempts = 15;
        
        while (!pathCreated && attempts < maxAttempts)
        {
            corridorPath.Clear();
            
            // Randomly choose path pattern
            int pattern = Random.Range(0, 4);
            switch (pattern)
            {
                case 0: // Horizontal then vertical
                    AddHorizontalPath(startPoint.x, endPoint.x, startPoint.y, corridorPath);
                    AddVerticalPath(startPoint.y, endPoint.y, endPoint.x, corridorPath);
                    break;
                case 1: // Vertical then horizontal
                    AddVerticalPath(startPoint.y, endPoint.y, startPoint.x, corridorPath);
                    AddHorizontalPath(startPoint.x, endPoint.x, endPoint.y, corridorPath);
                    break;
                case 2: // Two-turn path (horizontal, vertical, horizontal)
                    int midX1 = Random.Range(Mathf.Min(startPoint.x, endPoint.x), Mathf.Max(startPoint.x, endPoint.x));
                    AddHorizontalPath(startPoint.x, midX1, startPoint.y, corridorPath);
                    AddVerticalPath(startPoint.y, endPoint.y, midX1, corridorPath);
                    AddHorizontalPath(midX1, endPoint.x, endPoint.y, corridorPath);
                    break;
                case 3: // Two-turn path (vertical, horizontal, vertical)
                    int midY1 = Random.Range(Mathf.Min(startPoint.y, endPoint.y), Mathf.Max(startPoint.y, endPoint.y));
                    AddVerticalPath(startPoint.y, midY1, startPoint.x, corridorPath);
                    AddHorizontalPath(startPoint.x, endPoint.x, midY1, corridorPath);
                    AddVerticalPath(midY1, endPoint.y, endPoint.x, corridorPath);
                    break;
            }
            
            // Check if path respects spacing rules
            if (PathRespectsSpacing(corridorPath))
            {
                pathCreated = true;
            }
            else
            {
                attempts++;
            }
        }
        
        // If we couldn't create a path with spacing, try a direct Manhattan path as fallback
        if (!pathCreated)
        {
            corridorPath.Clear();
            // Simple Manhattan path as fallback
            AddHorizontalPath(startPoint.x, endPoint.x, startPoint.y, corridorPath);
            AddVerticalPath(startPoint.y, endPoint.y, endPoint.x, corridorPath);
        }
        
        // Carve the corridor
        bool corridorCreated = false;
        foreach (Vector2Int tile in corridorPath)
        {
            if (!IsPartOfRoom(tile.x, tile.y))
            {
                _data.Tiles[tile.x, tile.y].Type = TileType.Floor;
                if (!_data.CorridorTiles.Contains(tile))
                {
                    _data.CorridorTiles.Add(tile);
                }
                corridorCreated = true;
            }
        }
        
        return corridorCreated;
    }
    
    private Vector2Int GetRandomDoorPosition(Room room)
    {
        // Choose a random point on the room's perimeter for the door
        int side = Random.Range(0, 4);
        
        switch (side)
        {
            case 0: // Top side
                return new Vector2Int(
                    Random.Range(room.Rect.x, room.Rect.x + room.Rect.width),
                    room.Rect.y + room.Rect.height - 1
                );
            case 1: // Bottom side
                return new Vector2Int(
                    Random.Range(room.Rect.x, room.Rect.x + room.Rect.width),
                    room.Rect.y
                );
            case 2: // Left side
                return new Vector2Int(
                    room.Rect.x,
                    Random.Range(room.Rect.y, room.Rect.y + room.Rect.height)
                );
            case 3: // Right side
                return new Vector2Int(
                    room.Rect.x + room.Rect.width - 1,
                    Random.Range(room.Rect.y, room.Rect.y + room.Rect.height)
                );
            default:
                return room.Center;
        }
    }
    
    private void AddHorizontalPath(int startX, int endX, int y, List<Vector2Int> path)
    {
        int minX = Mathf.Min(startX, endX);
        int maxX = Mathf.Max(startX, endX);
        
        for (int x = minX; x <= maxX; x++)
        {
            if (x >= 0 && x < _data.Width && y >= 0 && y < _data.Height)
            {
                path.Add(new Vector2Int(x, y));
            }
        }
    }
    
    private void AddVerticalPath(int startY, int endY, int x, List<Vector2Int> path)
    {
        int minY = Mathf.Min(startY, endY);
        int maxY = Mathf.Max(startY, endY);
        
        for (int y = minY; y <= maxY; y++)
        {
            if (x >= 0 && x < _data.Width && y >= 0 && y < _data.Height)
            {
                path.Add(new Vector2Int(x, y));
            }
        }
    }
    
    private bool PathRespectsSpacing(List<Vector2Int> path)
    {
        foreach (Vector2Int tile in path)
        {
            // Skip if this is a room tile
            if (IsPartOfRoom(tile.x, tile.y)) continue;
            
            // Check for parallel corridors that are too close
            if (IsAdjacentToParallelCorridor(tile, path))
            {
                return false;
            }
        }
        return true;
    }
    
    private bool IsAdjacentToParallelCorridor(Vector2Int tile, List<Vector2Int> currentPath)
    {
        // Check all existing corridor tiles
        foreach (Vector2Int existingTile in _data.CorridorTiles)
        {
            // Skip if the existing tile is in our current path (we haven't placed it yet)
            if (currentPath.Contains(existingTile)) continue;
            
            // Check if tiles are adjacent
            int dx = Mathf.Abs(tile.x - existingTile.x);
            int dy = Mathf.Abs(tile.y - existingTile.y);
            
            // If they're adjacent (Manhattan distance of 1)
            if (dx + dy == 1)
            {
                // Check if they're part of parallel corridors
                bool currentIsHorizontal = IsHorizontalInPath(tile, currentPath);
                bool existingIsHorizontal = IsHorizontalInGrid(existingTile);
                
                // If both are horizontal and on the same y-level, or both vertical and on the same x-level
                if (currentIsHorizontal && existingIsHorizontal && tile.y == existingTile.y)
                {
                    return true; // Parallel horizontal corridors adjacent
                }
                if (!currentIsHorizontal && !existingIsHorizontal && tile.x == existingTile.x)
                {
                    return true; // Parallel vertical corridors adjacent
                }
            }
        }
        return false;
    }
    
    private bool IsHorizontalInPath(Vector2Int tile, List<Vector2Int> path)
    {
        // Check if this tile is part of a horizontal segment in the path
        bool hasLeft = path.Contains(new Vector2Int(tile.x - 1, tile.y));
        bool hasRight = path.Contains(new Vector2Int(tile.x + 1, tile.y));
        return hasLeft || hasRight;
    }

    private bool IsHorizontalInGrid(Vector2Int tile)
    {
        // Check if this tile is part of a horizontal corridor in the grid
        if (!_data.Tiles[tile.x, tile.y].IsWalkable) return false;
        
        bool hasLeft = tile.x > 0 && _data.Tiles[tile.x - 1, tile.y].IsWalkable && !IsPartOfRoom(tile.x - 1, tile.y);
        bool hasRight = tile.x < _data.Width - 1 && _data.Tiles[tile.x + 1, tile.y].IsWalkable && !IsPartOfRoom(tile.x + 1, tile.y);
        
        return hasLeft || hasRight;
    }
    
    private bool IsPartOfRoom(int x, int y)
    {
        foreach (var room in _data.Rooms)
        {
            if (room.Contains(new Vector2Int(x, y)))
            {
                return true;
            }
        }
        return false;
    }
    
    private bool RectIntersects(RectInt rectA, RectInt rectB, int padding)
    {
        RectInt paddedA = new RectInt(
            rectA.x - padding, rectA.y - padding,
            rectA.width + padding * 2, rectA.height + padding * 2
        );
        return paddedA.Overlaps(rectB);
    }

    private List<Vector2Int> GetFloorTilesInRoom(Room room)
    {
        List<Vector2Int> floorTiles = new();
        for (int x = room.Rect.x; x < room.Rect.x + room.Rect.width; x++)
        {
            for (int y = room.Rect.y; y < room.Rect.y + room.Rect.height; y++)
            {
                if (_data.Tiles[x, y].Type == TileType.Floor)
                    floorTiles.Add(new Vector2Int(x, y));
            }
        }
        return floorTiles;
    }

    private void RegisterConnection(int roomAIndex, int roomBIndex)
    {
        if (!_roomConnections.ContainsKey(roomAIndex))
            _roomConnections[roomAIndex] = new List<int>();
        if (!_roomConnections.ContainsKey(roomBIndex))
            _roomConnections[roomBIndex] = new List<int>();
            
        _roomConnections[roomAIndex].Add(roomBIndex);
        _roomConnections[roomBIndex].Add(roomAIndex);
    }
    
    private int GetTotalConnections()
    {
        int total = 0;
        foreach (var connections in _roomConnections.Values)
        {
            total += connections.Count;
        }
        return total / 2; // Each connection counted twice
    }
}

[System.Serializable]
public class GenerationSettings
{
    public int NumberOfRooms = 5;
    public int MinRoomSize = 3;
    public int MaxRoomSize = 8;
    public int RoomPadding = 2;
    [Range(0, 1)] public float ExtraConnectionChance = 0.6f;
    public int MaxConnectionsPerRoom = 4;
}

public class DisjointSet
{
    private int[] _parent;
    private int[] _rank;
    
    public DisjointSet(int size)
    {
        _parent = new int[size];
        _rank = new int[size];
        for (int i = 0; i < size; i++)
        {
            _parent[i] = i;
            _rank[i] = 0;
        }
    }
    
    public int Find(int x)
    {
        if (_parent[x] != x)
        {
            _parent[x] = Find(_parent[x]);
        }
        return _parent[x];
    }
    
    public void Union(int x, int y)
    {
        int rootX = Find(x);
        int rootY = Find(y);
        
        if (rootX != rootY)
        {
            if (_rank[rootX] < _rank[rootY])
            {
                _parent[rootX] = rootY;
            }
            else if (_rank[rootX] > _rank[rootY])
            {
                _parent[rootY] = rootX;
            }
            else
            {
                _parent[rootY] = rootX;
                _rank[rootX]++;
            }
        }
    }
}