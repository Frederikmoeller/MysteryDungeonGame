using UnityEngine;
using System.Collections.Generic;

public class GridData
{
    public Tile[,] Tiles { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }
    public List<Room> Rooms { get; set; } = new();
    public List<Vector2Int> CorridorTiles { get; set; } = new();
    public Vector2Int? StairsPosition { get; set; }

    public GridData(int width, int height)
    {
        Width = width;
        Height = height;
        Tiles = new Tile[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Tiles[x, y] = new Tile()
                {
                    Position = new Vector2Int(x, y),
                    Type = TileType.Wall
                };
            }
        }
    }

    public bool IsInBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;
}
