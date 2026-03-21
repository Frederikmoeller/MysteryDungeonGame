using System.Collections;
using UnityEngine;

public enum MovementMode
{
    Free,
    Grid
}
public class MovementController : MonoBehaviour
{
    public event System.Action<Vector2Int> OnGridMoveCompleted;
    
    [SerializeField] private UnitStats _baseStats;
    [SerializeField] private MovementMode _currentMode = MovementMode.Grid;

    [Header("Free Movement Settings")] 
    [SerializeField] private float _freeWalkSpeed = 5f;
    [SerializeField] private float _freeSprintSpeed = 10;
    private float _currentFreeSpeed;
    private bool _isSprinting;

    [Header("Grid Movement Settings")]
    [SerializeField] private float _timePerGridMove = 0.2f;

    private bool _isMovingOnGrid;
    private GridManager _currentGrid;

    public GridManager CurrentGrid => _currentGrid;
    public MovementMode CurrentMode => _currentMode;
    public bool IsMovingOnGrid => _isMovingOnGrid;
    public Vector2Int CurrentGridPosition => new Vector2Int(
        Mathf.RoundToInt(transform.position.x),
        Mathf.RoundToInt(transform.position.y)
    );

    void Awake()
    {
        Initialize();
    }

    public void Initialize(UnitStats stats = null)
    {
        if (stats != null)
        {
            _baseStats = stats;
        }

        if (_baseStats != null)
        {
            _freeWalkSpeed = _baseStats.WalkSpeed;
            _freeSprintSpeed = _baseStats.SprintSpeed;
        }

        _currentFreeSpeed = _freeWalkSpeed;
    }

    public void EnterDungeon(GridManager gridManager)
    {
        _currentGrid = gridManager;
        SetMovementMode(MovementMode.Grid);
        SnapToGrid();
    }

    public void ExitDungeon()
    {
        _currentGrid = null;
        SetMovementMode(MovementMode.Free);
    }

    public void SetMovementMode(MovementMode newMode)
    {
        _currentMode = newMode;
    }

    public void Move(Vector2 direction)
    {
        switch (_currentMode)
        {
            case MovementMode.Free:
                HandleFreeMovement(direction);
                break;
            case MovementMode.Grid:
                HandleGridMovement(direction);
                break;
        }
    }

    public void SetSprinting(bool sprinting)
    {
        _isSprinting = sprinting;
        if (_currentMode == MovementMode.Free)
        {
            _currentFreeSpeed = sprinting ? _freeSprintSpeed : _freeWalkSpeed;
        }
        else
        {
            Time.timeScale = sprinting ? 10f : 1f;
        }
    }

    private void HandleFreeMovement(Vector2 direction)
    {
        if (direction == Vector2.zero) return;

        Vector3 movement = direction.normalized * (_currentFreeSpeed * Time.deltaTime);
        transform.position += movement;
    }

    private void HandleGridMovement(Vector2 direction)
    {
        if (_isMovingOnGrid) return;

        Vector2Int gridDirection = GetGridDirection(direction);
        if (gridDirection == Vector2Int.zero) return;

        Vector2Int currentPosition = CurrentGridPosition;
        Vector2Int targetPosition = currentPosition + gridDirection;

        if (_currentGrid.IsWalkable(targetPosition.x, targetPosition.y))
        {
            StartCoroutine(MoveToGridCell(targetPosition));
        }
    }

    private Vector2Int GetGridDirection(Vector2 input)
    {
        // Allow diagonal movement
        int x = 0;
        int y = 0;
    
        if (Mathf.Abs(input.x) > 0.1f)
            x = (int)Mathf.Sign(input.x);
        if (Mathf.Abs(input.y) > 0.1f)
            y = (int)Mathf.Sign(input.y);
        
        return new Vector2Int(x, y);
    }

    private IEnumerator MoveToGridCell(Vector2Int targetGridPosition)
    {
        _isMovingOnGrid = true;
        UpdateGridOccupancy(CurrentGridPosition, targetGridPosition);

        Vector3 targetWorldPos = new Vector3(targetGridPosition.x, targetGridPosition.y, transform.position.z);

        Vector3 startWorldPos = transform.position;

        float elapsedTime = 0;

        while (elapsedTime < _timePerGridMove)
        {
            float t = elapsedTime / _timePerGridMove;

            transform.position = Vector3.Lerp(startWorldPos, targetWorldPos, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = targetWorldPos;

        _isMovingOnGrid = false;
        
        OnGridMoveCompleted?.Invoke(targetGridPosition);
    }

    private void UpdateGridOccupancy(Vector2Int oldPos, Vector2Int newPos)
    {
        if (_currentGrid == null) return;

        _currentGrid.SetOccupant(oldPos.x, oldPos.y, null);
        _currentGrid.SetOccupant(newPos.x, newPos.y, gameObject);
    }

    public void SnapToGrid()
    {
        if (_currentGrid == null) return;

        Vector2Int gridPos = CurrentGridPosition;
        transform.position = new Vector3(gridPos.x, gridPos.y, transform.position.z);

        _currentGrid.SetOccupant(gridPos.x, gridPos.y, gameObject);

        /*Vector3 gridPosition = new Vector3(
            Mathf.Round(transform.position.x / _gridSize) * _gridSize,
            Mathf.Round(transform.position.y / _gridSize) * _gridSize,
            transform.position.z
            );
        transform.position = gridPosition;*/
    }
}
