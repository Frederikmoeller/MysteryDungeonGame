using System;
using UnityEngine;

[Serializable]
public class EffectData
{
    public enum EffectCategory
    {
        Health,
        Gold,
        Trap,
        Buff,
        Cleanse,
        Other
    }

    public EffectCategory Category;
    public string EffectName;
    public int Value;
    public Sprite Icon;
    public bool OneTimeUse = true;

    public void Apply(Unit unit)
    {
        switch (Category)
        {
            case EffectCategory.Health:
                Debug.Log($"{unit.name} Stepped on health effect tile");
                break;
            case EffectCategory.Gold:
                Debug.Log($"{unit.name} Stepped on gold effect tile");
                break;
            case EffectCategory.Trap:
                Debug.Log($"{unit.name} Stepped on trap effect tile");
                break;
            case EffectCategory.Buff:
                Debug.Log($"{unit.name} Stepped on buff effect tile");
                break;
            case EffectCategory.Cleanse:
                Debug.Log($"{unit.name} Stepped on cleanse effect tile");
                break;
            case EffectCategory.Other:
                Debug.Log($"{unit.name} Stepped on other effect tile");
                break;
        }
    }
}
