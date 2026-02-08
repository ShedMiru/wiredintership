using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance;

    [Header("Konfigurasi Tas")]
    public int maxSlots = 2; // Maksimal bawa 2 barang

    // List untuk menyimpan barang yang sedang dibawa
    public List<ItemData> carriedItems = new List<ItemData>();

    private void Awake()
    {
        // PERBAIKAN: Gunakan Scene-Specific Singleton.
        // Hapus DontDestroyOnLoad agar inventory ter-reset saat restart level/pindah scene.

        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); <--- HAPUS ATAU KOMENTARI INI
        }
    }

    // PENTING: Bersihkan static instance saat object hancur (pindah scene)
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Mencoba memasukkan barang ke tas.
    /// Return TRUE jika berhasil, FALSE jika tas penuh.
    /// </summary>
    public bool AddItem(ItemData item)
    {
        // Cek apakah tas penuh
        if (carriedItems.Count >= maxSlots)
        {
            Debug.LogWarning("Tas Penuh! Tidak bisa mengambil barang lagi.");
            return false;
        }

        // Cek apakah barang sudah ada
        if (carriedItems.Contains(item))
        {
            Debug.Log("Kamu sudah punya barang ini.");
            return false;
        }

        carriedItems.Add(item);
        Debug.Log($"BERHASIL MENYIMPAN: {item.itemName} | Total Isi: {carriedItems.Count}/{maxSlots}");

        // Refresh UI jika ada displayer aktif
        if (PLCInventoryDisplay.Instance != null)
            PLCInventoryDisplay.Instance.RefreshInventoryUI();

        return true;
    }

    /// <summary>
    /// Menghapus barang dari tas (dipakai nanti saat dipasang ke PLC)
    /// </summary>
    public void RemoveItem(ItemData item)
    {
        if (carriedItems.Contains(item))
        {
            carriedItems.Remove(item);
            Debug.Log($"Barang {item.itemName} digunakan/dihapus.");

            // Refresh UI jika ada displayer aktif
            if (PLCInventoryDisplay.Instance != null)
                PLCInventoryDisplay.Instance.RefreshInventoryUI();
        }
    }

    /// <summary>
    /// Helper untuk cek apakah kita punya barang tertentu
    /// </summary>
    public bool HasItem(ItemData item)
    {
        return carriedItems.Contains(item);
    }
}