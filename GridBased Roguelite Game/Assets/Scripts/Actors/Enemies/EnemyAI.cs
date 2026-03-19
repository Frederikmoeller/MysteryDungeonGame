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
    [SerializeField] private float pathUpdateInterval = 0.5f;
    [SerializeField] private float roamPauseTime = 1f;

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
    private bool _isWaiting;
    private Coroutine _aiCoroutine;

    private Item _carriedItem;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        if (_unit == null) _unit = GetComponent<Unit>();
        if (_movement == null) _movement = GetComponent<MovementController>();
        if (_combat == null) _combat = GetComponent<CombatController>();
        
        // Find player (Assumes player has tag "Player")
        GameObject playerGO = GameObject.FindWithTag("Player");
        if (playerGO != null)
        {
            _playerTransform = playerGO.transform;
            _playerUnit = playerGO.GetComponent<Unit>();
        }
    }

    private void OnEnable()
    {
        // Subscribe to movement completion event
        if (_movement != null)
            _movement.OnGridMoveCompleted += OnMoveCompleted;
        // Subscribe to death event
        if (_unit != null && _unit.Health != null)
            _unit.Health.OnDeath += OnDeath;
    }

    public void Initialize(GridManager grid)
    {
        _currentGrid = grid;
        Debug.LogWarning(_currentGrid);
        // Start AI Coroutine
        if (_aiCoroutine != null) StopCoroutine(_aiCoroutine);
        _aiCoroutine = StartCoroutine(AIUpdate());
    }

    private IEnumerator AIUpdate()
    {
        print($"Grid: {_currentGrid}. Player: {_playerTransform}");
        while (true)
        {
            yield return new WaitForSeconds(pathUpdateInterval);
            if (_currentGrid == null || _playerTransform == null)
            {
                Debug.LogWarning("Player transform or Grid is missing!");
                continue;
            }
            if (_unit == null || !_unit.IsAlive())
            {
                Debug.LogWarning("_unit is null or dead.");
                continue;
            }
            
            // Check if player is in range
            float distToPlayer = Vector2Int.Distance(_movement.CurrentGridPosition, GetPlayerGridPosition());

            bool playerInRange = distToPlayer <= detectionRange;

            if (playerInRange)
            {
                if (distToPlayer <= attackRange && _combat.CanAttack())
                {
                    SetState(EnemyState.Attacking);
                }
                else
                {
                    SetState(EnemyState.Chasing);
                }
            }
            else
            {
                SetState(EnemyState.Roaming);
            }

            switch (_currentState)
            {
                case EnemyState.Roaming:
                    HandleRoaming();
                    break;
                case EnemyState.Chasing:
                    HandleChasing();
                    break;
                case EnemyState.Attacking:
                    HandleAttacking();
                    break;
            }
        }
    }

    private void SetState(EnemyState newState)
    {
        if (_currentState == newState) return;
        _currentState = newState;

        if (newState == EnemyState.Roaming)
        {
            _roamingTarget = null;
        }
    }

    private void HandleRoaming()
    {
        // If we are waiting (pause after reaching target), do nothing
        if (_isWaiting) return;
        
        // If we have no path or reached end, pick a new roam target
        if (_currentPath == null || _currentPathIndex >= _currentPath.Count)
        {
            if (!_roamingTarget.HasValue || _movement.CurrentGridPosition == _roamingTarget.Value)
            {
                StartCoroutine(WaitAndPickNewRoamTarget());
                return;
            }
        }
    }
    
    private IEnumerator WaitAndPickNewRoamTarget()
    {
        _isWaiting = true;
        yield return new WaitForSeconds(roamPauseTime);
        _isWaiting = false;
        _roamingTarget = GetRandomRoamPosition();
        if (_roamingTarget.HasValue)
        {
            _currentPath = GridPathFinder.FindPath(_currentGrid, _movement.CurrentGridPosition, _roamingTarget.Value);
            _currentPathIndex = 0;
        }
    }
    
    private void HandleChasing()
    {
        Vector2Int playerPos = GetPlayerGridPosition();
        // Recompute path to player
        _currentPath = GridPathFinder.FindPath(_currentGrid, _movement.CurrentGridPosition, playerPos);
        _currentPathIndex = 0;
    }
    
    private void HandleAttacking()
    {
        // Attack if possible
        if (_combat.CanAttack() && _playerUnit != null && _playerUnit.IsAlive())
        {
            _combat.Attack(_playerUnit.gameObject);
        }
        // If player moved out of range, state will change in next AI update
    }
    
    private void OnMoveCompleted(Vector2Int newPosition)
    {
        // Check for item pickup
        TryPickupItem(newPosition);

        // Continue following path if we have one
        if (_currentPath != null && _currentPathIndex < _currentPath.Count)
        {
            Vector2Int nextStep = _currentPath[_currentPathIndex];
            // If next step is not walkable anymore (e.g., another enemy moved in), recalc path
            if (!_currentGrid.IsWalkable(nextStep.x, nextStep.y))
            {
                // Path blocked, we'll recalc in next AI update
                _currentPath = null;
                return;
            }

            // Move towards next step
            Vector2Int current = _movement.CurrentGridPosition;
            Vector2Int direction = nextStep - current;
            _movement.Move(direction); // This will trigger another move
            _currentPathIndex++;
        }
        else
        {
            // No more path – if chasing, we might be adjacent to player
            if (_currentState == EnemyState.Chasing)
            {
                Vector2Int playerPos = GetPlayerGridPosition();
                if (Vector2Int.Distance(newPosition, playerPos) <= attackRange)
                {
                    // We are adjacent, next AI update will switch to attacking
                }
            }
        }
    }
    
    private void TryPickupItem(Vector2Int position)
    {
        if (_currentGrid == null) return;
        Tile tile = _currentGrid.GetTile(position.x, position.y);
        if (tile != null && tile.HasItem)
        {
            // Can only carry one item
            if (_carriedItem == null)
            {
                _carriedItem = tile.Item;
                tile.Item = null; // remove from tile
                _carriedItem.transform.SetParent(transform); // optional: attach to enemy
                _carriedItem.gameObject.SetActive(false); // hide
                Debug.Log($"{_unit.UnitName} picked up {_carriedItem.name}");
            }
        }
    }
    
    private void DropItem()
    {
        if (_carriedItem != null && _currentGrid != null)
        {
            // Place item on current tile
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
        // Drop carried item
        DropItem();
        // Stop AI coroutine
        if (_aiCoroutine != null) StopCoroutine(_aiCoroutine);
        // Optionally, disable this component
        enabled = false;
    }
    
    private Vector2Int GetPlayerGridPosition()
    {
        // Player's grid position from MovementController if in grid mode
        if (_playerUnit != null && _playerUnit.Movement != null)
        {
            var pos = _playerUnit.Movement.CurrentGridPosition;
            return pos;
        }
        // Fallback: round world position
        return new Vector2Int(Mathf.RoundToInt(_playerTransform.position.x), Mathf.RoundToInt(_playerTransform.position.y));
    }
    
    private Vector2Int? GetRandomRoamPosition()
    {
        if (_currentGrid == null || _currentGrid.Rooms == null || _currentGrid.Rooms.Count == 0)
            return null;

        // Pick a random room, then a random floor tile in that room
        Room randomRoom = _currentGrid.Rooms[Random.Range(0, _currentGrid.Rooms.Count)];
        if (randomRoom.FloorTiles.Count == 0) return null;

        Vector2Int candidate;
        int attempts = 0;
        do
        {
            candidate = randomRoom.FloorTiles[Random.Range(0, randomRoom.FloorTiles.Count)];
            attempts++;
        } while (!_currentGrid.IsWalkable(candidate.x, candidate.y) && attempts < 10);

        return _currentGrid.IsWalkable(candidate.x, candidate.y) ? candidate : null;
    }
    
    // Public method to set carried item (e.g., when spawning with an item)
    public void SetCarriedItem(Item item)
    {
        _carriedItem = item;
        if (item != null)
        {
            item.transform.SetParent(transform);
            item.gameObject.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
