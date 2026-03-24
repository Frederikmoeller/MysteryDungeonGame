using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Unit))]
public class PlayerInput : MonoBehaviour
{
    [SerializeField] private Unit _unit;
    private InputAction _moveAction;
    private InputAction _attackAction;
    private bool _hasActedThisTurn;
    [SerializeField] private Vector2Int _lastDirection = Vector2Int.down;

    void Awake()
    {
        _unit = GetComponent<Unit>();
        _moveAction = InputSystem.actions.FindAction("Move");
        _attackAction = InputSystem.actions.FindAction("Attack");
    }

    void OnEnable()
    {
        TrySubscribeToTurnManager();
    }

    void Start()
    {
        TrySubscribeToTurnManager();
    }

    void OnDisable()
    {
        if (TurnManager.Instance != null)
            TurnManager.Instance.OnTurnChanged -= OnTurnChanged;
    }

    void TrySubscribeToTurnManager()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnChanged += OnTurnChanged;
        }
    }

    void Update()
    {
        if (TurnManager.Instance == null) return;
        if (TurnManager.Instance.CurrentTurn != TurnType.Player) return;
        if (_hasActedThisTurn) return;

        Vector2 moveValue = _moveAction.ReadValue<Vector2>();
        if (moveValue != Vector2.zero)
        {
            TryMove(moveValue);
        }

        if (_attackAction.triggered)
        {
            Vector2 attackDir = moveValue;
            if (attackDir == Vector2.zero)
            {
                attackDir = _lastDirection == Vector2Int.zero ? Vector2.down : new Vector2(_lastDirection.x, _lastDirection.y);
            }

            TryAttack(attackDir);
        }
    }

    private void TryMove(Vector2 direction)
    {
        Vector2Int gridDirection = GetGridDirection(direction);
        if (gridDirection == Vector2Int.zero) return;

        Vector2Int currentPos = _unit.Movement.CurrentGridPosition;
        Vector2Int targetPos = currentPos + gridDirection;
        _lastDirection = gridDirection;

        if (_unit.Movement.CurrentGrid.IsWalkable(targetPos.x, targetPos.y))
        {
            _unit.Movement.OnGridMoveCompleted += OnMoveCompleted;
            _unit.Move(direction);

            TurnManager.Instance.OnPlayerMovementStarted();
        }
    }

    private void TryAttack(Vector2 direction)
    {
        Vector2Int gridDirection = GetGridDirection(direction);

        Vector2Int currentPos = _unit.Movement.CurrentGridPosition;
        Vector2Int targetPos = currentPos + gridDirection;

        Tile targetTile = _unit.Movement.CurrentGrid.GetTile(targetPos.x, targetPos.y);


        Unit targetUnit = targetTile.Occupant?.GetComponent<Unit>();

        _hasActedThisTurn = true;
        _lastDirection = gridDirection;

        // Attack with callback – after attack (including delay), end player turn
        _unit.Attack(targetUnit?.gameObject, () =>
        {
            TurnManager.Instance.EndPlayerTurnAfterAction();
        });
    }

    private IEnumerator EndTurnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        TurnManager.Instance.EndPlayerTurnAfterAction();
    }

    private void OnMoveCompleted(Vector2Int newPosition)
    {
        _hasActedThisTurn = true;
        _unit.Movement.OnGridMoveCompleted -= OnMoveCompleted;
        TurnManager.Instance.OnPlayerMovementComplete();
    }

    private void OnTurnChanged(TurnType newTurn)
    {
        if (newTurn == TurnType.Player)
        {
            _hasActedThisTurn = false;
            Debug.Log("Player's turn - ready to act");
        }
    }

    private Vector2Int GetGridDirection(Vector2 input)
    {
        int x = 0, y = 0;
        if (Mathf.Abs(input.x) > 0.1f) x = (int)Mathf.Sign(input.x);
        if (Mathf.Abs(input.y) > 0.1f) y = (int)Mathf.Sign(input.y);
        return new Vector2Int(x, y);
    }
}