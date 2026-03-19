using System;
using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    public event Action<int, GameObject> OnDamageTaken;
    public event Action<int> OnHealed;
    public event Action<int, int> OnMaxHealthChanged;
    public event Action<GameObject> OnDeath;
    
    private ExperienceSystem _expSystem;

    [SerializeField] private UnitStats _baseStats;
    [Saveable("Health_Current")] [SerializeField] private int _currentHealth;
    [Saveable("Health_Max")] [SerializeField] private int _maxHealth;

    public int CurrentHealth => _currentHealth;
    public int MaxHealth => _maxHealth;
    public float HealthPercentage => (float)_currentHealth / _maxHealth;
    public bool IsDead => _currentHealth <= 0;

    void Awake()
    {
        _expSystem = GetComponent<ExperienceSystem>();
        Initialize();
    }

    void Start()
    {
        if (_expSystem != null)
        {
            _expSystem.OnStatsChanged += UpdateMaxHealth;
        }
    }

    public void Initialize(UnitStats stats = null)
    {
        
        if (stats != null)
        {
            _baseStats = stats;
        }

        if (_baseStats != null)
        {
            _maxHealth = _baseStats.BaseMaxHealth;
        }

        _currentHealth = _maxHealth;
    }
    
    private void UpdateMaxHealth(UnitStats stats)
    {
        if (_expSystem != null)
        {
            int newMax = _expSystem.MaxHealth;
            float healthPercent = HealthPercentage;
            _maxHealth = newMax;
            _currentHealth = Mathf.RoundToInt(_maxHealth * healthPercent);
            _currentHealth = Mathf.Max(1, _currentHealth);
            OnMaxHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }
    }

    public void TakeDamage(int damage, GameObject attacker)
    {
        if (IsDead) return;

        // Optional: Apply defense reduction
        if (_expSystem != null)
        {
            float defenseReduction = 1 - (_expSystem.Defense / 100f);
            damage = Mathf.RoundToInt(damage * defenseReduction);
        }

        _currentHealth = Mathf.Max(0, _currentHealth - damage);
        OnDamageTaken?.Invoke(damage, attacker);

        if (IsDead)
        {
            OnDeath?.Invoke(attacker);
        }
    }

    public void Heal(int amount)
    {
        if (IsDead) return;

        _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
        OnHealed?.Invoke(amount);
    }

    public void SetMaxHealth(int newMax, bool fullHeal = false)
    {
        _maxHealth = newMax;
        _currentHealth = fullHeal ? _maxHealth : Mathf.Min(_currentHealth, _maxHealth);
    }
    
    private void OnDestroy()
    {
        if (_expSystem != null)
        {
            _expSystem.OnStatsChanged -= UpdateMaxHealth;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
