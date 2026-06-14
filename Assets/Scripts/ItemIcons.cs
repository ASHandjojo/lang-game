using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "ItemIcons", menuName = "Scriptable Objects/ItemIcons")]
public sealed class ItemIcons : ScriptableObject
{
    private static ItemIcons instance;
    public static ItemIcons Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<ItemIcons>("ItemIcons");
            }
            return instance;
        }
    }

    private readonly Dictionary<InventoryItem, Sprite> icons = new();
    public IReadOnlyDictionary<InventoryItem, Sprite> Icons => icons; 

    void OnEnable()
    {
        Debug.Log("Loading Item Icons");

        icons.Clear();

        icons.Add(InventoryItem.ItemA, Resources.Load<Sprite>("ItemIcons/" + 'a' + "_light"));
        icons.Add(InventoryItem.ItemB, Resources.Load<Sprite>("ItemIcons/" + 'b' + "_light"));
        icons.Add(InventoryItem.ItemC, Resources.Load<Sprite>("ItemIcons/" + 'c' + "_light"));
    }
}
