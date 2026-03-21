using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Unit))]
public class PlayerInput : MonoBehaviour
{
    [SerializeField] private Unit _baseUnit;
    
    private InputAction _moveAction;
    private InputAction _attackAction;
    private InputAction _interactAction;
    private InputAction _sprintAction;
    private InputAction _lockAction;
    private InputAction _targetAction;
    private InputAction _pauseAction;
    private bool _hasMovedThisTurn;
    private bool _isWaitingForMoveComplete;

    void Start()
    {
        _baseUnit = GetComponent<Unit>();
        _moveAction = InputSystem.actions.FindAction("Move");
        _attackAction = InputSystem.actions.FindAction("Attack");
        _interactAction = InputSystem.actions.FindAction("Interact");
        _sprintAction = InputSystem.actions.FindAction("Sprint");
        _lockAction = InputSystem.actions.FindAction("Lock");
        _targetAction = InputSystem.actions.FindAction("Target");
        _pauseAction = InputSystem.actions.FindAction("Pause");
        
        _baseUnit.Movement.OnGridMoveCompleted += OnMoveComplete;
        TurnManager.Instance.OnTurnChanged += OnTurnChanged;
    }
    
    void OnDestroy()
    {
        if (_baseUnit != null && _baseUnit.Movement != null)
            _baseUnit.Movement.OnGridMoveCompleted -= OnMoveComplete;
            
        if (TurnManager.Instance != null)
            TurnManager.Instance.OnTurnChanged -= OnTurnChanged;
    }
    
    private void OnTurnChanged()
    {
        if (TurnManager.Instance.CurrentTurn == TurnType.Player)
        {
            _hasMovedThisTurn = false;
            _isWaitingForMoveComplete = false;
        }
    }

    void Update()
    {
        if (GameManager.Instance.InDungeon)
        {
            if (TurnManager.Instance.CurrentTurn != TurnType.Player) return;
            if (_isWaitingForMoveComplete) return;
        }
        
        OnMove();
        OnSprint(_sprintAction.IsPressed());
        
        if (_attackAction.WasPressedThisFrame())
        {
            // Attack logic
            TurnManager.Instance.EndTurn();
        }
    }

    private void OnMove()
    {
        if (_hasMovedThisTurn) return;
        
        Vector2 moveValue = _moveAction.ReadValue<Vector2>();
        
        if (moveValue != Vector2.zero)
        {
            Vector2Int currentPos = _baseUnit.Movement.CurrentGridPosition;
            Vector2Int gridDirection = GetGridDirection(moveValue);
            Vector2Int targetPos = currentPos + gridDirection;
            
            if (_baseUnit.Movement.CurrentGrid.IsWalkable(targetPos.x, targetPos.y))
            {
                _hasMovedThisTurn = true;
                _isWaitingForMoveComplete = true;
                _baseUnit.Move(moveValue);
            }
        }
    }
    
    private Vector2Int GetGridDirection(Vector2 input)
    {
        int x = 0;
        int y = 0;
    
        if (Mathf.Abs(input.x) > 0.1f)
            x = (int)Mathf.Sign(input.x);
        if (Mathf.Abs(input.y) > 0.1f)
            y = (int)Mathf.Sign(input.y);
        
        return new Vector2Int(x, y);
    }

    private void OnSprint(bool active)
    {
        _baseUnit.Movement.SetSprinting(active);
    }

    private void OnMoveComplete(Vector2Int newPosition)
    {
        _isWaitingForMoveComplete = false;
        TurnManager.Instance.EndTurn();
    }
}