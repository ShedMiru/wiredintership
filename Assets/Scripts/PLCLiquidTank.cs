using UnityEngine;
using UnityEngine.UI;

public class PLCLiquidTank : MonoBehaviour
{
    [Header("UI References")]
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

        // PENTING: Outline harus menempel pada objek yang punya komponen 'Image'
        // Kita prioritaskan Fill Image (Airnya) agar outline mengikuti bentuk air
        GameObject targetObj = fillImage != null ? fillImage.gameObject : gameObject;

        // Pastikan tidak dobel
        feedback = targetObj.GetComponent<AutoErrorFeedback>();
        if (feedback == null) feedback = targetObj.AddComponent<AutoErrorFeedback>();

        UpdateVisual();
    }

    private void OnPlusClicked()
    {
        if (currentLevel < 1.0f)
        {
            currentLevel += 0.1f;
            currentLevel = Mathf.Round(currentLevel * 10f) / 10f;
            UpdateVisual();

            // Panggil cek logika segera agar realtime
            if (masterController != null) masterController.CheckLogic();
        }
    }

    private void OnMinusClicked()
    {
        if (currentLevel > 0.0f)
        {
            currentLevel -= 0.1f;
            currentLevel = Mathf.Round(currentLevel * 10f) / 10f;
            UpdateVisual();

            if (masterController != null) masterController.CheckLogic();
        }
    }

    public void ScrambleLevel()
    {
        int rnd = Random.Range(0, 11);
        currentLevel = rnd * 0.1f;
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (fillImage != null) fillImage.fillAmount = currentLevel;
    }

    // Fungsi ini dipanggil terus menerus oleh PLC Controller
    public void SetErrorState(bool isError)
    {
        if (feedback != null) feedback.SetError(isError);
    }
}