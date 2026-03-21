using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyState
{
    Sleep,
    Roaming,
    Chasing,
    Attacking
}

public class EnemyAI : MonoBehaviour
{
    [Header("AI Settings")] 
    [SerializeField] private float detectionRange = 8f;
    [SerializeField] private float attackRange = 1.5f;

    [Header("References")] 
    [SerializeField] private Unit _unit;
    [SerializeField] private MovementController _movement;
    [SerializeField] private CombatController _combat;
    
    [Header("State")]
    [SerializeField] private EnemyState _currentState = EnemyState.Roaming;
    private Transform _playerTransform;
    private Unit _playerUnit;
    private GridManager _currentGrid;
    private List<Vector2Int> _currentPath;
    private int _currentPathIndex;
    private Vector2Int? _roamingTarget;
    private int _roamPauseTurnsRemaining;
    private Item _carriedItem;
    private bool _isTakingTurn;
    
    void Awake()
    {
        if (_unit == null) _unit = GetComponent<Unit>();
        if (_movement == null) _movement = GetComponent<MovementController>();
        if (_combat == null) _combat = GetComponent<CombatController>();
        
        GameObject playerGO = GameObject.FindWithTag("Player");
        if (playerGO != null)
        {
            _playerTransform = playerGO.transform;
            _playerUnit = playerGO.GetComponent<Unit>();
        }
    }

    private void OnEnable()
    {
        _movement.OnGridMoveCompleted += OnMoveCompleted;
        
        if (_unit != null && _unit.Health != null)
            _unit.Health.OnDeath += OnDeath;
            
        TurnManager.Instance.RegisterEnemy(this);
    }

    private void OnDisable()
    {
        _movement.OnGridMoveCompleted -= OnMoveCompleted;
        
        if (_unit != null && _unit.Health != null)
            _unit.Health.OnDeath -= OnDeath;
            
        TurnManager.Instance.UnregisterEnemy(this);
    }

    public void Initialize(GridManager grid)
    {
        _currentGrid = grid;
    }

    public void TakeTurn()
    {
        if (_isTakingTurn) return;
        if (_unit == null || !_unit.IsAlive()) 
        {
            EndTurn();
            return;
        }
        
        _isTakingTurn = true;
        
        float distToPlayer = Vector2Int.Distance(_movement.CurrentGridPosition, GetPlayerGridPosition());
        
        if (distToPlayer <= attackRange && _combat.CanAttack())
        {
            // Attack
            _combat.Attack(_playerUnit.gameObject);
            EndTurn();
        }
        else if (distToPlayer <= detectionRange)
        {
            HandleChasing();
        }
        else
        {
            HandleRoaming();
        }
    }
    
    private void EndTurn()
    {
        _isTakingTurn = false;
        TurnManager.Instance.EndTurn();
    }
    
    private void HandleRoaming()
    {
        // Handle pause between roams
        if (_roamPauseTurnsRemaining > 0)
        {
            _roamPauseTurnsRemaining--;
            EndTurn();
            return;
        }
        
        // Need a new path?
        if (_currentPath == null || _currentPathIndex >= _currentPath.Count)
        {
            PickNewRoamTarget();
            if (_currentPath == null || _currentPath.Count == 0)
            {
                EndTurn();
            }
            return;
        }
        
        // Try to move
        if (_currentPathIndex < _currentPath.Count)
        {
            Vector2Int nextStep = _currentPath[_currentPathIndex];
            Vector2Int direction = nextStep - _movement.CurrentGridPosition;
            
            if (_currentGrid.IsWalkable(nextStep.x, nextStep.y))
            {
                _unit.Move(direction);
                // Wait for OnMoveCompleted
            }
            else
            {
                // Blocked, try again next turn
                _currentPath = null;
                EndTurn();
            }
        }
    }
    
    private void PickNewRoamTarget()
    {
        _roamingTarget = GetRandomRoamPosition();
        
        if (_roamingTarget.HasValue)
        {
            _currentPath = GridPathFinder.FindPath(_currentGrid, _movement.CurrentGridPosition, _roamingTarget.Value);
            _currentPathIndex = 0;
            
            if (_currentPath == null || _currentPath.Count == 0)
            {
                _currentPath = null;
            }
        }
    }
    
    private void HandleChasing()
    {
        Vector2Int playerPos = GetPlayerGridPosition();
        Vector2Int currentPos = _movement.CurrentGridPosition;
    
        if (Vector2Int.Distance(currentPos, playerPos) <= attackRange)
        {
            SetState(EnemyState.Attacking);
            EndTurn();
            return;
        }
    
        // Need a new path?
        if (_currentPath == null || _currentPathIndex >= _currentPath.Count)
        {
            _currentPath = GridPathFinder.FindPath(_currentGrid, currentPos, playerPos);
            _currentPathIndex = 0;
            
            if (_currentPath == null || _currentPath.Count == 0)
            {
                EndTurn();
                return;
            }
        }
    
        // Try to move
        if (_currentPathIndex < _currentPath.Count)
        {
            Vector2Int nextStep = _currentPath[_currentPathIndex];
            Vector2Int direction = nextStep - currentPos;
            
            if (_currentGrid.IsWalkable(nextStep.x, nextStep.y))
            {
                _unit.Move(direction);
                // Wait for OnMoveCompleted
            }
            else
            {
                // Blocked, try again next turn
                _currentPath = null;
                EndTurn();
            }
        }
    }
    
    private void OnMoveCompleted(Vector2Int newPosition)
    {
        // Increment path index
        if (_currentPath != null && _currentPathIndex < _currentPath.Count)
        {
            _currentPathIndex++;
        }
        
        // Check for item pickup after moving
        TryPickupItem();
        
        // End turn after moving
        EndTurn();
    }
    
    private void SetState(EnemyState newState)
    {
        if (_currentState == newState) return;
        _currentState = newState;
        
        if (newState == EnemyState.Roaming)
        {
            _roamingTarget = null;
        }
        else if (newState == EnemyState.Attacking)
        {
            _currentPath = null;
            _currentPathIndex = 0;
        }
    }
    
    private Vector2Int GetPlayerGridPosition()
    {
        if (_playerUnit != null && _playerUnit.Movement != null)
        {
            return _playerUnit.Movement.CurrentGridPosition;
        }
        return new Vector2Int(Mathf.RoundToInt(_playerTransform.position.x), Mathf.RoundToInt(_playerTransform.position.y));
    }
    
    private Vector2Int? GetRandomRoamPosition()
    {
        if (_currentGrid == null || _currentGrid.Rooms == null || _currentGrid.Rooms.Count == 0)
            return null;

        List<Vector2Int> allWalkableTiles = new List<Vector2Int>();
        foreach (Room room in _currentGrid.Rooms)
        {
            foreach (Vector2Int tile in room.FloorTiles)
            {
                if (_currentGrid.IsWalkable(tile.x, tile.y))
                {
                    allWalkableTiles.Add(tile);
                }
            }
        }
    
        if (allWalkableTiles.Count == 0)
            return null;
    
        Vector2Int currentPos = _movement.CurrentGridPosition;
        List<Vector2Int> validTiles = new List<Vector2Int>();
    
        foreach (Vector2Int tile in allWalkableTiles)
        {
            if (tile != currentPos)
            {
                validTiles.Add(tile);
            }
        }
    
        if (validTiles.Count == 0)
            return null;
    
        return validTiles[Random.Range(0, validTiles.Count)];
    }
    
    private void TryPickupItem()
    {
        if (_currentGrid == null || _carriedItem != null) return;
        
        Vector2Int pos = _movement.CurrentGridPosition;
        Tile tile = _currentGrid.GetTile(pos.x, pos.y);
        if (tile != null && tile.HasItem)
        {
            _carriedItem = tile.Item;
            tile.Item = null;
            _carriedItem.transform.SetParent(transform);
            _carriedItem.gameObject.SetActive(false);
        }
    }
    
    private void DropItem()
    {
        if (_carriedItem != null && _currentGrid != null)
        {
            Vector2Int pos = _movement.CurrentGridPosition;
            Tile tile = _currentGrid.GetTile(pos.x, pos.y);
            if (tile != null && tile.Item == null)
            {
                _carriedItem.transform.SetParent(null);
                _carriedItem.transform.position = new Vector3(pos.x, pos.y, 0);
                _carriedItem.gameObject.SetActive(true);
                tile.Item = _carriedItem;
                _carriedItem = null;
            }
        }
    }
    
    private void OnDeath(GameObject killer)
    {
        DropItem();
        enabled = false;
    }
    
    public void SetCarriedItem(Item item)
    {
        _carriedItem = item;
        if (item != null)
        {
            item.transform.SetParent(transform);
            item.gameObject.SetActive(false);
        }
    }
}