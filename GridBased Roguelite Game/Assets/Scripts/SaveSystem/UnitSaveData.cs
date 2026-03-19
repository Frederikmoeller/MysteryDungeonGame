using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class UnitSaveData
{
    public string UnitName;
    public UnitType Type;
    
    //Health data
    public int CurrentHealth;
    public int MaxHealth;
    
    //Mana data
    public int CurrentMana;
    public int MaxMana;
    
    // Experience data
    public int CurrentLevel;
    public int CurrentExp;
    public int RequiredExp;
    
    // Stats reference
    public string BaseStatsName;
}

[Serializable]
public class GameSaveData
{
    public string saveVersion = "1.0";
    public string saveTime;
    public List<UnitSaveData> Units = new();
}
