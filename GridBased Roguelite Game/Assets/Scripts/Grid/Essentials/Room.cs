using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Room
{
    private static int _nextId = 0;

    public int Id { get; private set; }
    public RectInt Rect { get; private set; }
    public List<Vector2Int> FloorTiles;
    public Vector2Int Center => new Vector2Int(Rect.x + Rect.width / 2, Rect.y + Rect.height / 2);

    public Room(RectInt rect)
    {
        Id = _nextId++;
        Rect = rect;
    }

    public bool Contains(Vector2Int point)
    {
        return Rect.Contains(point);
    }
}

// Helper classes for minimum spanning tree
public class RoomConnection
{
    public int RoomA;
    public int RoomB;
    public float Distance;
    
    public RoomConnection(int a, int b, float dist)
    {
        RoomA = a;
        RoomB = b;
        Distance = dist;
    }
}
