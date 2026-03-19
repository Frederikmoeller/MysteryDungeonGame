using System;
using System.Collections.Generic;
using UnityEngine;

public class PartyInventory : MonoBehaviour
{
    public static PartyInventory Instance { get; private set; }
    
    public event Action OnInventoryChanged;
    public event Action<ItemData, int> OnItemAdded;
    public event Action<ItemData, int> OnItemRemoved;
    public event Action<ItemData> OnItemUsed;
    
    [SerializeField] private Inventory _inventory;
    [SerializeField] private List<Unit> _partyMembers = new List<Unit>();
    [SerializeField] private bool _persistAcrossScenes = true;
    
    // Quick access to different item categories
    public IReadOnlyList<InventorySlot> AllItems => _inventory.Slots;
    public int FreeSlots => _inventory.FreeSlots;
    public bool IsFull => _inventory.IsFull;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        
        if (_persistAcrossScenes)
        {
            DontDestroyOnLoad(gameObject);
        }
        
        _inventory = new Inventory(20);
        _inventory.OnInventoryChanged += () => OnInventoryChanged?.Invoke();
    }
    
    public bool AddItem(ItemData itemData, int quantity = 1)
    {
        bool result = _inventory.AddItem(itemData, quantity);
        
        if (result)
        {
            OnItemAdded?.Invoke(itemData, quantity);
        }
        
        return result;
    }
    
    public bool RemoveItem(ItemData itemData, int quantity = 1)
    {
        bool result = _inventory.RemoveItem(itemData, quantity);
        
        if (result)
        {
            OnItemRemoved?.Invoke(itemData, quantity);
        }
        
        return result;
    }
    
    public bool UseItem(ItemData itemData, Unit user, Unit target = null)
    {
        if (!_inventory.HasItem(itemData, 1))
        {
            Debug.LogWarning($"Cannot use {itemData.ItemName}: not in inventory");
            return false;
        }
        
        // Check if we're in dungeon for usable items
        if (GameManager.Instance != null && !GameManager.Instance.InDungeon)
        {
            Debug.LogWarning("Can only use items in dungeon");
            return false;
        }
        
        // Create temporary item instance to use its effects
        var tempItem = new GameObject("TempItem").AddComponent<Item>();
        tempItem.Initialize(itemData, 1);
        
        // Use the item
        tempItem.Use(user.gameObject, target?.gameObject);
        
        // Remove from inventory
        RemoveItem(itemData, 1);
        
        OnItemUsed?.Invoke(itemData);
        
        Destroy(tempItem.gameObject);
        return true;
    }
    
    public bool UseItemAtSlot(int slotIndex, Unit user, Unit target = null)
    {
        var slots = _inventory.Slots;
        
        if (slotIndex < 0 || slotIndex >= slots.Count)
            return false;
            
        var slot = slots[slotIndex];
        if (slot.IsEmpty)
            return false;
            
        return UseItem(slot.ItemData, user, target);
    }
    
    public void AddPartyMember(Unit member)
    {
        if (!_partyMembers.Contains(member))
        {
            _partyMembers.Add(member);
        }
    }
    
    public void RemovePartyMember(Unit member)
    {
        _partyMembers.Remove(member);
    }
    
    public bool HasItem(ItemData itemData, int quantity = 1)
    {
        return _inventory.HasItem(itemData, quantity);
    }
    
    public int GetItemQuantity(ItemData itemData)
    {
        return _inventory.GetItemQuantity(itemData);
    }
    
    public List<InventorySlot> GetItemsByType(EffectType effectType)
    {
        List<InventorySlot> result = new List<InventorySlot>();
        
        foreach (var slot in _inventory.Slots)
        {
            foreach (var effect in slot.ItemData.Effects)
            {
                if (effect.Type == effectType)
                {
                    result.Add(slot);
                    break;
                }
            }
        }
        
        return result;
    }
    
    public void TransferToParty(Inventory sourceInventory, ItemData itemData, int quantity = 1)
    {
        if (sourceInventory.HasItem(itemData, quantity))
        {
            sourceInventory.RemoveItem(itemData, quantity);
            AddItem(itemData, quantity);
        }
    }
    
    public void TransferFromParty(Inventory targetInventory, ItemData itemData, int quantity = 1)
    {
        if (HasItem(itemData, quantity))
        {
            RemoveItem(itemData, quantity);
            targetInventory.AddItem(itemData, quantity);
        }
    }
    
    public void SortItems()
    {
        // Simple sort by item name
        var slots = new List<InventorySlot>(_inventory.Slots);
        slots.Sort((a, b) => string.Compare(a.ItemData.ItemName, b.ItemData.ItemName));
        
        _inventory.Clear();
        
        foreach (var slot in slots)
        {
            _inventory.AddItem(slot.ItemData, slot.Quantity);
        }
    }
}