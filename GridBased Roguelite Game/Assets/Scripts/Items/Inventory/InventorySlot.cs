using System;
using UnityEngine;

[Serializable]
public class InventorySlot
{
    public event Action OnSlotChanged;
    
    [SerializeField] private ItemData _itemData;
    [SerializeField] private int _quantity;
    
    public ItemData ItemData => _itemData;
    public int Quantity => _quantity;
    public bool IsEmpty => _itemData == null || _quantity <= 0;
    public bool IsFull => _itemData != null && _itemData.Stackable && _quantity >= _itemData.MaxStack;
    
    public InventorySlot(ItemData itemData, int quantity = 1)
    {
        _itemData = itemData;
        _quantity = quantity;
    }
    
    public void AddQuantity(int amount)
    {
        if (_itemData == null || !_itemData.Stackable) return;
        
        _quantity = Mathf.Min(_quantity + amount, _itemData.MaxStack);
        OnSlotChanged?.Invoke();
    }
    
    public void RemoveQuantity(int amount)
    {
        if (_itemData == null) return;
        
        _quantity = Mathf.Max(0, _quantity - amount);
        
        if (_quantity <= 0)
        {
            _itemData = null;
        }
        
        OnSlotChanged?.Invoke();
    }
    
    public void SetSlot(ItemData newItem, int quantity)
    {
        _itemData = newItem;
        _quantity = quantity;
        OnSlotChanged?.Invoke();
    }
    
    public void Clear()
    {
        _itemData = null;
        _quantity = 0;
        OnSlotChanged?.Invoke();
    }
    
    public bool CanStackWith(ItemData otherItem)
    {
        return _itemData != null && 
               otherItem != null && 
               _itemData == otherItem && 
               _itemData.Stackable && 
               _quantity < _itemData.MaxStack;
    }
}