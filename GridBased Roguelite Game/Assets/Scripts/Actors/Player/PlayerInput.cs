// PlayerInput.cs - Basic movement only
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Unit))]
public class PlayerInput : MonoBehaviour
{
    [SerializeField] private Unit _unit;
    private InputAction _moveAction;
    private bool _hasMovedThisTurn;

    void Awake()
    {
        _unit = GetComponent<Unit>();
        _moveAction = InputSystem.actions.FindAction("Move");
        
        TurnManager.Instance.OnTurnChanged += OnTurnChanged;
    }
    
    void OnDestroy()
    {
        if (TurnManager.Instance != null)
            TurnManager.Instance.OnTurnChanged -= OnTurnChanged;
    }
    
    private void OnTurnChanged(TurnType newTurn)
    {
        if (newTurn == TurnType.Player)
        {
            _hasMovedThisTurn = false;
            Debug.Log("Player's turn - ready to move");
        }
    }

    void Update()
    {
        // Only process movement on player's turn and if hasn't moved yet
        if (TurnManager.Instance.CurrentTurn != TurnType.Player) return;
        if (_hasMovedThisTurn) return;
        
        Vector2 moveValue = _moveAction.ReadValue<Vector2>();
        if (moveValue != Vector2.zero)
        {
            TryMove(moveValue);
        }
    }
    
    private void TryMove(Vector2 direction)
    {
        Vector2Int currentPos = _unit.Movement.CurrentGridPosition;
        Vector2Int gridDirection = GetGridDirection(direction);
        Vector2Int targetPos = currentPos + gridDirection;
        
        if (_unit.Movement.CurrentGrid.IsWalkable(targetPos.x, targetPos.y))
        {
            _hasMovedThisTurn = true;
            _unit.Move(direction);
            
            // After moving, end player turn
            TurnManager.Instance.EndPlayerTurn();
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
}