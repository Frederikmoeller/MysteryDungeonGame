using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Inventory
{
    public event Action OnInventoryChanged;
    
    [SerializeField] private List<InventorySlot> _slots = new List<InventorySlot>();
    [SerializeField] private int _maxSlots;
    [SerializeField] private int _currentCurrency;
    [SerializeField] private int _maxCurrency;

    public IReadOnlyList<InventorySlot> Slots => _slots;
    public int MaxSlots => _maxSlots;
    
    public int CurrentCurrency => _currentCurrency;
    public int MaxCurrency => _maxCurrency;
    public int UsedSlots => _slots.Count;
    public int FreeSlots => _maxSlots - _slots.Count;
    public bool IsFull => UsedSlots >= MaxSlots;
    
    public Inventory(int maxSlots = 20)
    {
        _maxSlots = maxSlots;
    }
    
    public bool AddItem(ItemData itemData, int quantity = 1)
    {
        if (itemData == null || quantity <= 0) return false;

        switch (itemData.IsCurrency)
        {
            case true when _currentCurrency != _maxCurrency:
                _currentCurrency = Math.Min(_currentCurrency + quantity, _maxCurrency);
                OnInventoryChanged?.Invoke();
                return true;
            
            case true when _currentCurrency == _maxCurrency:
                Debug.LogWarning("Coin purse is full!");
                return false;
        }

        // Try stacking if item is stackable
        if (itemData.Stackable)
        {
            foreach (var slot in _slots)
            {
                if (slot.ItemData == itemData && slot.Quantity < itemData.MaxStack)
                {
                    int spaceInStack = itemData.MaxStack - slot.Quantity;
                    int amountToAdd = Mathf.Min(quantity, spaceInStack);
                    
                    slot.AddQuantity(amountToAdd);
                    quantity -= amountToAdd;

                    if (quantity <= 0)
                    {
                        OnInventoryChanged?.Invoke();
                        return true;
                    }
                }
            }
        }
        
        // Create new slots for remaining items
        while (quantity > 0 && _slots.Count < _maxSlots)
        {
            int stackSize = itemData.Stackable ? 
                Mathf.Min(quantity, itemData.MaxStack) : 1;
            
            var newSlot = new InventorySlot(itemData, stackSize);
            _slots.Add(newSlot);
            
            quantity -= stackSize;
        }
        
        OnInventoryChanged?.Invoke();
        return quantity <= 0;
    }
    
    public bool RemoveItem(ItemData itemData, int quantity = 1)
    {
        if (itemData == null || quantity <= 0) return false;
        
        int remainingToRemove = quantity;
        
        for (int i = _slots.Count - 1; i >= 0; i--)
        {
            var slot = _slots[i];
            
            if (slot.ItemData == itemData)
            {
                int amountToRemove = Mathf.Min(remainingToRemove, slot.Quantity);
                slot.RemoveQuantity(amountToRemove);
                remainingToRemove -= amountToRemove;

                if (slot.IsEmpty)
                {
                    _slots.RemoveAt(i);
                }
                
                if (remainingToRemove <= 0)
                {
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }
        }
        
        OnInventoryChanged?.Invoke();
        return false;
    }
    
    public bool HasItem(ItemData itemData, int quantity = 1)
    {
        int totalQuantity = 0;
        
        foreach (var slot in _slots)
        {
            if (slot.ItemData == itemData)
            {
                totalQuantity += slot.Quantity;
                if (totalQuantity >= quantity)
                    return true;
            }
        }
        
        return false;
    }
    
    public int GetItemQuantity(ItemData itemData)
    {
        int total = 0;
        
        foreach (var slot in _slots)
        {
            if (slot.ItemData == itemData)
                total += slot.Quantity;
        }
        
        return total;
    }
    
    public void SwapSlots(int indexA, int indexB)
    {
        if (indexA < 0 || indexA >= _slots.Count || 
            indexB < 0 || indexB >= _slots.Count)
            return;
            
        (_slots[indexA], _slots[indexB]) = (_slots[indexB], _slots[indexA]);
        OnInventoryChanged?.Invoke();
    }
    
    public bool MergeSlots(int sourceIndex, int targetIndex)
    {
        if (sourceIndex < 0 || sourceIndex >= _slots.Count ||
            targetIndex < 0 || targetIndex >= _slots.Count ||
            sourceIndex == targetIndex)
            return false;
            
        var sourceSlot = _slots[sourceIndex];
        var targetSlot = _slots[targetIndex];
        
        // Can only merge same items and if target is stackable
        if (sourceSlot.ItemData != targetSlot.ItemData || 
            !targetSlot.ItemData.Stackable)
            return false;
            
        int spaceInTarget = targetSlot.ItemData.MaxStack - targetSlot.Quantity;
        int amountToMove = Mathf.Min(sourceSlot.Quantity, spaceInTarget);
        
        if (amountToMove <= 0) return false;
        
        sourceSlot.RemoveQuantity(amountToMove);
        targetSlot.AddQuantity(amountToMove);
        
        if (sourceSlot.IsEmpty)
        {
            _slots.RemoveAt(sourceIndex);
        }
        
        OnInventoryChanged?.Invoke();
        return true;
    }
    
    private float GetItemWeight(ItemData itemData)
    {
        // You can add a weight property to ItemData if needed
        // For now, return a default value
        return 1f;
    }
    
    public void Clear()
    {
        _slots.Clear();
        OnInventoryChanged?.Invoke();
    }
    
    public List<InventorySlot> GetItemsByRarity(Rarity rarity)
    {
        return _slots.FindAll(slot => slot.ItemData.ItemRarity == rarity);
    }
}