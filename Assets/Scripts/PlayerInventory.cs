using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance;

    [Header("Inventory Settings")]
    public int maxSlots = 2;
    public List<ItemData> carriedItems = new List<ItemData>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    // Mengambil barang dari meja
    public bool AddItem(ItemData item)
    {
        if (carriedItems.Count >= maxSlots)
        {
            Debug.Log("Inventory Penuh! Hanya bisa bawa 2 barang.");
            return false;
        }

        carriedItems.Add(item);
        Debug.Log($"Mengambil: {item.itemName}");
        return true;
    }

    // Memasang barang ke PLC (Barang hilang dari tas)
    public void RemoveItem(ItemData item)
    {
        if (carriedItems.Contains(item))
        {
            carriedItems.Remove(item);
        }
    }

    // Cek apakah punya barang tertentu (untuk validasi drag)
    public bool HasItem(ItemData item)
    {
        return carriedItems.Contains(item);
    }
}