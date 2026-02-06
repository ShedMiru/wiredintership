using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Puzzle/Item Data")]
public class ItemData : ScriptableObject
{
    public string itemID;        // ID Unik (misal: "FUSE_5A")
    public string itemName;      // Nama Tampilan
    public Sprite icon;          // Gambar untuk UI
    public GameObject physicsPrefab; // Prefab jika dijatuhkan kembali ke meja (opsional)
}