using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PLCSlotHandler : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    public PLCController controller;
    public int slotIndex;

    public void OnDrop(PointerEventData eventData)
    {
        GameObject droppedObj = eventData.pointerDrag;
        if (droppedObj != null)
        {
            DraggableInventoryItem itemDrag = droppedObj.GetComponent<DraggableInventoryItem>();
            if (itemDrag != null)
            {
                // Panggil Controller dan simpan status sukses/gagal
                bool success = controller.OnItemDropped(slotIndex, itemDrag.myData);

                // Jika sukses masuk slot, tandai item agar dia menghancurkan diri sendiri
                if (success)
                {
                    itemDrag.wasConsumed = true;
                }
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (controller != null)
            controller.OnSlotClicked(slotIndex);
    }
}