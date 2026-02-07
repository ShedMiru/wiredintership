using UnityEngine;
using UnityEngine.UI;

public class PLCLiquidTank : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Drag gambar wadah/tangki kosong di sini (untuk target Outline Merah)")]
    public Image tankBackgroundImage; // TEMPAT BARU: Background Tangki
    public Image fillImage; // Gambar air (Type: Filled Vertical)
    public Button btnPlus;
    public Button btnMinus;

    [Header("Data")]
    public string tankID;
    [Range(0f, 1f)]
    public float currentLevel = 0.5f;

    private PLCController masterController;
    private AutoErrorFeedback feedback;

    public void Setup(PLCController master)
    {
        masterController = master;

        btnPlus.onClick.AddListener(OnPlusClicked);
        btnMinus.onClick.AddListener(OnMinusClicked);

        // Target outline diutamakan ke tankBackgroundImage
        GameObject targetObj = gameObject;
        if (tankBackgroundImage != null) targetObj = tankBackgroundImage.gameObject;
        else if (fillImage != null) targetObj = fillImage.gameObject;

        feedback = targetObj.GetComponent<AutoErrorFeedback>();
        if (feedback == null) feedback = targetObj.AddComponent<AutoErrorFeedback>();

        UpdateVisual();
    }

    private void OnPlusClicked()
    {
        // Cek apakah belum penuh (pakai toleransi kecil agar 0.99 dianggap belum penuh)
        if (currentLevel < 1.0f - 0.01f)
        {
            currentLevel += 0.2f; // Naik 20%

            // 1. Rounding agar rapi (hilangkan koma panjang)
            currentLevel = Mathf.Round(currentLevel * 10f) / 10f;

            // 2. CRITICAL FIX: Kunci (Clamp) agar tidak pernah tembus di atas 1.0
            // Contoh: Jika 0.9 + 0.2 = 1.1, fungsi ini memaksanya jadi 1.0
            currentLevel = Mathf.Clamp(currentLevel, 0f, 1.0f);

            UpdateVisual();
            if (masterController != null) masterController.CheckLogic();
        }
    }

    private void OnMinusClicked()
    {
        // Cek apakah masih ada isinya
        if (currentLevel > 0.0f + 0.01f)
        {
            currentLevel -= 0.2f; // Turun 20%

            // 1. Rounding
            currentLevel = Mathf.Round(currentLevel * 10f) / 10f;

            // 2. CRITICAL FIX: Kunci (Clamp) agar tidak pernah minus
            // Contoh: Jika 0.1 - 0.2 = -0.1, fungsi ini memaksanya jadi 0.0
            currentLevel = Mathf.Clamp(currentLevel, 0f, 1.0f);

            UpdateVisual();
            if (masterController != null) masterController.CheckLogic();
        }
    }

    public void ScrambleLevel()
    {
        // Acak kelipatan 0.2 (0, 0.2, 0.4, 0.6, 0.8, 1.0)
        int rnd = Random.Range(0, 6);
        currentLevel = rnd * 0.2f;
        currentLevel = Mathf.Round(currentLevel * 10f) / 10f;
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (fillImage != null) fillImage.fillAmount = currentLevel;
    }

    public void SetErrorState(bool isError)
    {
        if (feedback != null) feedback.SetError(isError);
    }
}