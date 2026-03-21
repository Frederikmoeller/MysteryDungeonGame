using UnityEngine;

public class CombatController : MonoBehaviour
{
    [SerializeField] private UnitStats _baseStats;
    [SerializeField] private int _attackDamage;
    [SerializeField] private float _attackCooldown = 1f;
    
    [Header("Damage Settings")]
    [SerializeField] private float _minDamageMultiplier = 0.9f;
    [SerializeField] private float _maxDamageMultiplier = 1.1f;
    
    private float _lastAttackTime;
    private HealthSystem _healthSystem;
    private ExperienceSystem _expSystem;
    
    private void Awake()
    {
        _healthSystem = GetComponent<HealthSystem>();
        _expSystem = GetComponent<ExperienceSystem>();
    }
    
    private void Start()
    {
        Initialize();
        
        // Subscribe to death event
        if (_healthSystem != null)
        {
            _healthSystem.OnDeath += HandleDeath;
        }
    }
    
    public void Initialize(UnitStats stats = null)
    {
        if (stats != null)
            _baseStats = stats;
    }
    
    public bool CanAttack()
    {
        return Time.time >= _lastAttackTime + _attackCooldown;
    }
    
    public void Attack(GameObject target)
    {
        if (!CanAttack()) return;
    
        var targetHealth = target.GetComponent<HealthSystem>();
    
        if (targetHealth != null)
        {
            int damage = CalculateBaseDamage();
            damage = ApplyDamageVariance(damage);
            targetHealth.TakeDamage(damage, gameObject);
            _lastAttackTime = Time.time;
        }
        TurnManager.Instance.EndTurn();
    }
    
    private int CalculateBaseDamage()
    {
        if (_expSystem == null)
            return _baseStats?.BaseAttack ?? 10;
    
        return _expSystem.Attack;
    }
    
    private int ApplyDamageVariance(int baseDamage)
    {
        float variance = Random.Range(_minDamageMultiplier, _maxDamageMultiplier);
        return Mathf.Max(1, Mathf.RoundToInt(baseDamage * variance));
    }
    
    private void HandleDeath(GameObject killer)
    {
        if (killer != null && _baseStats != null)
        {
            var killerExp = killer.GetComponent<ExperienceSystem>();
            if (killerExp != null && _expSystem != null)
            {
                int expReward = _baseStats.GetExpRewardForLevel(_expSystem.CurrentLevel);
                killerExp.GainExp(expReward);
            }
        }
    
        Destroy(gameObject, 0.5f);
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (_healthSystem != null)
        {
            _healthSystem.OnDeath -= HandleDeath;
        }
    }
}
