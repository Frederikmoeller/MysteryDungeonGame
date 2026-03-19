using UnityEngine;

public class PlayerPickup : MonoBehaviour
{
    [SerializeField] private float _pickupRange = 0.1f;
    [SerializeField] private Unit _unit;
    [SerializeField] private GridManager _currentGrid;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        _unit = GetComponent<Unit>();
    }

    private void OnEnable()
    {
        print(_unit.Movement);
        _unit.Movement.OnGridMoveCompleted += HandleTileInteraction;
    }

    private void OnDisable()
    {
        if (_unit.Movement != null)
            _unit.Movement.OnGridMoveCompleted -= HandleTileInteraction;
    }

    private void HandleTileInteraction(Vector2Int gridPosition)
    {
        if (_unit.Movement.CurrentMode != MovementMode.Grid) return;

        _currentGrid = _unit.Movement.CurrentGrid;
        if (_currentGrid == null) return;

        Tile tile = _currentGrid.GetTile(gridPosition.x, gridPosition.y);
        if (tile == null) return;

        // Check tile type and handle accordingly
        switch (tile.Type)
        {
            case TileType.Floor:
                if (tile.HasItem)
                    TryPickUpItem(tile.Item);
                break;
                
            case TileType.Effect:
                TryTriggerEffect(tile);
                break;
                
            case TileType.Stairs:
                TryUseStairs(tile);
                break;
        }
    }

    private void TryPickUpItem(Item worldItem)
    {
        if (worldItem == null) return;
        
        int stackSize = worldItem.CurrentStackSize;
        
        //Try to add to party inventory
        if (PartyInventory.Instance != null && PartyInventory.Instance.AddItem(worldItem.Data, stackSize))
        {
            Debug.Log($"Picked up {worldItem.Data.ItemName} x{stackSize}");
            
            // Clear the item from the tile before destroying
            Vector2Int gridPos = new Vector2Int(
                Mathf.RoundToInt(worldItem.transform.position.x),
                Mathf.RoundToInt(worldItem.transform.position.y)
            );
            
            Tile tile = _currentGrid.GetTile(gridPos.x, gridPos.y);
            
            if (tile != null)
            {
                tile.Item = null;
            }
            
            Destroy(worldItem.gameObject);
        }
        else
        {
            Debug.Log("Cannot pick up item - inventory full or weight limit reached");
            //TODO: Make this into UI
        }
    }

    private void TryTriggerEffect(Tile tile)
    {
        if (tile.GroundEffect == null) return;

        EffectData effect = tile.GroundEffect;
        Debug.Log($"Triggered effect: {effect.EffectName}");
        
        effect.Apply(_unit);
        _currentGrid.OnEffectTriggered?.Invoke(effect);

        if (effect.OneTimeUse)
        {
            tile.GroundEffect = null;
        }
    }

    private void TryUseStairs(Tile tile)
    {
        if (tile.Type != TileType.Stairs) return;
        
        Debug.Log("Used stairs to proceed to next floor");

        _currentGrid.OnStairsUsed?.Invoke();
        
        // Tell the grid manager to go to next floor
        if (_currentGrid != null)
        {
            _currentGrid.GoToNextFloor();
        }
    }
}
