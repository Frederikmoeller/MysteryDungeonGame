using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "DungeonLootTable", menuName = "Dungeon/Loot Table")]
public class DungeonLootTable : ScriptableObject
{
    [System.Serializable]
    public class RarityWeight
    {
        public Rarity Rarity;
        [Range(0, 100)]
        public float SpawnWeight = 20f;
    }
    
    [Header("Dungeon Settings")]
    public string DungeonName;
    public int DungeonTier = 1;
    
    [Header("Rarity Weights")]
    public List<RarityWeight> RarityWeights = new List<RarityWeight>();
    
    [Header("Available Items")]
    [Tooltip("Drag all items that can appear in this dungeon")]
    public List<ItemData> PossibleItems = new List<ItemData>();
    
    [Header("Spawn Settings")]
    public int MinItemsPerRoom = 1;
    public int MaxItemsPerRoom = 3;

    // Cache for faster lookups
    private Dictionary<Rarity, float> _weightCache;
    private Dictionary<Rarity, List<ItemData>> _itemsByRarity;
    private float _totalWeight;
    
    void OnEnable()
    {
        BuildCache();
    }
    
    private void BuildCache()
    {
        // Build weight cache
        _weightCache = new Dictionary<Rarity, float>();
        _totalWeight = 0f;
        
        foreach (var weight in RarityWeights)
        {
            _weightCache[weight.Rarity] = weight.SpawnWeight;
            _totalWeight += weight.SpawnWeight;
        }
        
        // Group items by their inherent rarity
        _itemsByRarity = new Dictionary<Rarity, List<ItemData>>();
        
        foreach (var item in PossibleItems)
        {
            if (item == null) continue;
            
            if (!_itemsByRarity.ContainsKey(item.ItemRarity))
                _itemsByRarity[item.ItemRarity] = new List<ItemData>();
                
            _itemsByRarity[item.ItemRarity].Add(item);
        }
    }
    
    public ItemData PickRandomItem()
    {
        if (PossibleItems.Count == 0 || _totalWeight <= 0)
        {
            Debug.LogWarning($"Loot table {name} has no items!");
            return null;
        }
        
        // Step 1: Roll for rarity based on weights
        float roll = Random.Range(0f, _totalWeight);
        float cumulative = 0f;
        Rarity? selectedRarity = null;
        
        foreach (var weight in RarityWeights)
        {
            cumulative += weight.SpawnWeight;
            if (roll <= cumulative)
            {
                selectedRarity = weight.Rarity;
                break;
            }
        }
        
        if (!selectedRarity.HasValue) return null;
        
        // Step 2: Get all items with that rarity
        if (!_itemsByRarity.ContainsKey(selectedRarity.Value) || 
            _itemsByRarity[selectedRarity.Value].Count == 0)
        {
            Debug.LogWarning($"No items of rarity {selectedRarity.Value} in {name}");
            return null;
        }
        
        // Step 3: Pick random item from that rarity group
        var itemsOfRarity = _itemsByRarity[selectedRarity.Value];
        int randomIndex = Random.Range(0, itemsOfRarity.Count);
        
        return itemsOfRarity[randomIndex];
    }
    
    // Generate multiple items for a room
    public List<ItemData> GenerateRoomItems()
    {
        List<ItemData> items = new List<ItemData>();
        
        int itemCount = Random.Range(MinItemsPerRoom, MaxItemsPerRoom + 1);
        
        for (int i = 0; i < itemCount; i++)
        {
            var itemData = PickRandomItem();
            if (itemData != null)
            {
                items.Add(itemData);
            }
        }
        
        return items;
    }
    
    // Optional: Get all items of a specific rarity
    public List<ItemData> GetItemsOfRarity(Rarity rarity)
    {
        return _itemsByRarity.ContainsKey(rarity) ? 
               _itemsByRarity[rarity] : 
               new List<ItemData>();
    }
    
    // Optional: Validate the loot table
    public bool IsValid()
    {
        if (PossibleItems.Count == 0)
        {
            Debug.LogError($"Loot table {name} has no items!");
            return false;
        }
        
        if (RarityWeights.Count == 0)
        {
            Debug.LogError($"Loot table {name} has no rarity weights!");
            return false;
        }
        
        // Check if we have items for each weighted rarity
        foreach (var weight in RarityWeights)
        {
            if (!_itemsByRarity.ContainsKey(weight.Rarity) || 
                _itemsByRarity[weight.Rarity].Count == 0)
            {
                Debug.LogWarning($"Loot table {name} has weight for {weight.Rarity} but no items of that rarity!");
            }
        }
        
        return true;
    }
}