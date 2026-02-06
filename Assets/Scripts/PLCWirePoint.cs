using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PLCWirePoint : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [Header("Identity")]
    public string pointID; // ID Unik (misal: "Point_A1")

    [Header("Runtime State")]
    public PLCWirePoint connectedTo; // Titik mana yang nyambung kesini?
    public GameObject wireLineObject; // Garis visualnya

    private PLCController masterController;
    private AutoErrorFeedback feedback;

    public void Setup(PLCController master)
    {
        masterController = master;
        feedback = gameObject.AddComponent<AutoErrorFeedback>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Hanya boleh tarik kabel jika belum tersambung
        if (connectedTo == null)
        {
            masterController.OnWireDragStart(this);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (connectedTo == null)
        {
            masterController.OnWireDrag(eventData.position);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (connectedTo == null)
        {
            masterController.OnWireDragEnd(eventData);
        }
    }

    // Klik untuk putus kabel
    public void OnPointerClick(PointerEventData eventData)
    {
        if (connectedTo != null)
        {
            // Putuskan hubungan
            masterController.DisconnectWire(this, connectedTo);
        }
    }

    public void SetErrorState(bool isError)
    {
        if (feedback != null) feedback.SetError(isError);
    }
}