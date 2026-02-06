using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PLCInventoryDisplay : MonoBehaviour
{
    public static PLCInventoryDisplay Instance;

    [Header("UI References")]
    [Tooltip("Prefab UI Image yang ada script DraggableInventoryItem")]
    public GameObject inventoryItemPrefab;

    [Tooltip("Panel Horizontal Layout Group tempat spawn icon")]
    public Transform contentArea;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        // Saat Canvas PLC nyala, langsung update isi tas
        RefreshInventoryUI();
    }

    public void RefreshInventoryUI()
    {
        // 1. Hapus semua icon lama (bersihkan tampilan)
        foreach (Transform child in contentArea)
        {
            Destroy(child.gameObject);
        }

        // 2. Spawn icon baru berdasarkan data di PlayerInventory
        if (PlayerInventory.Instance != null)
        {
            foreach (ItemData item in PlayerInventory.Instance.carriedItems)
            {
                // Buat icon baru
                GameObject newIcon = Instantiate(inventoryItemPrefab, contentArea);

                // Set Gambarnya
                Image img = newIcon.GetComponent<Image>();
                if (img) img.sprite = item.icon;

                // Suntikkan Datanya ke skrip Draggable
                DraggableInventoryItem dragScript = newIcon.GetComponent<DraggableInventoryItem>();
                if (dragScript) dragScript.myData = item;
            }
        }
    }
}