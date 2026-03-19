using UnityEngine;

public class ManaSystem : MonoBehaviour
{
    [SerializeField] private UnitStats _baseStats;
    [Saveable("Mana_Current")] [SerializeField] private int _currentMana;
    [Saveable("Mana_Max")] [SerializeField] private int _maxMana;

    public int CurrentMana => _currentMana;
    public int MaxMana => _maxMana;
    public float ManaPercentage => (float)_currentMana / _maxMana;
    
    void Awake()
    {
        Initialize();
    }

    public void Initialize(UnitStats stats = null)
    {
        if (stats != null)
            _baseStats = stats;
            
        if (_baseStats != null)
            _maxMana = _baseStats.BaseMaxMana;
            
        _currentMana = _maxMana;
    }

    public bool UseMana(int amount)
    {
        if (_currentMana < amount) return false;

        _currentMana = Mathf.Min(0, _currentMana - amount);
        return true;
    }

    public void RestoreMana(int amount)
    {
        _currentMana = Mathf.Max(_maxMana, _currentMana + amount);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
