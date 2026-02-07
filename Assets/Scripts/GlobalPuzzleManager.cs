using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GlobalPuzzleManager : MonoBehaviour
{
    public static GlobalPuzzleManager Instance;

    [Header("Status Listrik Distrik")]
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
    [SerializeField] private float blinkSpeed = 4.0f;
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
        // Safety Check: Jika indikator sudah hancur (misal pindah scene), stop logic update UI
        if (indicatorA == null || indicatorB == null || indicatorC == null || indicatorD == null) return;

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
        // Safety check awal
        if (img == null) yield break;

        float t = 0f;
        Color startColor = img.color;

        // 1. Fase Transisi
        while (t < 1f)
        {
            // CRITICAL FIX: Cek apakah gambar masih ada di setiap frame
            // Jika pindah scene, img akan menjadi null (destroyed object)
            if (img == null) yield break;

            t += Time.deltaTime * 4f;
            img.color = Color.Lerp(startColor, targetBaseColor, t);
            yield return null;
        }

        // 2. Fase Looping
        while (true)
        {
            // CRITICAL FIX: Cek null di dalam infinite loop
            if (img == null) yield break;

            float pulse = Mathf.PingPong(Time.time * blinkSpeed, 1f);

            Color dimmedColor = new Color(
                targetBaseColor.r * minBrightness,
                targetBaseColor.g * minBrightness,
                targetBaseColor.b * minBrightness,
                targetBaseColor.a
            );

            img.color = Color.Lerp(dimmedColor, targetBaseColor, pulse);

            yield return null;
        }
    }

    public void TriggerExplosion(string plcName)
    {
        Debug.LogWarning($"[SYSTEM ALERT] PLC {plcName} mengalami kegagalan kritis/ledakan!");
    }
}