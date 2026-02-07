using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SimpleDialogueManager : MonoBehaviour
{
    public static SimpleDialogueManager Instance;

    [System.Serializable]
    public class DialogueData
    {
        public string id; // Nama panggil (misal: "Cycle1", "Mistake")
        [TextArea(3, 10)]
        public List<string> lines; // Isi percakapan (bisa banyak halaman)
    }

    [Header("UI References")]
    [Tooltip("Panel background setengah layar")]
    public GameObject dialoguePanel;
    [Tooltip("Text element untuk menampilkan dialog")]
    public Text dialogueText;
    [Tooltip("Tombol/Area besar transparan untuk mendeteksi klik")]
    public Button interactionButton;

    [Header("Settings")]
    public float typingSpeed = 0.04f;

    [Header("Library")]
    [Tooltip("Daftar semua percakapan di game ini")]
    public List<DialogueData> dialogueLibrary;

    // Internal State
    private Queue<string> _linesQueue = new Queue<string>();
    private string _currentFullLine;
    private bool _isTyping = false;
    private Coroutine _typingRoutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;

        // Setup awal
        if (dialoguePanel) dialoguePanel.SetActive(false);
        if (interactionButton) interactionButton.onClick.AddListener(OnInteract);
    }

    /// <summary>
    /// Fungsi utama untuk memanggil dialog berdasarkan ID.
    /// Pasang ini di Unity Event GameCycleManager.
    /// </summary>
    public void ShowDialogue(string dialogueID)
    {
        // 1. Cari data dialog di library
        DialogueData data = dialogueLibrary.Find(d => d.id == dialogueID);

        if (data != null)
        {
            StartDialogueSequence(data.lines);
        }
        else
        {
            Debug.LogWarning($"[Dialogue] ID '{dialogueID}' tidak ditemukan di Library!");
        }
    }

    private void StartDialogueSequence(List<string> lines)
    {
        _linesQueue.Clear();
        foreach (var line in lines)
        {
            _linesQueue.Enqueue(line);
        }

        dialoguePanel.SetActive(true);
        ShowNextLine();
    }

    private void ShowNextLine()
    {
        if (_linesQueue.Count > 0)
        {
            _currentFullLine = _linesQueue.Dequeue();
            if (_typingRoutine != null) StopCoroutine(_typingRoutine);
            _typingRoutine = StartCoroutine(TypeLine(_currentFullLine));
        }
        else
        {
            CloseDialogue();
        }
    }

    private void OnInteract()
    {
        // Logika Interaksi: Percepat atau Lanjut
        if (_isTyping)
        {
            // Jika sedang ngetik -> Langsung selesaikan (Skip)
            if (_typingRoutine != null) StopCoroutine(_typingRoutine);
            dialogueText.text = _currentFullLine;
            _isTyping = false;
        }
        else
        {
            // Jika sudah selesai ngetik -> Lanjut baris berikutnya / Tutup
            ShowNextLine();
        }
    }

    private IEnumerator TypeLine(string line)
    {
        _isTyping = true;
        dialogueText.text = "";

        foreach (char letter in line.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        _isTyping = false;
    }

    private void CloseDialogue()
    {
        dialoguePanel.SetActive(false);
    }
}