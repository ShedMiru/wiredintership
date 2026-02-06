using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GlobalPuzzleManager : MonoBehaviour
{
    public static GlobalPuzzleManager Instance;

    [Header("Status Listrik Distrik")]
    // Sekarang bisa di-edit di Inspector saat Play Mode dan akan berefek langsung
    public bool districtA_Active;
    public bool districtB_Active;
    public bool districtC_Active;
    public bool districtD_Active;

    [Header("Referensi UI Map")]
    [SerializeField] private Image indicatorA;
    [SerializeField] private Image indicatorB;
    [SerializeField] private Image indicatorC;
    [SerializeField] private Image indicatorD;

    [Header("Pengaturan Visual")]
    [SerializeField] private Color powerOnColor = Color.green;
    [SerializeField] private Color powerOffColor = Color.red;
    [SerializeField] private float blinkSpeed = 3.0f; // Kecepatan kedip
    [Range(0f, 1f)]
    [SerializeField] private float minBrightness = 0.3f; // Seberapa gelap saat fase redup (0.3 = 30% brightness)

    // Variabel pembantu untuk mendeteksi perubahan di Inspector
    private bool lastA, lastB, lastC, lastD;

    // Menyimpan Coroutine yang sedang berjalan agar animasi tiap lampu independen
    private Coroutine routineA, routineB, routineC, routineD;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Set kondisi awal & sinkronisasi variabel pembantu
        lastA = districtA_Active;
        lastB = districtB_Active;
        lastC = districtC_Active;
        lastD = districtD_Active;

        UpdateMapVisuals(true); // Update instan saat mulai
    }

    private void Update()
    {
        // --- LOGIKA DETEKSI PERUBAHAN INSPECTOR ---
        // Ini mengecek setiap frame: "Apakah centangan di Inspector beda sama data terakhir?"
        // Jika beda, berarti Anda baru saja mengubahnya, maka jalankan update visual.

        if (districtA_Active != lastA || districtB_Active != lastB ||
            districtC_Active != lastC || districtD_Active != lastD)
        {
            // Simpan data terbaru
            lastA = districtA_Active;
            lastB = districtB_Active;
            lastC = districtC_Active;
            lastD = districtD_Active;

            // Update visualnya
            UpdateMapVisuals();
        }
    }

    // Fungsi ini dipanggil oleh PLC Controller
    public void SetDistrictStatus(string districtID, bool isActive)
    {
        switch (districtID)
        {
            case "A": districtA_Active = isActive; break;
            case "B": districtB_Active = isActive; break;
            case "C": districtC_Active = isActive; break;
            case "D": districtD_Active = isActive; break;
        }
        // Tidak perlu panggil UpdateMapVisuals disini karena sudah dihandle oleh Update() di atas
    }

    private void UpdateMapVisuals(bool instant = false)
    {
        HandleIndicator(indicatorA, districtA_Active, ref routineA, instant);
        HandleIndicator(indicatorB, districtB_Active, ref routineB, instant);
        HandleIndicator(indicatorC, districtC_Active, ref routineC, instant);
        HandleIndicator(indicatorD, districtD_Active, ref routineD, instant);
    }

    private void HandleIndicator(Image img, bool active, ref Coroutine currentRoutine, bool instant)
    {
        if (img == null) return;

        // Tentukan warna dasar target (Merah/Hijau)
        Color targetBaseColor = active ? powerOnColor : powerOffColor;

        // Reset coroutine lama agar tidak tumpang tindih
        if (currentRoutine != null) StopCoroutine(currentRoutine);

        // Mulai animasi kedip (looping forever)
        currentRoutine = StartCoroutine(AnimateBlinking(img, targetBaseColor));
    }

    /// <summary>
    /// Animasi berulang (Loop) untuk membuat efek berkedip/pulsing
    /// </summary>
    private IEnumerator AnimateBlinking(Image img, Color targetBaseColor)
    {
        // 1. Fase Transisi Cepat (Opsional: agar pergantian warna smooth dulu)
        float t = 0f;
        Color startColor = img.color;

        // Transisi warna dalam 0.25 detik sebelum mulai blinking
        while (t < 1f)
        {
            t += Time.deltaTime * 4f;
            img.color = Color.Lerp(startColor, targetBaseColor, t);
            yield return null;
        }

        // 2. Fase Looping (Kedip-kedip teknikal)
        while (true)
        {
            // Menghasilkan nilai gelombang 0 s/d 1 menggunakan Sinus
            // (Sin(time) + 1) / 2 mengubah range -1..1 menjadi 0..1
            float pulse = (Mathf.Sin(Time.time * blinkSpeed) + 1f) / 2f;

            // Hitung warna redup (Darker version of target color)
            // Kita pakai konstruktor Color baru agar Alpha tidak ikut mengecil (tetap solid)
            Color dimmedColor = new Color(
                targetBaseColor.r * minBrightness,
                targetBaseColor.g * minBrightness,
                targetBaseColor.b * minBrightness,
                targetBaseColor.a
            );

            // Lerp bolak-balik antara Terang dan Redup
            img.color = Color.Lerp(dimmedColor, targetBaseColor, pulse);

            yield return null;
        }
    }

    public void TriggerExplosion(string plcName)
    {
        Debug.LogError($"BAHAYA! PLC {plcName} MELEDAK!");
    }
}