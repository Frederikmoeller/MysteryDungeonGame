using UnityEngine;
using UnityEngine.Tilemaps;

public class GridVisualizer : MonoBehaviour
{
    [Header("Visualization Mode")]
    [SerializeField] private bool _useFallbackSprites = true;
    [Tooltip("When true: Uses simple sprites from this component. When false: Uses Tilemaps from DungeonVisuals")]
    
    [Header("Fallback Sprite Settings (Prototyping)")]
    [SerializeField] private GameObject _tilePrefab;
    [SerializeField] private Transform _tileContainer;
    
    // Default fallback sprites (generated at runtime)
    private Sprite _defaultFloorSprite;
    private Sprite _defaultWallSprite;
    private Sprite _defaultEffectSprite;
    private Sprite _defaultStairsSprite;
    
    // Runtime references
    private GameObject[,] _tileVisuals;
    private GridData _currentData;
    private DungeonVisuals _currentDungeonVisuals;

    private void Awake()
    {
        GenerateDefaultSprites();
    }

    private void GenerateDefaultSprites()
    {
        _defaultFloorSprite = CreateColoredSprite(Color.gray, "DefaultFloor");
        _defaultWallSprite = CreateColoredSprite(Color.red, "DefaultWall");
        _defaultEffectSprite = CreateColoredSprite(Color.yellow, "DefaultEffect");
        _defaultStairsSprite = CreateColoredSprite(Color.cyan, "DefaultStairs");
        
        Debug.Log("GridVisualizer: Default fallback sprites generated");
    }

    private Sprite CreateColoredSprite(Color color, string name)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1f);
        sprite.name = name;
        return sprite;
    }

    public void Visualize(GridData data, DungeonVisuals dungeonVisuals)
    {
        _currentData = data;
        _currentDungeonVisuals = dungeonVisuals;
        
        ClearAllVisuals();
        
        if (_useFallbackSprites)
        {
            VisualizeWithFallbackSprites(data);
            Debug.Log("GridVisualizer: Using FALLBACK SPRITE mode");
        }
        else
        {
            VisualizeWithTilemaps(data, dungeonVisuals);
            Debug.Log("GridVisualizer: Using TILEMAP mode from DungeonVisuals");
        }
    }

    private void VisualizeWithFallbackSprites(GridData data)
    {
        if (_tilePrefab == null)
        {
            Debug.LogError("GridVisualizer: Fallback sprite mode enabled but no TilePrefab assigned!");
            return;
        }

        // Create container if needed
        if (_tileContainer == null)
        {
            GameObject container = new GameObject("FallbackTiles");
            container.transform.SetParent(transform);
            _tileContainer = container.transform;
        }

        _tileVisuals = new GameObject[data.Width, data.Height];

        for (int x = 0; x < data.Width; x++)
        {
            for (int y = 0; y < data.Height; y++)
            {
                CreateFallbackTile(x, y, data.Tiles[x, y].Type);
            }
        }
        
        Debug.Log($"GridVisualizer: Created {data.Width * data.Height} fallback sprite tiles");
    }

    private void CreateFallbackTile(int x, int y, TileType type)
    {
        Sprite sprite = GetFallbackSprite(type);
        if (sprite == null) return;

        GameObject tile = Instantiate(_tilePrefab, new Vector3(x, y, 0), Quaternion.identity, _tileContainer);
        tile.name = $"FallbackTile_{x}_{y}_{type}";

        SpriteRenderer renderer = tile.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sprite = sprite;
            renderer.sortingOrder = GetSortingOrder(type);
        }

        _tileVisuals[x, y] = tile;
    }

    private Sprite GetFallbackSprite(TileType type)
    {
        return type switch
        {
            TileType.Floor => _defaultFloorSprite,
            TileType.Wall => _defaultWallSprite,
            TileType.Effect => _defaultEffectSprite,
            TileType.Stairs => _defaultStairsSprite,
            _ => _defaultFloorSprite
        };
    }

    private int GetSortingOrder(TileType type)
    {
        return type switch
        {
            TileType.Floor => 0,
            TileType.Wall => 1,
            TileType.Effect => 2,
            TileType.Stairs => 2,
            _ => 0
        };
    }

    private void VisualizeWithTilemaps(GridData data, DungeonVisuals visuals)
    {
        if (visuals == null)
        {
            Debug.LogError("GridVisualizer: Tilemap mode enabled but DungeonVisuals is null!");
            return;
        }

        // Validate required tilemaps
        if (visuals.FloorTilemap == null || visuals.WallTilemap == null || visuals.EffectTilemap == null)
        {
            Debug.LogError("GridVisualizer: Tilemap mode enabled but one or more Tilemaps are not assigned in DungeonVisuals!");
            return;
        }

        for (int x = 0; x < data.Width; x++)
        {
            for (int y = 0; y < data.Height; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                TileType type = data.Tiles[x, y].Type;

                switch (type)
                {
                    case TileType.Floor:
                        visuals.FloorTilemap.SetTile(pos, visuals.FloorTile);
                        visuals.WallTilemap.SetTile(pos, null);
                        visuals.EffectTilemap.SetTile(pos, null);
                        break;
                    case TileType.Wall:
                        visuals.WallTilemap.SetTile(pos, visuals.WallTile);
                        visuals.FloorTilemap.SetTile(pos, null);
                        visuals.EffectTilemap.SetTile(pos, null);
                        break;
                    case TileType.Effect:
                        visuals.EffectTilemap.SetTile(pos, visuals.EffectTile);
                        visuals.FloorTilemap.SetTile(pos, visuals.FloorTile);
                        visuals.WallTilemap.SetTile(pos, null);
                        break;
                    case TileType.Stairs:
                        visuals.EffectTilemap.SetTile(pos, visuals.StairsTile);
                        visuals.FloorTilemap.SetTile(pos, visuals.FloorTile);
                        visuals.WallTilemap.SetTile(pos, null);
                        break;
                }
            }
        }
        
        Debug.Log("GridVisualizer: Tilemap visualization complete");
    }

    private void ClearAllVisuals()
    {
        // Clear fallback sprites
        if (_tileVisuals != null)
        {
            foreach (GameObject tile in _tileVisuals)
            {
                if (tile != null)
                {
                    if (Application.isPlaying)
                        Destroy(tile);
                    else
                        DestroyImmediate(tile);
                }
            }
            _tileVisuals = null;
        }

        // Clear container children if it exists but we lost reference
        if (_tileContainer != null)
        {
            foreach (Transform child in _tileContainer)
            {
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }

        // Clear tilemaps (if we were in tilemap mode)
        if (_currentDungeonVisuals != null && !_useFallbackSprites)
        {
            _currentDungeonVisuals.FloorTilemap?.ClearAllTiles();
            _currentDungeonVisuals.WallTilemap?.ClearAllTiles();
            _currentDungeonVisuals.EffectTilemap?.ClearAllTiles();
        }
    }

    public void UpdateTileVisual(int x, int y, TileType type)
    {
        if (_currentData == null || !_currentData.IsInBounds(x, y)) return;

        if (_useFallbackSprites)
        {
            // Remove old fallback sprite
            if (_tileVisuals?[x, y] != null)
            {
                if (Application.isPlaying)
                    Destroy(_tileVisuals[x, y]);
                else
                    DestroyImmediate(_tileVisuals[x, y]);
            }
            
            // Create new fallback sprite
            CreateFallbackTile(x, y, type);
        }
        else
        {
            // Update tilemap
            Vector3Int pos = new Vector3Int(x, y, 0);
            
            switch (type)
            {
                case TileType.Floor:
                    _currentDungeonVisuals?.FloorTilemap?.SetTile(pos, _currentDungeonVisuals.FloorTile);
                    _currentDungeonVisuals?.WallTilemap?.SetTile(pos, null);
                    _currentDungeonVisuals?.EffectTilemap?.SetTile(pos, null);
                    break;
                case TileType.Wall:
                    _currentDungeonVisuals?.WallTilemap?.SetTile(pos, _currentDungeonVisuals.WallTile);
                    _currentDungeonVisuals?.FloorTilemap?.SetTile(pos, null);
                    _currentDungeonVisuals?.EffectTilemap?.SetTile(pos, null);
                    break;
                case TileType.Effect:
                    _currentDungeonVisuals?.EffectTilemap?.SetTile(pos, _currentDungeonVisuals.EffectTile);
                    _currentDungeonVisuals?.FloorTilemap?.SetTile(pos, _currentDungeonVisuals.FloorTile);
                    _currentDungeonVisuals?.WallTilemap?.SetTile(pos, null);
                    break;
                case TileType.Stairs:
                    _currentDungeonVisuals?.EffectTilemap?.SetTile(pos, _currentDungeonVisuals.StairsTile);
                    _currentDungeonVisuals?.FloorTilemap?.SetTile(pos, _currentDungeonVisuals.FloorTile);
                    _currentDungeonVisuals?.WallTilemap?.SetTile(pos, null);
                    break;
            }
        }
    }
}