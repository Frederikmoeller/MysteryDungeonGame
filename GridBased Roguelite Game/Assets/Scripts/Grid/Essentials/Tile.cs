using UnityEngine;

public enum TileType
{
    Wall,
    Floor,
    Effect,
    Stairs
}
public class Tile
{
    public Vector2Int Position;
    public Item Item;
    public TileType Type;
    public GameObject Occupant;

    public EffectData GroundEffect;
    
    public bool IsWalkable => Type != TileType.Wall;
    public bool HasItem => Item != null;
}
