using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PLCController : MonoBehaviour
{
    [System.Serializable]
    public class PLCSlot
    {
        public string slotName;         // Nama Slot (misal "Slot_Fuse_Atas")
        public Image itemDisplayImage;  // Image UI untuk menampilkan barang yg terpasang
        public Button removeButton;     // Tombol kecil untuk cabut barang (opsional)
        public ItemData currentItem;    // Barang apa yang sedang terpasang? (Null jika kosong)
        public ItemData requiredItem;   // Barang apa yang SEHARUSNYA ada di sini? (Kunci Jawaban)
    }

    [System.Serializable]
    public class PLCSwitch
    {
        public string switchName;
        public Toggle toggleUI;
        public bool isOn;
    }

    [Header("Identity")]
    public string plcID = "PLC_A";

    [Header("Components")]
    public List<PLCSlot> slots;      // Drag komponen UI Slot kesini
    public List<PLCSwitch> switches; // Drag komponen Toggle UI kesini

    [Header("Logic Rules (Customizable)")]
    // Jika semua jawaban BENAR (Slot terisi item yang benar & Saklar posisi benar)
    // Maka distrik target akan NYALA.
    public string targetDistrictToPower = "A"; // Mengontrol Distrik A

    [Header("Danger Logic")]
    // Jika Slot X diisi Item Y, maka MELEDAK
    public ItemData explosiveItem;
    public int explosiveSlotIndex;

    private void Start()
    {
        // Setup Listener untuk setiap perubahan
        foreach (var sw in switches)
        {
            if (sw.toggleUI != null)
            {
                // Menggunakan delegate agar saat toggle berubah, logika langsung jalan
                sw.toggleUI.onValueChanged.AddListener(delegate { OnStateChanged(); });
            }
        }

        RefreshUI();
        OnStateChanged(); // PERBAIKAN: Sebelumnya tertulis 'CheckLogicRealtime()'
    }

    // Dipanggil saat Player men-Drag barang dari Inventory UI ke area Slot di PLC
    // Anda perlu trigger event dari UI Slot ke fungsi ini
    public void TryInstallItem(int slotIndex, ItemData item)
    {
        if (slotIndex < 0 || slotIndex >= slots.Count) return;

        PLCSlot slot = slots[slotIndex];

        // 1. Cek apakah Slot kosong
        if (slot.currentItem != null)
        {
            Debug.Log("Slot sudah terisi! Cabut dulu.");
            return;
        }

        // 2. Pasang Item
        slot.currentItem = item;
        PlayerInventory.Instance.RemoveItem(item); // Hapus dari tas

        RefreshUI();
        OnStateChanged(); // Cek logika segera (Realtime)
    }

    public void RemoveItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Count) return;

        // Opsional: Logika cabut barang (hancur atau kembali ke inventory)
        // Disini kita set null saja (barang hancur/dibuang)
        slots[slotIndex].currentItem = null;

        RefreshUI();
        OnStateChanged();
    }

    private void RefreshUI()
    {
        // Update visual slot
        foreach (var slot in slots)
        {
            if (slot.itemDisplayImage != null)
            {
                if (slot.currentItem != null)
                {
                    slot.itemDisplayImage.sprite = slot.currentItem.icon;
                    slot.itemDisplayImage.enabled = true;
                    slot.itemDisplayImage.color = Color.white;
                }
                else
                {
                    slot.itemDisplayImage.sprite = null;
                    slot.itemDisplayImage.enabled = false; // Sembunyikan jika kosong
                }
            }
        }
    }

    // Jantung Logika Realtime
    private void OnStateChanged()
    {
        // 1. Update data switch internal
        foreach (var sw in switches)
        {
            if (sw.toggleUI != null)
                sw.isOn = sw.toggleUI.isOn;
        }

        // 2. CEK BAHAYA (Explosion Check)
        // Contoh: Jika Slot explosiveSlotIndex diisi "explosiveItem" -> BOOM
        // Pastikan index valid sebelum cek
        if (slots.Count > explosiveSlotIndex && explosiveSlotIndex >= 0)
        {
            if (slots[explosiveSlotIndex].currentItem == explosiveItem)
            {
                // Tambahan logika: hanya meledak jika saklar tertentu ON (misal saklar pertama)
                if (switches.Count > 0 && switches[0].isOn)
                {
                    GlobalPuzzleManager.Instance.TriggerExplosion(plcID);
                    return; // Stop logic lain agar status tidak sempat berubah jadi 'Benar'
                }
            }
        }

        // 3. VALIDASI PUZZLE
        bool isPuzzleSolved = true;

        // Cek Kelengkapan Komponen
        foreach (var slot in slots)
        {
            if (slot.requiredItem != null)
            {
                // Jika slot kosong ATAU isinya salah -> Belum solved
                if (slot.currentItem != slot.requiredItem)
                {
                    isPuzzleSolved = false;
                }
            }
        }

        // Cek Saklar (Contoh sederhana: Switch pertama harus ON agar listrik mengalir)
        // Anda bisa kustomisasi logika saklar di sini
        if (switches.Count > 0)
        {
            // Misal: Semua saklar harus ON (atau sesuaikan dengan puzzle Anda)
            // if (!switches[0].isOn) isPuzzleSolved = false; 
        }

        // 4. KIRIM HASIL KE GLOBAL MANAGER
        if (GlobalPuzzleManager.Instance != null)
        {
            GlobalPuzzleManager.Instance.SetDistrictStatus(targetDistrictToPower, isPuzzleSolved);
        }
    }
}