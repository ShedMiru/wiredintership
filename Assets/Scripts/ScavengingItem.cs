using UnityEngine;
using UnityEngine.EventSystems;

// Wajib punya Rigidbody2D dan Collider2D agar fisika bekerja
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class ScavengingItem : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Data Barang")]
    [Tooltip("Kosongkan jika ini barang sampah/pengganggu. Isi jika ini barang target.")]
    public ItemData itemData;

    [Header("Pengaturan Fisika")]
    [Tooltip("Seberapa responsif benda mengikuti mouse saat di-drag. Nilai lebih besar lebih nempel.")]
    [SerializeField] private float dragResponsiveness = 30f;
    [Tooltip("Kekuatan lempar saat dilepas setelah di-drag cepat.")]
    [SerializeField] private float throwForceMultiplier = 0.1f;

    private Rigidbody2D rb;
    private Canvas rootCanvas;
    private bool isDragging = false;

    // Variabel untuk membedakan antara "Geser/Drag" dan "Klik/Ambil"
    private float clickTime;
    private Vector2 clickPositionStart;
    private Vector2 lastPosForThrow;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rootCanvas = GetComponentInParent<Canvas>();

        // Pastikan settingan rigidbody benar untuk mode top-down
        rb.gravityScale = 0;
        // PENTING: Continuous detection mencegah tembus dinding saat gerak cepat
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isDragging = true;

        // Catat waktu dan posisi awal untuk logika membedakan Klik vs Drag
        clickTime = Time.time;
        clickPositionStart = eventData.position;
        lastPosForThrow = rb.position;

        // Saat mulai dipegang, reset kecepatan putar agar lebih mudah dikontrol
        rb.angularVelocity = 0f;
        // Kita TIDAK membuat kinematic, agar tetap bisa menabrak benda lain saat di-drag
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Dapatkan posisi target mouse di dunia nyata
        Vector2 targetWorldPos = GetMouseWorldPosition();

        // --- PERBAIKAN UTAMA (ANTI TEMBUS DINDING) ---
        // Daripada teleportasi (MovePosition), kita gunakan kecepatan (Velocity).
        // Kita hitung arah dari benda ke mouse.
        Vector2 directionToMouse = targetWorldPos - rb.position;

        // Kita set kecepatan benda menuju mouse. 
        // Semakin jauh mouse, semakin cepat dia mengejar (dikalikan dragResponsiveness).
        // Ini memungkinkan sistem fisika mendeteksi tabrakan di sepanjang jalur.
        rb.velocity = directionToMouse * dragResponsiveness;

        // Simpan posisi untuk perhitungan lemparan nanti
        lastPosForThrow = targetWorldPos;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;

        // --- LOGIKA LEMPARAN REALISTIS ---
        // Hitung seberapa cepat mouse bergerak tepat sebelum dilepas
        Vector2 currentPos = GetMouseWorldPosition();
        Vector2 throwVector = currentPos - lastPosForThrow;

        // Terapkan kecepatan akhir sebagai lemparan, dikali multiplier agar tidak terlalu kencang
        // Gunakan velocity saat ini sebagai basis agar transisi mulus
        rb.velocity = throwVector * throwForceMultiplier * 100f; // dikali 100 untuk kompensasi delta kecil

        // --- LOGIKA KLIK vs DRAG ---
        // Hitung apakah ini KLIK (diam) atau DRAG (gerak)
        float distanceMoved = Vector2.Distance(eventData.position, clickPositionStart);
        float clickDuration = Time.time - clickTime;

        // Jika gerakannya sangat sedikit (di bawah 20 pixel) DAN waktunya singkat (< 0.3 detik)
        // Maka anggap ini KLIK (Ingin mengambil barang)
        // Toleransi jarak sedikit diperbesar agar tidak terlalu sensitif
        if (distanceMoved < 20f && clickDuration < 0.3f)
        {
            TryPickupItem();
        }
    }

    private void TryPickupItem()
    {
        // Hentikan benda saat mencoba diambil
        rb.velocity = Vector2.zero;

        // Hanya ambil jika ada ItemData (Bukan sampah)
        if (itemData != null && PlayerInventory.Instance != null)
        {
            if (PlayerInventory.Instance.AddItem(itemData))
            {
                // Efek visual sukses (misal: mengecil lalu hilang) - bisa ditambahkan nanti
                Destroy(gameObject);
            }
            else
            {
                Debug.Log("Tas penuh, tidak bisa diambil!");
                // Opsional: Beri sedikit dorongan kecil sebagai feedback negatif
                rb.AddForce(Random.insideUnitCircle * 1f, ForceMode2D.Impulse);
            }
        }
        else
        {
            // Ini sampah
            Debug.Log("Ini hanya sampah, tidak berguna.");
            // Opsional: Add force random biar kerasa 'ditolak'
            rb.AddForce(Random.insideUnitCircle * 3f, ForceMode2D.Impulse);
        }
    }

    // Helper untuk mengubah posisi mouse di layar ke posisi dunia (karena Canvas Camera)
    private Vector3 GetMouseWorldPosition()
    {
        // Pastikan menggunakan Camera.main jika di mode Screen Space Camera
        Camera cam = rootCanvas.worldCamera != null ? rootCanvas.worldCamera : Camera.main;

        if (rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay && cam != null)
        {
            Vector3 screenPos = Input.mousePosition;
            // Penting: jarak Z harus sesuai dengan jarak canvas plane
            screenPos.z = rootCanvas.planeDistance;
            return cam.ScreenToWorldPoint(screenPos);
        }

        // Fallback jika setup canvas salah (tapi ini tidak akan bekerja baik untuk fisika)
        return transform.position;
    }
}