using System;
using UnityEngine;

public class ExperienceSystem : MonoBehaviour
{
    public event Action<int> OnLevelUp;
    public event Action<int, int> OnExpGained;
    public event Action<UnitStats> OnStatsChanged;

    [SerializeField] private UnitStats _baseStats;
    [Saveable("exp_level")] [SerializeField] private int _currentLevel = 1;
    [Saveable("exp_current")] [SerializeField] private int _currentExp;
    [Saveable("exp_required")] [SerializeField] private int _requiredExp;

    // Runtime calculated stats
    private int _calculatedMaxHealth;
    private int _calculatedMaxMana;
    private int _calculatedAttack;
    private int _calculatedDefense;

    public int CurrentLevel => _currentLevel;
    public int CurrentExp => _currentExp;
    public int RequiredExp => _requiredExp;
    public float ExpProgress => (float)_currentExp / _requiredExp;
    
    // Expose calculated stats
    public int MaxHealth => _calculatedMaxHealth;
    public int MaxMana => _calculatedMaxMana;
    public int Attack => _calculatedAttack;
    public int Defense => _calculatedDefense;
    
    void Awake()
    {
        Initialize();
    }

    public void Initialize(UnitStats stats = null)
    {
        if (stats != null)
            _baseStats = stats;

        if (_baseStats != null)
        {
            CalculateRequiredExp();
            RecalculateStats();
        }
    }

    /// <summary>
    /// Set the unit to a specific level (useful for zone-based enemy spawning)
    /// </summary>
    public void SetLevel(int level)
    {
        if (_baseStats == null) return;
        
        _currentLevel = Mathf.Clamp(level, 1, 99);
        _currentExp = 0;
        CalculateRequiredExp();
        RecalculateStats();
        OnStatsChanged?.Invoke(_baseStats);
    }

    public void GainExp(int amount)
    {
        if (_baseStats == null) return;
        
        _currentExp += amount;
        OnExpGained?.Invoke(_currentExp, _requiredExp);
        CheckForLevelUps();
    }

    private void CheckForLevelUps()
    {
        bool leveledUp = false;
        
        while (_currentExp >= _requiredExp)
        {
            LevelUp();
            leveledUp = true;
        }
        
        if (leveledUp)
        {
            RecalculateStats();
            OnStatsChanged?.Invoke(_baseStats);
        }
    }

    private void LevelUp()
    {
        _currentLevel++;
        _currentExp -= _requiredExp;
        CalculateRequiredExp();
        OnLevelUp?.Invoke(_currentLevel);
    }

    private void CalculateRequiredExp()
    {
        if (_baseStats == null) return;
        
        // Formula: 100 * level^2 (your existing formula - works great!)
        _requiredExp = 100 * _currentLevel * _currentLevel;
    }

    private void RecalculateStats()
    {
        if (_baseStats == null) return;
        
        // Predetermined stat growth based on base stats + per-level scaling
        _calculatedMaxHealth = Mathf.RoundToInt(_baseStats.BaseMaxHealth + _baseStats.HealthScaling * (_currentLevel - 1));
        _calculatedMaxMana = Mathf.RoundToInt(_baseStats.BaseMaxMana + _baseStats.ManaScaling * (_currentLevel - 1));
        _calculatedAttack = Mathf.RoundToInt(_baseStats.BaseAttack + _baseStats.AttackScaling * (_currentLevel - 1));
        _calculatedDefense = Mathf.RoundToInt(_baseStats.BaseDefense + _baseStats.DefenseScaling * (_currentLevel - 1));
    }

    // Call this when you need to sync stats with other systems
    public void ApplyStatChanges()
    {
        RecalculateStats();
        OnStatsChanged?.Invoke(_baseStats);
    }
}