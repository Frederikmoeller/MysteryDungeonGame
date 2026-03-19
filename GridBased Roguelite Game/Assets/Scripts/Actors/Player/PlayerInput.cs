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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
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
    }

    // Update is called once per frame
    void Update()
    {
        OnMove();
        OnSprint(_sprintAction.IsPressed());
        if (_attackAction.IsPressed())
        {
            // _baseUnit.Attack();
        }

    }

    private void OnMove()
    {
        Vector2 moveValue = _moveAction.ReadValue<Vector2>();
        print(moveValue);
        _baseUnit.Move(moveValue);
    }

    private void OnSprint(bool active)
    {
        _baseUnit.Movement.SetSprinting(active);
    }
}
