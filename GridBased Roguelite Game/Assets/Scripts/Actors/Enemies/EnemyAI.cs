// EnemyAI.cs (modified)

using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Random = UnityEngine.Random;

public enum EnemyState
{
    Roaming,
    Chasing,
    Attacking
}

public class EnemyAI : MonoBehaviour, IEnemy
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
    private int _pathfindingRetryCount;

    public bool IsMoving => _isMoving;
    public Unit Unit => _unit;

    // IEnemy implementation
    public int Priority
    {
        get
        {
            // Higher priority for enemies closer to player
            if (_playerUnit == null) return 0;
            float dist = Vector2Int.Distance(_unit.Movement.CurrentGridPosition, _playerUnit.Movement.CurrentGridPosition);
            return Mathf.RoundToInt((detectionRange - dist) * 100);
        }
    }

    public bool IsAlive() => _unit.IsAlive();

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

    void OnEnable()
    {
        TurnManager.Instance?.RegisterEnemy(this);
        if (_unit?.Health != null)
            _unit.Health.OnDeath += OnDeath;
    }

    void OnDisable()
    {
        TurnManager.Instance?.UnregisterEnemy(this);
        if (_unit?.Health != null)
            _unit.Health.OnDeath -= OnDeath;
    }

    void OnDestroy()
    {
        if (_unit?.Movement != null)
            _unit.Movement.OnGridMoveCompleted -= OnMoveCompleted;
    }

    public void Initialize(GridManager grid)
    {
        _currentGrid = grid;
        Debug.Log($"{name} initialized with grid");
    }

    // IEnemy methods
    public bool WantsToAttack()
    {
        if (!_unit.IsAlive()) return false;
        float distToPlayer = Vector2Int.Distance(_unit.Movement.CurrentGridPosition, GetPlayerGridPosition());
        return distToPlayer <= attackRange;
    }

    public void Attack(Action onComplete)
    {
        _currentState = EnemyState.Attacking;
        if (_playerUnit == null)
        {
            onComplete?.Invoke();
            return;
        }
        _unit.Attack(_playerUnit.gameObject, onComplete);
    }

    public void Move(Action onComplete)
    {
        _onMoveComplete = onComplete;
        _pathfindingRetryCount = 0;

        if (!_unit.IsAlive())
        {
            _onMoveComplete?.Invoke();
            return;
        }

        float distToPlayer = Vector2Int.Distance(_unit.Movement.CurrentGridPosition, GetPlayerGridPosition());

        // Decide to chase or roam
        if (distToPlayer <= detectionRange)
        {
            ChasePlayer();
        }
        else
        {
            Roam();
        }
    }

    // (Keep existing chase, roam, pathfinding logic, but adapt to use callback)
    private Action _onMoveComplete;

    private void ChasePlayer()
    {
        _currentState = EnemyState.Chasing;

        Vector2Int playerPos = GetPlayerGridPosition();
        Vector2Int currentPos = _unit.Movement.CurrentGridPosition;

        _currentPath = GridPathFinder.FindPath(_currentGrid, currentPos, playerPos);
        _currentPathIndex = 0;

        if (_currentPath == null || _currentPath.Count == 0)
        {
            Debug.Log($"{name} no path to player");
            _onMoveComplete?.Invoke();
            return;
        }

        TryMoveAlongPath();
    }

    private void Roam()
    {
        _currentState = EnemyState.Roaming;

        if (_roamPauseTurnsRemaining > 0)
        {
            _roamPauseTurnsRemaining--;
            _onMoveComplete?.Invoke();
            return;
        }

        if (_currentPath == null || _currentPathIndex >= _currentPath.Count)
        {
            PickNewRoamTarget();
            if (_currentPath == null || _currentPath.Count == 0)
            {
                _onMoveComplete?.Invoke();
                return;
            }
        }

        TryMoveAlongPath();
    }

    private void TryMoveAlongPath()
    {
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

        if (GridPathFinder.IsValidDestination(_currentGrid, nextStep.x, nextStep.y, gameObject))
        {
            _isMoving = true;
            _unit.Move(new Vector2(direction.x, direction.y));
            // OnMoveCompleted will call _onMoveComplete
        }
        else
        {
            // Destination blocked, try alternate path
            if (_currentState == EnemyState.Chasing)
            {
                Vector2Int playerPos = GetPlayerGridPosition();
                Vector2Int currentPosition = _unit.Movement.CurrentGridPosition;
                if (Vector2Int.Distance(currentPosition, playerPos) <= 1.1f)
                {
                    Debug.Log($"{name} adjacent to player but tile occupied");
                    _onMoveComplete?.Invoke();
                    return;
                }

                var alternatePath = GridPathFinder.FindPath(_currentGrid, currentPosition, playerPos);
                if (alternatePath != null && alternatePath.Count > 0)
                {
                    _currentPath = alternatePath;
                    _currentPathIndex = 0;
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
                    _currentPath = null;
                    _roamPauseTurnsRemaining = Random.Range(1, 3);
                    _onMoveComplete?.Invoke();
                }
            }
            else
            {
                _currentPath = null;
                _onMoveComplete?.Invoke();
            }
        }
    }

    private void OnMoveCompleted(Vector2Int newPosition)
    {
        _isMoving = false;
        if (_currentPath != null && _currentPathIndex < _currentPath.Count)
            _currentPathIndex++;
        _onMoveComplete?.Invoke();
    }

    // ... Keep existing helper methods: GetPlayerGridPosition, PickNewRoamTarget, GetRandomRoamPosition, OnDeath
    private Vector2Int GetPlayerGridPosition()
    {
        if (_playerUnit != null && _playerUnit.Movement != null)
            return _playerUnit.Movement.TargetGridPosition;
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
                Tile tileData = _currentGrid.GetTile(tile.x, tile.y);
                if (tileData != null && (tileData.Type == TileType.Floor || tileData.Type == TileType.Effect))
                {
                    allWalkableTiles.Add(tile);
                }
            }
        }

        if (allWalkableTiles.Count == 0) return null;

        Vector2Int currentPos = _unit.Movement.CurrentGridPosition;
        List<Vector2Int> validTiles = allWalkableTiles.Where(t => t != currentPos).ToList();
        if (validTiles.Count == 0) return null;

        return validTiles[Random.Range(0, validTiles.Count)];
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

    private void OnDeath(GameObject killer)
    {
        enabled = false;
        if (_isMoving && _onMoveComplete != null)
            _onMoveComplete?.Invoke();
    }
}