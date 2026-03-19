using UnityEngine;

[CreateAssetMenu(fileName = "UnitStats", menuName = "Unit/UnitStats")]
public class UnitStats : ScriptableObject
{
    [Header("Base Stats")]
    public int BaseMaxHealth;
    public int BaseMaxMana;
    public int BaseAttack;
    public int BaseDefense;
    public int BaseVitality;
    public int BaseIntelligence;
    public int BaseLuck;
    
    [Header("Stat Scaling (per level)")]
    public float HealthScaling = 15f;
    public float ManaScaling = 8f;
    public float AttackScaling = 2f;
    public float DefenseScaling = 1f;

    [Header("Movement")]
    public float WalkSpeed = 5f;
    public float SprintSpeed = 10;

    [Header("Unit Info")] 
    public string UnitName;
    public UnitType Type;

    [Header("Experience")]
    public int BaseExpReward = 50;
    public float ExpRewardScaling = 0.1f;
    
    // Add these helper methods at the bottom of the class
    public int GetMaxHealthForLevel(int level) => 
        Mathf.RoundToInt(BaseMaxHealth + (HealthScaling * (level - 1)));

    public int GetMaxManaForLevel(int level) => 
        Mathf.RoundToInt(BaseMaxMana + (ManaScaling * (level - 1)));

    public int GetAttackForLevel(int level) => 
        Mathf.RoundToInt(BaseAttack + (AttackScaling * (level - 1)));

    public int GetDefenseForLevel(int level) => 
        Mathf.RoundToInt(BaseDefense + (DefenseScaling * (level - 1)));

    public int GetExpRewardForLevel(int level) => 
        Mathf.RoundToInt(BaseExpReward * (1 + (ExpRewardScaling * (level - 1))));

}
