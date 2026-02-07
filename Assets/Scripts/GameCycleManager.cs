using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class GameCycleManager : MonoBehaviour
{
    public static GameCycleManager Instance;

    [Header("Game Settings")]
    public float totalTime = 300f;
    public float panicThreshold = 180f;
    public string gameOverSceneName = "Scene_GameOver";
    public string winSceneName = "Scene_Win";

    [Header("UI References")]
    public Text timerText;
    public GameObject killSwitchButton;
    public GameObject warningAlertUI; // Pastikan ini punya komponen Text (Legacy)
    public GameObject warningAlertUI2;
    public Image fadeOutPanel;

    [Header("Audio System")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;
    public AudioClip normalBGM;
    public AudioClip panicBGM;
    public AudioClip warningAlarmSFX;
    public AudioClip powerOutageSFX;

    [Header("Cycle Logic")]
    public List<PLCController> chaosTargets;
    public List<PLCController> allPLCs;

    [Header("Story Events")]
    public UnityEvent onCycle1Reached;
    public UnityEvent onMistakeMade;
    public UnityEvent openingEvent;

    // Internal State
    private float currentTime;
    private bool isPanicMode = false;
    private bool hasReachedCycle1 = false;
    private bool isGameEnded = false;
    private bool introCheck = true;


    private int lastGreenCount = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        currentTime = totalTime;

        if (bgmSource != null && normalBGM != null)
        {
            bgmSource.clip = normalBGM;
            bgmSource.loop = true;
            bgmSource.Play();
        }

        if (killSwitchButton) killSwitchButton.SetActive(false);
        if (warningAlertUI) warningAlertUI.SetActive(false);
        if (fadeOutPanel)
        {
            fadeOutPanel.canvasRenderer.SetAlpha(0f);
        }

        openingEvent.Invoke();
    }

    private void Update()
    {
        if (isGameEnded) return;

        HandleTimer();
        CheckDistrictStatus();
    }

    public void IntroSequenceEnd()
    {
        introCheck = false;
    }

    private void HandleTimer()
    {
        if (introCheck) return;
        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime;
            UpdateTimerUI();

            if (currentTime <= panicThreshold && !isPanicMode)
            {
                ActivatePanicMode();
            }
        }
        else
        {
            currentTime = 0;
            StartCoroutine(GameOverSequence());
        }
    }

    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(currentTime / 60);
            int seconds = Mathf.FloorToInt(currentTime % 60);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

            if (isPanicMode) timerText.color = Color.red;
        }
    }

    private void CheckDistrictStatus()
    {
        if (GlobalPuzzleManager.Instance == null) return;

        int currentGreenCount = 0;
        if (GlobalPuzzleManager.Instance.districtA_Active) currentGreenCount++;
        if (GlobalPuzzleManager.Instance.districtB_Active) currentGreenCount++;
        if (GlobalPuzzleManager.Instance.districtC_Active) currentGreenCount++;
        if (GlobalPuzzleManager.Instance.districtD_Active) currentGreenCount++;

        // --- DETEKSI CYCLE 1 (SEMUA HIJAU) ---
        if (currentGreenCount == 4)
        {
            if (!hasReachedCycle1 || hasReachedCycle1)
            {
                if (lastGreenCount != 4)
                {
                    TriggerCycle1Chaos();
                }
            }
        }

        // --- DETEKSI MENANG MANUAL (SEMUA MERAH SETELAH CYCLE 1) ---
        if (hasReachedCycle1 && currentGreenCount == 0 && !isGameEnded)
        {
            Debug.Log("PLAYER BERHASIL MEMATIKAN SEMUA DISTRIK SECARA MANUAL!");
            StartCoroutine(WinSequence());
        }

        // --- DETEKSI KESALAHAN ---
        if (hasReachedCycle1 && currentGreenCount > lastGreenCount)
        {
            if (!isGameEnded)
            {
                Debug.Log("Mistake: Menyalakan distrik di fase akhir.");
                onMistakeMade.Invoke();
            }
        }

        lastGreenCount = currentGreenCount;
    }

    private void TriggerCycle1Chaos()
    {
        Debug.Log("CYCLE 1 TERCAPAI!");
        hasReachedCycle1 = true;

        if (currentTime > panicThreshold)
        {
            currentTime = panicThreshold;
        }
        else
        {
            if (!isPanicMode) ActivatePanicMode();
        }

        if (killSwitchButton)
        {
            killSwitchButton.SetActive(true);
            TriggerRedBlink(killSwitchButton);
        }

        onCycle1Reached.Invoke();

        foreach (var plc in chaosTargets)
        {
            if (plc != null) plc.ScrambleChaos();
        }
    }

    private void ActivatePanicMode()
    {
        isPanicMode = true;
        bgmSource.Stop();

        if (sfxSource != null && warningAlarmSFX != null && powerOutageSFX != null)
        {
            sfxSource.PlayOneShot(powerOutageSFX);
            StartCoroutine(WarningAlarmDelay(8f));
        }

        if (warningAlertUI)
        {
            warningAlertUI.SetActive(true);
            TriggerRedBlink(warningAlertUI);
        }
    }

    private IEnumerator WarningAlarmDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        sfxSource.PlayOneShot(warningAlarmSFX);

        StartCoroutine(PanicBGMDelay(1f));
    }

    private IEnumerator PanicBGMDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (bgmSource != null && panicBGM != null)
        {
            bgmSource.clip = panicBGM;
            bgmSource.loop = true;
            bgmSource.Play();
        }
    }

    private void WarningChange()
    {
        warningAlertUI2.SetActive(false);
        warningAlertUI.SetActive(true);
        TriggerRedBlink(warningAlertUI);
    }

    private void TriggerRedBlink(GameObject targetObj)
    {
        var feedback = targetObj.GetComponent<AutoErrorFeedback>();
        if (feedback == null) feedback = targetObj.AddComponent<AutoErrorFeedback>();
        feedback.SetError(true);
    }

    public void ActivateKillSwitch()
    {
        if (isGameEnded) return;
        StartCoroutine(WinSequence());
    }

    // --- LOGIKA MENANG (Dramatic) ---
    private IEnumerator WinSequence()
    {
        WarningChange();
        isGameEnded = true;
        Debug.Log("WIN SEQUENCE INITIATED...");

        // 1. Sembunyikan tombol kill switch agar layar bersih
        if (killSwitchButton) killSwitchButton.SetActive(false);

        // 2. Siapkan Teks Countdown (Warning UI)
        Text warningText = null;
        if (warningAlertUI != null)
        {
            warningAlertUI.SetActive(true);
            // Paksa outline tetap nyala (efek darurat)
            TriggerRedBlink(warningAlertUI);

            warningText = warningAlertUI.GetComponent<Text>();
            if (warningText == null) warningText = warningAlertUI.GetComponentInChildren<Text>();
        }

        // 3. Matikan Status Global & Rusak PLC (Visual effect only)
        if (GlobalPuzzleManager.Instance != null)
        {
            GlobalPuzzleManager.Instance.SetDistrictStatus("A", false);
            GlobalPuzzleManager.Instance.SetDistrictStatus("B", false);
            GlobalPuzzleManager.Instance.SetDistrictStatus("C", false);
            GlobalPuzzleManager.Instance.SetDistrictStatus("D", false);
        }
        foreach (var plc in allPLCs) { if (plc != null) plc.ScrambleChaos(); }

        // 4. FASE 1: COUNTDOWN VISUAL (Layar Masih Terang)
        float countdownDuration = 3.0f;
        float timeLeft = countdownDuration;

        while (timeLeft > 0)
        {
            if (warningText != null)
            {
                // Tampilkan angka 3, 2, 1
                warningText.text = Mathf.CeilToInt(timeLeft).ToString();

                // Efek font membesar saat angka mengecil (Detak Jantung)
                // Base size 80, tambah besar saat waktu habis
                warningText.fontSize = 80 + (int)((3 - timeLeft) * 20);

                // Note: Warna teks TIDAK DIUBAH, tetap sesuai settingan Anda
            }

            timeLeft -= Time.deltaTime;
            yield return null;
        }

        // Tampilkan 0 sebentar
        if (warningText != null) warningText.text = "0";

        // 5. FASE 2: FADE OUT (Layar Gelap & Audio Hilang)
        float fadeDuration = 2.0f;
        float t = 0f;

        // Mulai fade out audio BERSAMAAN dengan visual fade out
        if (bgmSource != null) StartCoroutine(FadeOutAudio(bgmSource, fadeDuration));

        while (t < 1f)
        {
            fadeOutPanel.gameObject.SetActive(true);
            t += Time.deltaTime / fadeDuration;
            if (fadeOutPanel) fadeOutPanel.canvasRenderer.SetAlpha(t);
            yield return null;
        }

        // Pastikan gelap total sebelum pindah
        if (fadeOutPanel) fadeOutPanel.canvasRenderer.SetAlpha(1f);

        yield return new WaitForSeconds(0.5f); // Jeda hening
        SceneManager.LoadScene(winSceneName);
    }

    private IEnumerator GameOverSequence()
    {
        WarningChange();
        fadeOutPanel.gameObject.SetActive(true);
        isGameEnded = true;
        Debug.Log("GAME OVER");

        if (fadeOutPanel) fadeOutPanel.canvasRenderer.SetAlpha(1f);

        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene(gameOverSceneName);
    }

    private IEnumerator FadeOutAudio(AudioSource source, float duration)
    {
        float startVol = source.volume;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            source.volume = Mathf.Lerp(startVol, 0f, t);
            yield return null;
        }
        source.Stop();
    }
}