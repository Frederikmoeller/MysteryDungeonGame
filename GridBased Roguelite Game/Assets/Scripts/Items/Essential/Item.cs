using UnityEngine;

public class Item : MonoBehaviour
{
    [SerializeField] private ItemData _data;
    private int _currentStackSize = 1;
    
    public ItemData Data => _data;
    public int CurrentStackSize => _currentStackSize;

    public void Use(GameObject user, GameObject target = null)
    {
        if (!CanUse()) return;

        foreach (var effect in _data.Effects)
        {
            ApplyEffect(effect, user, target);
        }

        if (_currentStackSize > 0)
        {
            _currentStackSize--;
        }
    }
    
    // Initialize method called when spawning the item
    public void Initialize(ItemData data, int stackSize = 1)
    {
        _data = data;
        _currentStackSize = stackSize;
        
        // Optional: Update visual to show stack size (like "Health Potion x3")
        UpdateVisuals();
    }
    
    private void UpdateVisuals()
    {
        // Optional: Update sprite or add text for stack size
        if (_data != null && _data.Stackable && _currentStackSize > 1)
        {
            // You could add a TextMesh or update a UI element here
            // For example: GetComponentInChildren<TextMesh>().text = $"x{_currentStackSize}";
        }
    }

    private void ApplyEffect(ItemEffect effect, GameObject user, GameObject target)
    {
        GameObject effectTarget = target ?? user;

        switch (effect.Type)
        {
            case EffectType.Heal:
                var health = effectTarget.GetComponent<HealthSystem>();
                if (health != null)
                {
                    int healAmount = effect.IsPercentage ? Mathf.RoundToInt(health.MaxHealth * (effect.Value / 100)) : effect.Value;
                    health.Heal(healAmount);
                }

                ;
                break;
            
            case EffectType.ManaRestore:
                var mana = effectTarget.GetComponent<ManaSystem>();
                if (mana != null)
                {
                    int manaAmount = effect.IsPercentage ? Mathf.RoundToInt(mana.MaxMana * (effect.Value / 100)) : effect.Value;
                    mana.RestoreMana(manaAmount);
                }
                break;
            
            case EffectType.Damage:
                var damageable = effectTarget.GetComponent<HealthSystem>();
                damageable?.TakeDamage(effect.Value, user);
                break;
            
            case EffectType.StatusRemoval:
                // TODO: Implement status effects
                break;
        }
    }

    private bool CanUse()
    {
        return GameManager.Instance.InDungeon;
    }
}
