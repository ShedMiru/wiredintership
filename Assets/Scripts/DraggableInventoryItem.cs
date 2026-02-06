using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class DraggableInventoryItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [HideInInspector] public ItemData myData;
    [HideInInspector] public bool wasConsumed = false; // Flag: Apakah item ini berhasil dipasang?

    private Transform originalParent;
    private CanvasGroup canvasGroup;
    private Transform rootCanvasTransform;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        wasConsumed = false; // Reset status
        originalParent = transform.parent;

        // Pindahkan ke root canvas agar melayang di atas segalanya
        Canvas rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null)
        {
            rootCanvasTransform = rootCanvas.rootCanvas.transform;
            transform.SetParent(rootCanvasTransform);
        }

        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // LOGIKA PERBAIKAN:
        // Jika item ini sudah ditandai "terpakai" oleh Slot Handler, maka musnahkan.
        // Jangan kembali ke inventory panel karena inventory panel sudah di-refresh oleh Controller.
        if (wasConsumed)
        {
            Destroy(gameObject);
            return;
        }

        // Jika gagal drop (drop di tempat kosong/salah), kembali ke rumah
        transform.SetParent(originalParent);
        transform.localPosition = Vector3.zero;
    }
}