using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(Image))]
public class DistrictStatusOverrider : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Identity")]
    [Tooltip("ID Distrik yang diwakili oleh objek ini (A, B, C, atau D)")]
    public string districtID = "A";

    [Header("Visual Settings")]
    [Tooltip("Transparansi bayangan saat di-drag")]
    public float ghostAlpha = 0.8f;
    [Tooltip("Transparansi objek asli saat sedang di-drag (0 = hilang total)")]
    public float originalAlphaWhileDragging = 0.3f;

    // Internal Variables
    private CanvasGroup originalCanvasGroup;
    private GameObject dragGhost;
    private RectTransform dragGhostRect;
    private Canvas rootCanvas;

    private void Awake()
    {
        originalCanvasGroup = GetComponent<CanvasGroup>();
        // Pastikan CanvasGroup ada untuk kontrol alpha & raycast
        if (originalCanvasGroup == null) originalCanvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 1. Cari Root Canvas agar Ghost bisa melayang di atas semua UI
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null) rootCanvas = canvas.rootCanvas;

        // 2. Buat "Ghost" (Visual yang ikut mouse)
        CreateDragGhost();

        // 3. Sembunyikan/Redupkan objek asli agar terlihat seperti "dipindahkan"
        originalCanvasGroup.alpha = originalAlphaWhileDragging;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Gerakkan Ghost mengikuti mouse
        if (dragGhostRect != null && rootCanvas != null)
        {
            // Menggunakan posisi layar ke posisi lokal canvas agar akurat di mode Screen Space - Camera
            Vector2 pos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rootCanvas.transform as RectTransform,
                eventData.position,
                rootCanvas.worldCamera,
                out pos);

            dragGhostRect.position = rootCanvas.transform.TransformPoint(pos);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 1. Hancurkan Ghost
        if (dragGhost != null) Destroy(dragGhost);

        // 2. Kembalikan visual objek asli
        originalCanvasGroup.alpha = 1f;

        // 3. Deteksi Target Drop (Apakah dijatuhkan di atas Distrik lain?)
        GameObject targetObj = eventData.pointerEnter;

        if (targetObj != null)
        {
            // Cari skrip DistrictStatusOverrider di objek tujuan
            DistrictStatusOverrider targetScript = targetObj.GetComponent<DistrictStatusOverrider>();

            // Jika target valid DAN bukan diri sendiri
            if (targetScript != null && targetScript != this)
            {
                ApplyCheat(targetScript);
            }
        }
    }

    /// <summary>
    /// Logika Inti Cheating: Copy status SAYA ke status TARGET
    /// </summary>
    private void ApplyCheat(DistrictStatusOverrider target)
    {
        if (GlobalPuzzleManager.Instance == null) return;

        // A. Ambil Status SAYA (Source) saat ini (Realtime dari Global Manager)
        bool myStatus = GetCurrentStatus(this.districtID);

        // B. Timpa Status TARGET secara paksa
        Debug.LogWarning($"[CHEAT] Menyalin status {this.districtID} ({(myStatus ? "HIJAU" : "MERAH")}) ke {target.districtID}!");

        GlobalPuzzleManager.Instance.SetDistrictStatus(target.districtID, myStatus);

        // C. (Opsional) Tambahkan efek suara atau partikel di sini jika mau
    }

    private bool GetCurrentStatus(string id)
    {
        switch (id)
        {
            case "A": return GlobalPuzzleManager.Instance.districtA_Active;
            case "B": return GlobalPuzzleManager.Instance.districtB_Active;
            case "C": return GlobalPuzzleManager.Instance.districtC_Active;
            case "D": return GlobalPuzzleManager.Instance.districtD_Active;
            default: return false;
        }
    }

    private void CreateDragGhost()
    {
        // Duplikat objek ini untuk jadi ghost
        dragGhost = Instantiate(gameObject, rootCanvas.transform);
        dragGhost.name = gameObject.name + "_Ghost";

        // Hapus skrip logika dari ghost agar tidak error/berat
        Destroy(dragGhost.GetComponent<DistrictStatusOverrider>());

        // Atur Visual Ghost
        CanvasGroup ghostGroup = dragGhost.GetComponent<CanvasGroup>();
        if (ghostGroup == null) ghostGroup = dragGhost.AddComponent<CanvasGroup>();

        ghostGroup.alpha = ghostAlpha;
        ghostGroup.blocksRaycasts = false; // PENTING: Agar raycast tembus ke target di bawahnya saat di-drop

        // Simpan RectTransform untuk pergerakan
        dragGhostRect = dragGhost.GetComponent<RectTransform>();
    }
}