using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
public enum ItemCategory
{
    A,
    B,
    Count // do not remove; cast to int to get number of categories
}
public readonly struct ItemInfo
{
    public string Name { get; }
    public string Description { get; }
    public string HeroImageFilename { get; }
    public ItemCategory Category { get; }

    public ItemInfo(string name, string description, string heroImageFilename, ItemCategory category)
    {
        Name = name;
        Description = description;
        HeroImageFilename = heroImageFilename;
        Category = category;
    }
}

public enum InventoryItem
{
    ItemA,
    ItemB,
    ItemC
}
public class Inventory
{
    public static readonly IReadOnlyDictionary<InventoryItem, ItemInfo>
        ItemInfoTable = new Dictionary<InventoryItem, ItemInfo>()
    {
        { InventoryItem.ItemA, new("Item A", "Item A Description", "a_light", ItemCategory.A ) },
        { InventoryItem.ItemB, new("Item B", "Item B Description", "b_light", ItemCategory.B ) },
        { InventoryItem.ItemC, new("Item C", "Item C Description", "c_light", ItemCategory.B ) },
    };

    List<InventoryItem>[] categoryItems = new List<InventoryItem>[(int) ItemCategory.Count];

    Dictionary<InventoryItem, int> itemCounts = new();
    IReadOnlyDictionary<InventoryItem, int> ItemCounts { get { return itemCounts; } }

    public Inventory()
    {
        for (int i = 0; i < (int)ItemCategory.Count; i++)
        {
            categoryItems[i] = new();
        }

        // add items for debugging
        AdjustItemCount(InventoryItem.ItemA, 1);
        AdjustItemCount(InventoryItem.ItemB, 2);
        AdjustItemCount(InventoryItem.ItemC, 3);
    }

    public IReadOnlyList<InventoryItem> GetCategoryItems(ItemCategory category)
    {
        return categoryItems[(int)category];
    }

    public int GetItemCount(InventoryItem item)
    {
        return itemCounts.GetValueOrDefault(item, 0);
    }

    public void SetItemCount(InventoryItem item, int count)
    {
        List<InventoryItem> category = categoryItems[(int) ItemInfoTable[item].Category];

        if (count == 0)
        {
            category.Remove(item);
            itemCounts.Remove(item);
        }
        else
        {
            if (!itemCounts.ContainsKey(item)) category.Add(item);
            itemCounts[item] = count;
        }
    }

    public void AdjustItemCount(InventoryItem item, int adjustAmount)
    {
        int count = GetItemCount(item) + adjustAmount;
        SetItemCount(item, count);
    }

    // TODO: item saving and loading for persistence between runs

    public void LoadItems()
    {
        // TODO
    }

    public void SaveItems()
    {
        // TODO
    }
}
