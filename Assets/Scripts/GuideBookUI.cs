using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GuideBookUI : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    [Header("Targets (existing objects)")]
    [SerializeField] private RectTransform garduTarget;     // Canvas/Gardu (posisi OPEN)
    [SerializeField] private CanvasGroup canvasGroup;       // ada di GuideBook

    [Header("Book Root")]
    [SerializeField] private RectTransform bookRect;
    [SerializeField] private RectTransform dragBounds;      // biasanya Canvas (auto)

    [Header("Views (existing objects)")]
    [SerializeField] private GameObject closedBook;         // GuideBook/ClosedBook (cover)
    [SerializeField] private GameObject openBook;           // GuideBook/OpenBook (panel halaman)
    [SerializeField] private Image leftPage;                // OpenBook/Left
    [SerializeField] private Image rightPage;               // OpenBook/Right
    [SerializeField] private Image closedImage;             // optional (Image cover)

    [Header("Buttons (2 tombol berbeda)")]
    [SerializeField] private Button btnOpen;                // tombol di COVER (ClosedBook)
    [SerializeField] private Button btnClose;               // tombol CLOSE (muncul saat open)
    [SerializeField] private Button btnPrev;                // optional
    [SerializeField] private Button btnNext;                // optional

    [Header("Pages")]
    [Tooltip("Spread mode: kiri=0 kanan=1, lalu next kiri=2 kanan=3, dst.")]
    [SerializeField] private List<Sprite> pages = new List<Sprite>();

    [Header("Motion")]
    [SerializeField] private float moveDuration = 0.18f;

    [Header("Open Animation (Special)")]
    [SerializeField] private float openDuration = 0.6f;     // Durasi khusus membuka buku
    [SerializeField] private AnimationCurve openCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Flip Motion (lebih masuk akal: geser halaman)")]
    [SerializeField] private float flipDuration = 0.22f;     // durasi geser
    [SerializeField] private float flipScaleMin = 0.92f;     // sedikit “press” biar terasa kertas
    [SerializeField] private float flipAlphaMin = 0.75f;     // sedikit transparan saat bergerak

    private Vector2 closedAnchoredPos; // posisi awal GuideBook di scene (CLOSED)
    private bool isOpen;
    private bool isFlipping;
    private int spreadIndex;

    // drag
    private Vector2 dragStartBookPos;
    private Vector2 dragStartPointerLocal;

    // home positions halaman (di-cache setelah layout settle)
    private Vector2 leftHomePos;
    private Vector2 rightHomePos;

    // kalau OpenBook pakai LayoutGroup / ContentSizeFitter
    private LayoutGroup openLayoutGroup;
    private ContentSizeFitter openFitter;

    private void Reset()
    {
        bookRect = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void OnValidate()
    {
        if (bookRect == null) bookRect = GetComponent<RectTransform>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

        if (closedBook == null)
        {
            var t = transform.Find("ClosedBook");
            if (t) closedBook = t.gameObject;
        }

        if (openBook == null)
        {
            var t = transform.Find("OpenBook");
            if (t) openBook = t.gameObject;
        }

        if (openBook != null)
        {
            if (leftPage == null)
            {
                var t = openBook.transform.Find("Left");
                if (t) leftPage = t.GetComponent<Image>();
            }

            if (rightPage == null)
            {
                var t = openBook.transform.Find("Right");
                if (t) rightPage = t.GetComponent<Image>();
            }
        }
    }

    private void Awake()
    {
        if (bookRect == null) bookRect = GetComponent<RectTransform>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

        if (dragBounds == null)
        {
            var c = GetComponentInParent<Canvas>();
            dragBounds = c ? c.GetComponent<RectTransform>() : (bookRect.parent as RectTransform);
        }

        if (garduTarget == null)
        {
            var parent = transform.parent; // Canvas
            if (parent)
            {
                var g = parent.Find("Gardu");
                if (g) garduTarget = g.GetComponent<RectTransform>();
            }
        }

        if (openBook != null)
        {
            openLayoutGroup = openBook.GetComponent<LayoutGroup>();
            openFitter = openBook.GetComponent<ContentSizeFitter>();
        }

        if (btnOpen) btnOpen.onClick.AddListener(Open);
        if (btnClose) btnClose.onClick.AddListener(Close);
        if (btnPrev) btnPrev.onClick.AddListener(Prev);
        if (btnNext) btnNext.onClick.AddListener(Next);

        // init view dulu
        ApplyView(false);
        UpdateButtonsVisibility();

        // cache posisi setelah UI layout settle
        StartCoroutine(CacheHomePositionsAfterLayout());
    }

    private IEnumerator CacheHomePositionsAfterLayout()
    {
        // tunggu 1 frame biar layout group nerapin posisi Left/Right dulu
        yield return null;

        closedAnchoredPos = bookRect.anchoredPosition;

        if (leftPage) leftHomePos = leftPage.rectTransform.anchoredPosition;
        if (rightPage) rightHomePos = rightPage.rectTransform.anchoredPosition;
    }

    // -------------------- OPEN / CLOSE (2 tombol berbeda)
    public void Open()
    {
        if (isOpen || isFlipping) return;

        isOpen = true;
        // MODIFIKASI: Jangan panggil ApplyView(true) langsung.
        // Biarkan coroutine animasi yang menangani perubahan visual agar mulus.

        UpdateButtonsVisibility();

        StopAllCoroutines();
        // GANTI: Panggil animasi spesial pembuka buku
        StartCoroutine(AnimateOpeningSequence());
    }

    // MODIFIKASI: Animasi Unik Membuka Buku (Flip + Scale)
    private IEnumerator AnimateOpeningSequence()
    {
        isFlipping = true; // Kunci interaksi

        // Pastikan mulai dari posisi Closed (Cover)
        if (closedBook) closedBook.SetActive(true);
        if (openBook) openBook.SetActive(false);

        // Setup cover image (opsional)
        if (closedImage && pages.Count > 0) closedImage.sprite = pages[0];

        Vector2 start = closedAnchoredPos;
        Vector2 target = GetOpenAnchoredPos();

        float t = 0f;
        bool viewSwapped = false;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, openDuration);
            float progress = Mathf.Clamp01(t);
            float curveValue = openCurve.Evaluate(progress);

            // 1. Gerakan Posisi (Menuju Gardu)
            bookRect.anchoredPosition = Vector2.LerpUnclamped(start, target, curveValue);

            // 2. Efek Flip (Scale X) - Unik dan Menggeleggar
            // Cos(0) = 1 (Lebar) -> Cos(PI/2) = 0 (Tipis) -> Cos(PI) = -1 (Lebar lagi)
            float scaleX = Mathf.Abs(Mathf.Cos(progress * Mathf.PI));
            bookRect.localScale = new Vector3(scaleX, 1f, 1f);

            // 3. Swap View di tengah (saat buku "tipis")
            if (progress >= 0.5f && !viewSwapped)
            {
                ApplyView(true); // Ganti ke Halaman Terbuka
                viewSwapped = true;
            }

            yield return null;
        }

        // Finalisasi
        bookRect.anchoredPosition = target;
        bookRect.localScale = Vector3.one;
        if (!viewSwapped) ApplyView(true);

        isFlipping = false; // Buka kunci
        UpdateButtonsVisibility();
    }

    public void Close()
    {
        if (!isOpen || isFlipping) return;

        isOpen = false;
        // KODINGAN AWAL (Safety): Langsung ganti ke Cover (ClosedBook) sebelum bergerak.
        // Ini memastikan sprite buku "berjalan" pulang, bukan menghilang.
        ApplyView(false);

        UpdateButtonsVisibility();

        StopAllCoroutines();
        // Kembali menggunakan MoveToAnchored (Animasi simple & aman)
        StartCoroutine(MoveToAnchored(closedAnchoredPos));
    }

    private void ApplyView(bool open)
    {
        if (closedBook) closedBook.SetActive(!open);
        if (openBook) openBook.SetActive(open);

        if (!open && closedImage != null && pages != null && pages.Count > 0)
            closedImage.sprite = pages[0];

        if (open)
        {
            // pastikan sprite spread tampil benar
            spreadIndex = Mathf.Clamp(spreadIndex, 0, Mathf.Max(0, pages.Count - 1));
            RefreshSpreadSprites();
        }
    }

    private void UpdateButtonsVisibility()
    {
        if (btnOpen) btnOpen.gameObject.SetActive(!isOpen);
        if (btnClose) btnClose.gameObject.SetActive(isOpen);

        bool navVisible = isOpen && !isFlipping;
        if (btnPrev) btnPrev.gameObject.SetActive(navVisible);
        if (btnNext) btnNext.gameObject.SetActive(navVisible);

        UpdateNavInteractable();
    }

    private void UpdateNavInteractable()
    {
        if (btnPrev) btnPrev.interactable = isOpen && !isFlipping && (spreadIndex - 2 >= 0);
        if (btnNext) btnNext.interactable = isOpen && !isFlipping && (spreadIndex + 2 < pages.Count);
    }

    // -------------------- MOVE (no manual position)
    private Vector2 GetOpenAnchoredPos()
    {
        if (garduTarget != null && garduTarget.parent == bookRect.parent)
            return garduTarget.anchoredPosition;

        if (garduTarget != null)
        {
            Vector3 world = garduTarget.position;
            Vector3 local = bookRect.parent.InverseTransformPoint(world);
            return (Vector2)local;
        }

        return closedAnchoredPos;
    }

    private IEnumerator MoveToAnchored(Vector2 target)
    {
        // Pastikan scale normal saat move biasa (Close)
        bookRect.localScale = Vector3.one;

        Vector2 start = bookRect.anchoredPosition;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, moveDuration);
            float s = Smooth01(t);
            bookRect.anchoredPosition = Vector2.LerpUnclamped(start, target, s);
            yield return null;
        }

        bookRect.anchoredPosition = target;
    }

    // -------------------- DRAG
    public void OnBeginDrag(PointerEventData eventData)
    {
        dragStartBookPos = bookRect.anchoredPosition;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            dragBounds,
            eventData.position,
            eventData.pressEventCamera,
            out dragStartPointerLocal
        );
    }

    public void OnDrag(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            dragBounds,
            eventData.position,
            eventData.pressEventCamera,
            out var pointerLocal
        );

        Vector2 delta = pointerLocal - dragStartPointerLocal;
        Vector2 target = dragStartBookPos + delta;

        bookRect.anchoredPosition = ClampToBounds(target);
    }

    private Vector2 ClampToBounds(Vector2 anchoredPos)
    {
        if (dragBounds == null) return anchoredPos;

        Vector2 halfBounds = dragBounds.rect.size * 0.5f;
        Vector2 halfBook = bookRect.rect.size * 0.5f;

        float minX = -halfBounds.x + halfBook.x;
        float maxX = halfBounds.x - halfBook.x;
        float minY = -halfBounds.y + halfBook.y;
        float maxY = halfBounds.y - halfBook.y;

        anchoredPos.x = Mathf.Clamp(anchoredPos.x, minX, maxX);
        anchoredPos.y = Mathf.Clamp(anchoredPos.y, minY, maxY);

        return anchoredPos;
    }

    // -------------------- PAGES + FLIP (GESER KERTAS, BUKAN MUTER)
    public void Next()
    {
        if (!isOpen || isFlipping) return;
        int nextIndex = spreadIndex + 2;
        if (nextIndex >= pages.Count) return;

        StartCoroutine(FlipSlide(nextIndex, forward: true));
    }

    public void Prev()
    {
        if (!isOpen || isFlipping) return;
        int prevIndex = spreadIndex - 2;
        if (prevIndex < 0) return;

        StartCoroutine(FlipSlide(prevIndex, forward: false));
    }

    private IEnumerator FlipSlide(int newSpreadIndex, bool forward)
    {
        isFlipping = true;
        UpdateButtonsVisibility();

        // Disable layout agar posisi halaman bisa dianimasi (tanpa bikin object baru)
        bool hadLayout = false;
        if (openLayoutGroup != null && openLayoutGroup.enabled) { openLayoutGroup.enabled = false; hadLayout = true; }
        bool hadFitter = false;
        if (openFitter != null && openFitter.enabled) { openFitter.enabled = false; hadFitter = true; }

        // pastikan home pos sudah ada
        if (leftPage) leftHomePos = leftPage.rectTransform.anchoredPosition;
        if (rightPage) rightHomePos = rightPage.rectTransform.anchoredPosition;

        // yang bergerak:
        Image movingImg = forward ? rightPage : leftPage;
        RectTransform movingRt = movingImg.rectTransform;

        Vector2 from = forward ? rightHomePos : leftHomePos;
        Vector2 to = forward ? leftHomePos : rightHomePos;

        // biar layer-nya di atas saat bergerak
        movingRt.SetAsLastSibling();

        yield return SlidePage(movingImg, from, to, flipDuration);

        // setelah "kertas" sampai sisi seberang -> baru ganti sprite
        spreadIndex = newSpreadIndex;
        RefreshSpreadSprites();

        // reset posisi & visual halaman yang digeser
        movingRt.anchoredPosition = from;
        movingRt.localScale = Vector3.one;
        SetAlpha(movingImg, 1f);

        // enable layout lagi (kalau ada) supaya balik rapi
        if (hadLayout) openLayoutGroup.enabled = true;
        if (hadFitter) openFitter.enabled = true;

        isFlipping = false;
        UpdateButtonsVisibility();
    }

    private IEnumerator SlidePage(Image img, Vector2 from, Vector2 to, float duration)
    {
        RectTransform rt = img.rectTransform;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, duration);
            float s = Smooth01(t);

            // gerak dari kanan->kiri atau kiri->kanan
            rt.anchoredPosition = Vector2.LerpUnclamped(from, to, s);

            // kasih sedikit feel "kertas" (press kecil + alpha)
            float press = Mathf.Lerp(1f, flipScaleMin, Mathf.Sin(s * Mathf.PI)); // kecil di tengah
            rt.localScale = new Vector3(press, 1f, 1f);

            float a = Mathf.Lerp(1f, flipAlphaMin, Mathf.Sin(s * Mathf.PI));
            SetAlpha(img, a);

            yield return null;
        }

        rt.anchoredPosition = to;
    }

    private void RefreshSpreadSprites()
    {
        if (leftPage == null || rightPage == null) return;

        if (pages == null || pages.Count == 0)
        {
            leftPage.sprite = null;
            rightPage.sprite = null;
            return;
        }

        int left = Mathf.Clamp(spreadIndex, 0, pages.Count - 1);
        int right = left + 1;

        leftPage.sprite = pages[left];
        rightPage.sprite = (right < pages.Count) ? pages[right] : null;

        UpdateNavInteractable();
    }

    // -------------------- UTIL
    private static float Smooth01(float x)
    {
        x = Mathf.Clamp01(x);
        return x * x * (3f - 2f * x);
    }

    private static void SetAlpha(Image img, float a)
    {
        if (img == null) return;
        Color c = img.color;
        c.a = a;
        img.color = c;
    }
}