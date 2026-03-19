using System.Collections.Generic;
using UnityEngine;

public enum Rarity
{
     Common,
     Uncommon,
     Rare,
     Legendary,
}

[CreateAssetMenu(fileName = "ItemData", menuName = "Item/Item")]
public class ItemData : ScriptableObject
{
     public string ItemName;
     public bool Stackable;
     public bool IsCurrency;
     [TextArea] public string Description;
     public Rarity ItemRarity;
     public Sprite Icon;
     
     // Single effect or Multiple
     public List<ItemEffect> Effects;
     
     // Use conditions;
     public bool RequiresTarget;
     public float UseRange = 2f;

     [Header("Stack Settings")]
     public int MinStack = 1;  // Minimum stack size
     public int MaxStack = 1;  // Maximum stack size

     [Header("Spawn Settings")] 
     public int MinSpawnStack = 1;
     public int MaxSpawnStack = 1;

}
