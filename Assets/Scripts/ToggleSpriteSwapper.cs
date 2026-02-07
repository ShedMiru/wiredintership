using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class ToggleSpriteSwapper : MonoBehaviour
{
    [Header("Target Image")]
    [Tooltip("Image UI yang gambarnya akan diganti-ganti (Biasanya Background)")]
    public Image targetImage;

    [Header("Switch Sprites")]
    public Sprite spriteOn;  // Gambar saat Saklar Hidup
    public Sprite spriteOff; // Gambar saat Saklar Mati

    private Toggle toggle;

    private void Awake()
    {
        toggle = GetComponent<Toggle>();
    }

    private void Start()
    {
        // Auto-detect target image jika kosong (mengambil dari Target Graphic toggle)
        if (targetImage == null && toggle.targetGraphic != null)
        {
            targetImage = toggle.targetGraphic as Image;
        }

        // Daftarkan listener agar berubah saat diklik
        toggle.onValueChanged.AddListener(UpdateSprite);

        // Set gambar awal sesuai status saat ini
        UpdateSprite(toggle.isOn);
    }

    // Fungsi untuk menukar gambar
    public void UpdateSprite(bool isOn)
    {
        if (targetImage != null)
        {
            if (isOn && spriteOn != null)
            {
                targetImage.sprite = spriteOn;
            }
            else if (!isOn && spriteOff != null)
            {
                targetImage.sprite = spriteOff;
            }
        }
    }

    // Helper untuk editor: Mengisi referensi otomatis saat script dipasang
    private void OnValidate()
    {
        if (toggle == null) toggle = GetComponent<Toggle>();
        if (targetImage == null && toggle != null && toggle.targetGraphic != null)
        {
            targetImage = toggle.targetGraphic as Image;
        }
    }
}