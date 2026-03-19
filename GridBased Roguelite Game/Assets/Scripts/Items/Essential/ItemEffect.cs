using System.Collections.Generic;
using UnityEngine;

public enum EffectType
{
    Heal,
    ManaRestore,
    Damage,
    buff,
    Debuff,
    StatusRemoval,
    Teleport,
    SpawnEntity
}

public class ItemEffect
{
    public EffectType Type;

    public int Value;
    public bool IsPercentage;
    public float Duration;
    
    // For status removal
    public List<string> StatusEffectsToRemove;
    
    // For spawning
    public GameObject EntityToSpawn;
    public Vector3 SpawnOffset;
    
    // For buffs/Debuffs
    public string BuffType;
    public float BuffMultiplier;
}
