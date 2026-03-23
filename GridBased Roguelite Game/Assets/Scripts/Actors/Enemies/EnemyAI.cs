// EnemyAI.cs - Fixed to prevent stack overflow
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum EnemyState
{
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
    
    [Header("State")]
    [SerializeField] private EnemyState _currentState = EnemyState.Roaming;
    
    private Transform _playerTransform;
    private Unit _playerUnit;
    private GridManager _currentGrid;
    private List<Vector2Int> _currentPath;
    private int _currentPathIndex;
    private Vector2Int? _roamingTarget;
    private int _roamPauseTurnsRemaining;
    private bool _isMoving;
    private System.Action _onMoveComplete;
    private int _pathfindingRetryCount; // Prevent infinite retry loops

    public bool IsMoving => _isMoving;
    public Unit Unit => _unit;

    void Awake()
    {
        if (_unit == null) _unit = GetComponent<Unit>();
        
        GameObject playerGO = GameObject.FindWithTag("Player");
        if (playerGO != null)
        {
            _playerTransform = playerGO.transform;
            _playerUnit = playerGO.GetComponent<Unit>();
        }
        
        if (_unit.Movement != null)
        {
            _unit.Movement.OnGridMoveCompleted += OnMoveCompleted;
        }
    }

    private void OnEnable()
    {
        TurnManager.Instance?.RegisterEnemy(this);
        
        if (_unit?.Health != null)
            _unit.Health.OnDeath += OnDeath;
    }

    private void OnDisable()
    {
        TurnManager.Instance?.UnregisterEnemy(this);
        
        if (_unit?.Health != null)
            _unit.Health.OnDeath -= OnDeath;
    }

    void OnDestroy()
    {
        if (_unit?.Movement != null)
        {
            _unit.Movement.OnGridMoveCompleted -= OnMoveCompleted;
        }
    }

    public void Initialize(GridManager grid)
    {
        _currentGrid = grid;
        Debug.Log($"{name} initialized with grid");
    }

    public void MoveOnTurn(System.Action onComplete)
    {
        _onMoveComplete = onComplete;
        _pathfindingRetryCount = 0; // Reset retry counter for this turn
        
        if (!_unit.IsAlive())
        {
            _onMoveComplete?.Invoke();
            return;
        }
        
        // Get distance to player
        float distToPlayer = Vector2Int.Distance(_unit.Movement.CurrentGridPosition, GetPlayerGridPosition());
        
        // Decide what to do
        if (distToPlayer <= attackRange)
        {
            // Attack instead of move
            AttackPlayer();
        }
        else if (distToPlayer <= detectionRange)
        {
            ChasePlayer();
        }
        else
        {
            Roam();
        }
    }
    
    private void AttackPlayer()
    {
        _currentState = EnemyState.Attacking;
        _unit.Attack(_playerUnit.gameObject);
        
        // Attack is instant, complete immediately
        _onMoveComplete?.Invoke();
    }
    
    private void ChasePlayer()
    {
        _currentState = EnemyState.Chasing;
        
        Vector2Int playerPos = GetPlayerGridPosition();
        Vector2Int currentPos = _unit.Movement.CurrentGridPosition;
        
        // Use GridPathFinder to get path (now paths through enemies)
        _currentPath = GridPathFinder.FindPath(_currentGrid, currentPos, playerPos);
        _currentPathIndex = 0;
        
        if (_currentPath == null || _currentPath.Count == 0)
        {
            // No path found, just end turn
            Debug.Log($"{name} no path to player");
            _onMoveComplete?.Invoke();
            return;
        }
        
        // Try to move along the path
        TryMoveAlongPath();
    }
    
    private void Roam()
    {
        _currentState = EnemyState.Roaming;
        
        // Handle pause between roams
        if (_roamPauseTurnsRemaining > 0)
        {
            _roamPauseTurnsRemaining--;
            _onMoveComplete?.Invoke();
            return;
        }
        
        // Need a new roam target?
        if (_currentPath == null || _currentPathIndex >= _currentPath.Count)
        {
            PickNewRoamTarget();
            
            if (_currentPath == null || _currentPath.Count == 0)
            {
                _onMoveComplete?.Invoke();
                return;
            }
        }
        
        // Try to move along the path
        TryMoveAlongPath();
    }
    
    private void TryMoveAlongPath()
    {
        // Prevent infinite recursion
        _pathfindingRetryCount++;
        if (_pathfindingRetryCount > 5)
        {
            Debug.LogWarning($"{name} too many pathfinding retries, giving up this turn");
            _onMoveComplete?.Invoke();
            return;
        }
        
        if (_currentPath == null || _currentPathIndex >= _currentPath.Count)
        {
            _onMoveComplete?.Invoke();
            return;
        }
        
        Vector2Int nextStep = _currentPath[_currentPathIndex];
        Vector2Int currentPos = _unit.Movement.CurrentGridPosition;
        Vector2Int direction = nextStep - currentPos;
        
        // Check if the destination tile is actually available to move into
        if (GridPathFinder.IsValidDestination(_currentGrid, nextStep.x, nextStep.y, gameObject))
        {
            _isMoving = true;
            _unit.Move(new Vector2(direction.x, direction.y));
            // Will complete in OnMoveCompleted
        }
        else
        {
            // Destination is blocked (likely by another enemy), try to find alternate path
            if (_currentState == EnemyState.Chasing)
            {
                Vector2Int playerPos = GetPlayerGridPosition();
                Vector2Int currentPosition = _unit.Movement.CurrentGridPosition;
                
                // Check if we're already adjacent to player but tile is occupied
                if (Vector2Int.Distance(currentPosition, playerPos) <= 1.1f)
                {
                    // We're adjacent but can't move onto player's tile - just end turn
                    Debug.Log($"{name} adjacent to player but tile occupied");
                    _onMoveComplete?.Invoke();
                    return;
                }
                
                // Try to find alternate path, excluding the blocked tile
                var alternatePath = GridPathFinder.FindPath(_currentGrid, currentPosition, playerPos);
                if (alternatePath != null && alternatePath.Count > 0)
                {
                    _currentPath = alternatePath;
                    _currentPathIndex = 0;
                    // Try again with new path
                    TryMoveAlongPath();
                }
                else
                {
                    Debug.Log($"{name} no alternate path found");
                    _onMoveComplete?.Invoke();
                }
            }
            else if (_currentState == EnemyState.Roaming && _roamingTarget.HasValue)
            {
                // For roaming, try to find alternate path
                Vector2Int currentPosition = _unit.Movement.CurrentGridPosition;
                var alternatePath = GridPathFinder.FindPath(_currentGrid, currentPosition, _roamingTarget.Value);
                if (alternatePath != null && alternatePath.Count > 0)
                {
                    _currentPath = alternatePath;
                    _currentPathIndex = 0;
                    TryMoveAlongPath();
                }
                else
                {
                    // No alternate path, just pause and try again next turn
                    _currentPath = null;
                    _roamPauseTurnsRemaining = Random.Range(1, 3);
                    _onMoveComplete?.Invoke();
                }
            }
            else
            {
                // For roaming, just give up this turn
                _currentPath = null;
                _onMoveComplete?.Invoke();
            }
        }
    }
    
    private void PickNewRoamTarget()
    {
        _roamingTarget = GetRandomRoamPosition();
        
        if (_roamingTarget.HasValue)
        {
            Vector2Int currentPos = _unit.Movement.CurrentGridPosition;
            _currentPath = GridPathFinder.FindPath(_currentGrid, currentPos, _roamingTarget.Value);
            _currentPathIndex = 0;
            
            if (_currentPath == null || _currentPath.Count == 0)
            {
                _currentPath = null;
                _roamPauseTurnsRemaining = Random.Range(1, 3);
            }
        }
        else
        {
            _roamPauseTurnsRemaining = Random.Range(1, 3);
        }
    }
    
    private void OnMoveCompleted(Vector2Int newPosition)
    {
        _isMoving = false;
        
        // Increment path index
        if (_currentPath != null && _currentPathIndex < _currentPath.Count)
        {
            _currentPathIndex++;
        }
        
        // Notify that movement is complete
        _onMoveComplete?.Invoke();
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
                // For roaming, we only care about tile type, not occupation
                Tile tileData = _currentGrid.GetTile(tile.x, tile.y);
                if (tileData != null && (tileData.Type == TileType.Floor || tileData.Type == TileType.Effect))
                {
                    allWalkableTiles.Add(tile);
                }
            }
        }
    
        if (allWalkableTiles.Count == 0)
            return null;
    
        Vector2Int currentPos = _unit.Movement.CurrentGridPosition;
        List<Vector2Int> validTiles = allWalkableTiles.Where(t => t != currentPos).ToList();
    
        if (validTiles.Count == 0)
            return null;
    
        return validTiles[Random.Range(0, validTiles.Count)];
    }
    
    private void OnDeath(GameObject killer)
    {
        enabled = false;
        
        // If this enemy was moving, complete the callback
        if (_isMoving && _onMoveComplete != null)
        {
            _onMoveComplete?.Invoke();
        }
    }
}