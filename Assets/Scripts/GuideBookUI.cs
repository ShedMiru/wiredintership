using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GuideBookUI : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    [Header("Targets (existing objects)")]
    [SerializeField] private RectTransform garduTarget;     // Canvas/Gardu (posisi OPEN)
    [SerializeField] private CanvasGroup canvasGroup;       // sudah ada di GuideBook

    [Header("Book Root")]
    [SerializeField] private RectTransform bookRect;
    [SerializeField] private RectTransform dragBounds;      // biasanya Canvas (auto)

    [Header("Views (existing objects)")]
    [SerializeField] private GameObject closedBook;         // GuideBook/ClosedBook (cover)
    [SerializeField] private GameObject openBook;           // GuideBook/OpenBook (panel halaman)
    [SerializeField] private Image leftPage;                // OpenBook/Left
    [SerializeField] private Image rightPage;               // OpenBook/Right
    [SerializeField] private Image closedImage;             // (optional) Image cover kalau mau ganti sprite

    [Header("Buttons (2 tombol berbeda)")]
    [SerializeField] private Button btnOpen;                // tombol di COVER (ClosedBook)
    [SerializeField] private Button btnClose;               // tombol CLOSE (muncul saat open)
    [SerializeField] private Button btnPrev;                // optional
    [SerializeField] private Button btnNext;                // optional

    [Header("Pages")]
    [Tooltip("Open spread: kiri=0 kanan=1, lalu next kiri=2 kanan=3, dst.")]
    [SerializeField] private List<Sprite> pages = new List<Sprite>();

    [Header("Motion")]
    [SerializeField] private float moveDuration = 0.18f;

    [Header("Flip Effect")]
    [SerializeField] private RectTransform flipTarget;      // default = rightPage rect
    [SerializeField] private float flipHalfDuration = 0.12f;

    private Vector2 closedAnchoredPos; // posisi awal GuideBook di scene (CLOSED)
    private bool isOpen;
    private bool isFlipping;
    private int spreadIndex;

    // drag
    private Vector2 dragStartBookPos;
    private Vector2 dragStartPointerLocal;

    private void Reset()
    {
        bookRect = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void OnValidate()
    {
        if (bookRect == null) bookRect = GetComponent<RectTransform>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

        // auto-find sesuai hierarchy kamu
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

        if (flipTarget == null && rightPage != null)
            flipTarget = rightPage.rectTransform;
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

        // CLOSED pos = posisi awal yang kamu set di scene (tanpa angka manual)
        closedAnchoredPos = bookRect.anchoredPosition;

        // Wiring tombol
        if (btnOpen) btnOpen.onClick.AddListener(Open);
        if (btnClose) btnClose.onClick.AddListener(Close);
        if (btnPrev) btnPrev.onClick.AddListener(Prev);
        if (btnNext) btnNext.onClick.AddListener(Next);

        ApplyStateInstant(isOpen);
    }

    // -------------------- OPEN / CLOSE (2 tombol berbeda)
    public void Open()
    {
        if (isOpen || isFlipping) return;

        isOpen = true;
        ApplyView(true);
        UpdateButtonsVisibility();

        StopAllCoroutines();
        StartCoroutine(MoveToAnchored(GetOpenAnchoredPos()));
    }

    public void Close()
    {
        if (!isOpen || isFlipping) return;

        isOpen = false;
        ApplyView(false);
        UpdateButtonsVisibility();

        StopAllCoroutines();
        StartCoroutine(MoveToAnchored(closedAnchoredPos));
    }

    private void ApplyStateInstant(bool open)
    {
        isOpen = open;
        ApplyView(open);
        UpdateButtonsVisibility();

        bookRect.anchoredPosition = open ? GetOpenAnchoredPos() : closedAnchoredPos;
    }

    private void ApplyView(bool open)
    {
        if (closedBook) closedBook.SetActive(!open);
        if (openBook) openBook.SetActive(open);

        // cover sprite optional
        if (!open && closedImage != null && pages != null && pages.Count > 0)
            closedImage.sprite = pages[0];

        // saat buka, pastikan spread pertama tampil
        if (open)
        {
            spreadIndex = Mathf.Max(0, spreadIndex);
            RefreshSpreadSprites();
        }
    }

    private void UpdateButtonsVisibility()
    {
        // btnOpen biasanya menempel di ClosedBook (jadi otomatis hilang saat ClosedBook inactive)
        // tapi kalau btnOpen terpisah, ini tetap aman:
        if (btnOpen) btnOpen.gameObject.SetActive(!isOpen);

        // close hanya muncul saat open
        if (btnClose) btnClose.gameObject.SetActive(isOpen);

        // nav button hanya saat open
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

    // -------------------- PAGES + FLIP
    public void Next()
    {
        if (!isOpen || isFlipping) return;
        int nextIndex = spreadIndex + 2;
        if (nextIndex >= pages.Count) return;
        StartCoroutine(FlipTo(nextIndex));
    }

    public void Prev()
    {
        if (!isOpen || isFlipping) return;
        int prevIndex = spreadIndex - 2;
        if (prevIndex < 0) return;
        StartCoroutine(FlipTo(prevIndex));
    }

    private IEnumerator FlipTo(int newSpreadIndex)
    {
        isFlipping = true;
        UpdateButtonsVisibility();

        if (flipTarget != null)
            yield return ScaleX(flipTarget, 1f, 0f, flipHalfDuration);

        spreadIndex = newSpreadIndex;
        RefreshSpreadSprites();

        if (flipTarget != null)
            yield return ScaleX(flipTarget, 0f, 1f, flipHalfDuration);

        isFlipping = false;
        UpdateButtonsVisibility();
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

    private IEnumerator ScaleX(RectTransform rt, float from, float to, float duration)
    {
        Vector3 s = rt.localScale;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, duration);
            float v = Mathf.Lerp(from, to, Smooth01(t));
            rt.localScale = new Vector3(v, s.y, s.z);
            yield return null;
        }

        rt.localScale = new Vector3(to, s.y, s.z);
    }
}
