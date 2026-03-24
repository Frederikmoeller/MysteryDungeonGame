// CombatController.cs
using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class CombatController : MonoBehaviour
{
    [SerializeField] private UnitStats _baseStats;
    [SerializeField] private int _attackDamage;
    [SerializeField] private float _attackCooldown = 1f;
    
    [Header("Damage Settings")]
    [SerializeField] private float _minDamageMultiplier = 0.9f;
    [SerializeField] private float _maxDamageMultiplier = 1.1f;
    
    [Header("Animation")]
    [SerializeField] private float _attackAnimationDelay = 0.2f; // small visual delay
    
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
        if (_healthSystem != null)
            _healthSystem.OnDeath += HandleDeath;
    }
    
    public void Initialize(UnitStats stats = null)
    {
        if (stats != null)
            _baseStats = stats;
    }

    public void Attack(GameObject target, Action OnComplete)
    {
        // Start the attack coroutine
        StartCoroutine(ExecuteAttack(target, OnComplete));
    }
    
    private IEnumerator ExecuteAttack(GameObject target, Action onComplete)
    {
        // Wait for the visual delay (attack wind‑up)
        yield return new WaitForSeconds(_attackAnimationDelay);

        if (target != null)
        {
            // Apply damage
            var targetHealth = target.GetComponent<HealthSystem>();
            if (targetHealth != null)
            {
                int damage = CalculateBaseDamage();
                damage = ApplyDamageVariance(damage);
                targetHealth.TakeDamage(damage, gameObject);
                print($"{target} took {damage}");
            }
        }
    
        yield return new WaitForSeconds(0.5f);
    
        onComplete?.Invoke();
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

        GetComponent<Unit>().Movement.CurrentGrid.SetOccupant(GetComponent<Unit>().Movement.CurrentGridPosition.x, GetComponent<Unit>().Movement.CurrentGridPosition.y, null);
        if (GetComponent<EnemyAI>() != null)
        {
            Destroy(gameObject, 0.5f);
        }
        else
        {
            GetComponent<SpriteRenderer>().enabled = false;
            //TODO: End the dungeon with defeat screen and send player back to town
        }
    }
    
    private void OnDestroy()
    {
        if (_healthSystem != null)
            _healthSystem.OnDeath -= HandleDeath;
    }
}