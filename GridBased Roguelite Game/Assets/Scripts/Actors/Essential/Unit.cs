using System;
using JetBrains.Annotations;
using UnityEngine;

public enum UnitType
{
    Monster,
    Player,
    NPC
}

[RequireComponent(typeof(HealthSystem))]
[RequireComponent(typeof(ManaSystem))]
[RequireComponent(typeof(ExperienceSystem))]
[RequireComponent(typeof(MovementController))]
[RequireComponent(typeof(CombatController))]
public class Unit : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private UnitStats _baseStats;
    
    [Header("Component References")]
    [SerializeField] private HealthSystem _healthSystem;
    [SerializeField] private ManaSystem _manaSystem;
    [SerializeField] private ExperienceSystem _experienceSystem;
    [SerializeField] private MovementController _movementController;
    [SerializeField] private CombatController _combatController;

    [Header("Unit Info")]
    [Saveable("unit_name")] [SerializeField] private string _unitName;
    [Saveable("unit_type")][SerializeField] private UnitType _unitType;

    public UnitStats BaseStats => _baseStats;
    public string UnitName => _unitName;
    public UnitType Type => _unitType;

    public HealthSystem Health => _healthSystem;
    public ManaSystem Mana => _manaSystem;
    public ExperienceSystem Experience => _experienceSystem;
    public MovementController Movement => _movementController;
    public CombatController Combat => _combatController;

    public event Action<Unit> OnUnitSpawned;
    public event Action<Unit> OnUnitDestroyed;

    private void Awake()
    {
        FindAndAssignComponents();
        InitializeSystems();
    }
    
    void Start()
    {
        OnUnitSpawned?.Invoke(this);
    }

    private void FindAndAssignComponents()
    {
        if (_healthSystem == null)
            _healthSystem = GetComponent<HealthSystem>();
        if (_manaSystem == null)
            _manaSystem = GetComponent<ManaSystem>();
        if (_experienceSystem == null)
            _experienceSystem = GetComponent<ExperienceSystem>();
        if (_movementController == null)
            _movementController = GetComponent<MovementController>();
        if (_combatController == null)
            _combatController = GetComponent<CombatController>();
    }

    private void InitializeSystems()
    {
        _healthSystem?.Initialize(_baseStats);
        _manaSystem?.Initialize(_baseStats);
        _experienceSystem?.Initialize(_baseStats);
        _movementController?.Initialize(_baseStats);
        _combatController?.Initialize(_baseStats);
        
        _experienceSystem?.ApplyStatChanges();

        if (_healthSystem != null && _experienceSystem != null)
        {
            _healthSystem.OnDeath += (killer) =>
            {
                if (killer == null) return;
                var killerUnit = killer.GetComponent<Unit>();
                killerUnit?.Experience?.GainExp(_baseStats?.BaseExpReward ?? 0);
            };
        }
    }

    #region Public Methods - Common Actions

    /// <summary>
    /// Move the unit in a direction. Movement mode (free/grid) is handled internally.
    /// </summary>
    public void Move(Vector2 direction)
    {
        _movementController?.Move(direction);
    }

    /// <summary>
    /// Attack a target game object
    /// </summary>
    public void Attack(GameObject target = null, Action OnComplete = null)
    {
        _combatController?.Attack(target, OnComplete);
    }

    /// <summary>
    /// Take damage from another unit
    /// </summary>
    public void TakeDamage(int damage, Unit attacker)
    {
        _healthSystem?.TakeDamage(damage, attacker?.gameObject);
    }

    /// <summary>
    /// Heal the unit
    /// </summary>
    public void Heal(int amount)
    {
        _healthSystem?.Heal(amount);
    }
    
    /// <summary>
    /// Use mana for abilities
    /// </summary>
    public bool UseMana(int amount)
    {
        return _manaSystem != null && _manaSystem.UseMana(amount);
    }
    
    /// <summary>
    /// Gain experience points
    /// </summary>
    public void GainExp(int amount)
    {
        _experienceSystem?.GainExp(amount);
    }

    #endregion

    #region Public Methods - State Management

    /// <summary>
    /// Check if the unit is alive
    /// </summary>
    public bool IsAlive()
    {
        return _healthSystem != null && !_healthSystem.IsDead;
    }
    
    /// <summary>
    /// Get the current health percentage (0-1)
    /// </summary>
    public float GetHealthPercentage()
    {
        return _healthSystem?.HealthPercentage ?? 0;
    }
    
    /// <summary>
    /// Get the current mana percentage (0-1)
    /// </summary>
    public float GetManaPercentage()
    {
        return _manaSystem?.ManaPercentage ?? 0;
    }
    
    /// <summary>
    /// Get the current experience progress (0-1)
    /// </summary>
    public float GetExpProgress()
    {
        return _experienceSystem?.ExpProgress ?? 0;
    }

    #endregion

    #region Public Methods - Dungeon/Grid Integration

    /// <summary>
    /// Call this when entering a dungeon to switch to grid movement
    /// </summary>
    public void EnterDungeon(GridManager gridManager)
    {
        _movementController?.EnterDungeon(gridManager);
    }
    
    /// <summary>
    /// Call this when exiting a dungeon to switch to free movement
    /// </summary>
    public void ExitDungeon()
    {
        _movementController?.ExitDungeon();
    }
    
    /// <summary>
    /// Get the unit's current grid position (if in grid mode)
    /// </summary>
    public Vector2Int? GetGridPosition()
    {
        if (_movementController != null && _movementController.CurrentMode == MovementMode.Grid)
        {
            return _movementController.CurrentGridPosition;
        }
        return null;
    }

    #endregion
    
    #region Public Methods - Configuration
    
    /// <summary>
    /// Change the unit's base stats at runtime
    /// </summary>
    public void SetBaseStats(UnitStats newStats)
    {
        _baseStats = newStats;
        InitializeSystems();
    }
    
    /// <summary>
    /// Set the unit's name
    /// </summary>
    public void SetUnitName(string newName)
    {
        _unitName = newName;
    }
    
    /// <summary>
    /// Set the unit's type
    /// </summary>
    public void SetUnitType(UnitType newType)
    {
        _unitType = newType;
    }
    
    #endregion
    
    private void OnDestroy()
    {
        OnUnitDestroyed?.Invoke(this);
    }
}
