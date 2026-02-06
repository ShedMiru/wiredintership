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
    [SerializeField] private float blinkSpeed = 4.0f; // Diubah ke 4.0f agar sama persis dengan speed Outline di PLC
    [Range(0f, 1f)]
    [SerializeField] private float minBrightness = 0.3f;

    private bool lastA, lastB, lastC, lastD;
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
        lastA = districtA_Active;
        lastB = districtB_Active;
        lastC = districtC_Active;
        lastD = districtD_Active;

        UpdateMapVisuals(true);
    }

    private void Update()
    {
        // Deteksi perubahan manual di Inspector
        if (districtA_Active != lastA || districtB_Active != lastB ||
            districtC_Active != lastC || districtD_Active != lastD)
        {
            lastA = districtA_Active;
            lastB = districtB_Active;
            lastC = districtC_Active;
            lastD = districtD_Active;

            UpdateMapVisuals();
        }
    }

    public void SetDistrictStatus(string districtID, bool isActive)
    {
        switch (districtID)
        {
            case "A": districtA_Active = isActive; break;
            case "B": districtB_Active = isActive; break;
            case "C": districtC_Active = isActive; break;
            case "D": districtD_Active = isActive; break;
        }
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

        Color targetBaseColor = active ? powerOnColor : powerOffColor;

        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(AnimateBlinking(img, targetBaseColor));
    }

    private IEnumerator AnimateBlinking(Image img, Color targetBaseColor)
    {
        // 1. Fase Transisi Cepat (Agar warna berubah smooth dulu ke target)
        float t = 0f;
        Color startColor = img.color;

        while (t < 1f)
        {
            t += Time.deltaTime * 4f;
            img.color = Color.Lerp(startColor, targetBaseColor, t);
            yield return null;
        }

        // 2. Fase Looping (Kedip-kedip SEIRAMA dengan Outline)
        while (true)
        {
            // PERBAIKAN: Menggunakan PingPong (Segitiga) alih-alih Sin (Gelombang)
            // Ini menyamakan ritme dengan skrip AutoErrorFeedback di PLC Controller.
            float pulse = Mathf.PingPong(Time.time * blinkSpeed, 1f);

            // Hitung warna redup
            Color dimmedColor = new Color(
                targetBaseColor.r * minBrightness,
                targetBaseColor.g * minBrightness,
                targetBaseColor.b * minBrightness,
                targetBaseColor.a
            );

            // Interpolasi warna
            img.color = Color.Lerp(dimmedColor, targetBaseColor, pulse);

            yield return null;
        }
    }

    public void TriggerExplosion(string plcName)
    {
        // PERBAIKAN: Menggunakan LogWarning agar tidak dianggap Fatal Error oleh Unity Editor
        // Ini mencegah game terasa 'Crash' atau 'Pause' saat spamming terjadi.
        Debug.LogWarning($"[SYSTEM ALERT] PLC {plcName} mengalami kegagalan kritis/ledakan!");
    }
}