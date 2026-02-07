using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PLCController : MonoBehaviour
{
    // --- CLASS DEFINITIONS ---
    [System.Serializable]
    public class PLCSlot
    {
        public string slotName;
        public PLCSlotHandler slotHandlerUI;
        public Image itemDisplayImage;
        public ItemData currentItem;
        public ItemData requiredItem;
        [HideInInspector] public bool isBroken;
        [HideInInspector] public AutoErrorFeedback feedback;
    }

    [System.Serializable]
    public class PLCSwitch
    {
        public string switchName;
        public Toggle toggleUI;
        public bool requiredState;
        [HideInInspector] public AutoErrorFeedback feedback;
    }

    [System.Serializable]
    public class PLCLiquidRule
    {
        public PLCLiquidTank tankScript;
        [Range(0, 1f)] public float targetLevel = 0.5f;
        public float tolerance = 0.01f;
    }

    [System.Serializable]
    public class PLCWireRule
    {
        public PLCWirePoint pointA;
        public PLCWirePoint pointB;

        [Tooltip("Index elemen di list 'Wire Prefabs' yang akan dipakai untuk kabel ini")]
        public int wireColorIndex = 0;

        [Header("Logic Settings")]
        [Tooltip("Centang jika kabel HARUS tersambung agar menang. Uncheck jika kabel HARUS PUTUS/TIDAK ADA.")]
        public bool shouldBeConnected = true;

        [Header("Initial State")]
        [Tooltip("Centang jika kabel sudah terpasang otomatis saat game mulai (misal untuk jebakan kabel salah).")]
        public bool startConnected = false;
    }

    // --- MAIN SETTINGS ---
    [Header("Identitas")]
    public string plcID = "PLC_A";
    public string targetDistrict = "A";

    [Header("Chaos System")]
    public bool isChaosTrigger = false;
    public List<PLCController> chaosTargets;
    private bool hasTriggeredChaos = false;

    [Header("Komponen Puzzle")]
    public List<PLCSlot> slots;
    public List<PLCSwitch> switches;
    public List<PLCLiquidRule> liquidRules;
    public List<PLCWireRule> wireRules;
    public List<PLCWirePoint> allWirePoints;

    [Header("Visual Wiring")]
    public List<GameObject> wirePrefabs;
    public Transform wireCanvasParent;

    [Header("Logika Bahaya")]
    public ItemData explosiveItem;
    public int explosiveSlotIndex;

    // --- INTERNAL ---
    private PLCWirePoint currentDraggingPoint;
    private GameObject tempWireLine;
    private bool isSystemReady = false;
    private float lastExplosionTime = 0f;
    private Canvas rootCanvas; // Cache canvas untuk performa

    private void Start()
    {
        if (wirePrefabs == null) wirePrefabs = new List<GameObject>();
        rootCanvas = GetComponentInParent<Canvas>(); // Cache di awal
        StartCoroutine(InitializeSystem());
    }

    // Menggunakan LateUpdate untuk memastikan visual kabel selalu nempel di titiknya
    private void LateUpdate()
    {
        if (!isSystemReady) return;

        HashSet<GameObject> updatedWires = new HashSet<GameObject>();

        foreach (var pt in allWirePoints)
        {
            if (pt != null && pt.connectedTo != null && pt.wireLineObject != null)
            {
                if (!updatedWires.Contains(pt.wireLineObject))
                {
                    UpdateLineVisualWorld(pt.wireLineObject.transform as RectTransform, pt.transform.position, pt.connectedTo.transform.position);
                    updatedWires.Add(pt.wireLineObject);
                }
            }
        }
    }

    private IEnumerator InitializeSystem()
    {
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();

        // 1. Setup Switches
        foreach (var sw in switches)
        {
            if (sw.toggleUI != null)
            {
                GameObject targetObj = sw.toggleUI.targetGraphic ? sw.toggleUI.targetGraphic.gameObject : sw.toggleUI.gameObject;
                sw.feedback = GetOrAddFeedback(targetObj);
                sw.toggleUI.onValueChanged.AddListener(delegate { CheckLogic(); });
            }
        }

        // 2. Setup Slots
        foreach (var slot in slots)
        {
            if (slot.slotHandlerUI != null)
            {
                GameObject targetObj = slot.slotHandlerUI.gameObject;
                if (targetObj.GetComponent<Graphic>() == null)
                {
                    var gfx = targetObj.GetComponentInChildren<Graphic>();
                    if (gfx != null) targetObj = gfx.gameObject;
                }

                slot.feedback = GetOrAddFeedback(targetObj);
                slot.slotHandlerUI.slotIndex = slots.IndexOf(slot);
                slot.slotHandlerUI.controller = this;
            }
        }

        // 3. Setup Tanks
        foreach (var rule in liquidRules)
        {
            if (rule.tankScript != null) rule.tankScript.Setup(this);
        }

        // 4. Setup Wire Points
        foreach (var point in allWirePoints)
        {
            if (point != null) point.Setup(this);
        }

        // 5. Setup Initial Wires (Auto Connect)
        foreach (var rule in wireRules)
        {
            if (rule.startConnected && rule.pointA != null && rule.pointB != null)
            {
                if (rule.pointA.connectedTo == null && rule.pointB.connectedTo == null)
                {
                    GameObject selectedPrefab = null;
                    if (wirePrefabs.Count > 0)
                    {
                        int idx = (rule.wireColorIndex >= 0 && rule.wireColorIndex < wirePrefabs.Count) ? rule.wireColorIndex : 0;
                        selectedPrefab = wirePrefabs[idx];
                    }

                    if (selectedPrefab != null)
                    {
                        if (wireCanvasParent == null) wireCanvasParent = transform;

                        GameObject newWire = Instantiate(selectedPrefab, wireCanvasParent);

                        RectTransform rt = newWire.GetComponent<RectTransform>();
                        if (rt != null)
                        {
                            rt.pivot = new Vector2(0, 0.5f);
                            rt.localScale = Vector3.one;
                            // rt.anchoredPosition3D = Vector3.zero; // DELETE THIS: Jangan reset ke 0,0

                            // Pastikan Z lokal 0
                            Vector3 localPos = rt.localPosition;
                            localPos.z = 0;
                            rt.localPosition = localPos;
                        }

                        if (newWire.GetComponent<Image>()) newWire.GetComponent<Image>().raycastTarget = false;

                        ConnectWire(rule.pointA, rule.pointB, newWire);
                    }
                }
            }
        }

        RefreshSlotUI();
        isSystemReady = true;
        CheckLogic();
    }

    private AutoErrorFeedback GetOrAddFeedback(GameObject obj)
    {
        var fb = obj.GetComponent<AutoErrorFeedback>();
        if (fb == null) fb = obj.AddComponent<AutoErrorFeedback>();
        return fb;
    }

    // ================= ITEM LOGIC =================
    public bool OnItemDropped(int slotIndex, ItemData droppedItem)
    {
        if (slotIndex < 0 || slotIndex >= slots.Count) return false;
        PLCSlot slot = slots[slotIndex];
        if (slot.currentItem != null) return false;

        slot.currentItem = droppedItem;
        slot.isBroken = false;
        PlayerInventory.Instance.RemoveItem(droppedItem);

        if (PLCInventoryDisplay.Instance != null) PLCInventoryDisplay.Instance.RefreshInventoryUI();

        RefreshSlotUI();
        CheckLogic();
        return true;
    }

    public void OnSlotClicked(int slotIndex)
    {
        PLCSlot slot = slots[slotIndex];
        if (slot.currentItem != null)
        {
            if (slot.isBroken)
            {
                slot.currentItem = null;
                slot.isBroken = false;
            }
            else
            {
                if (PlayerInventory.Instance.AddItem(slot.currentItem))
                {
                    slot.currentItem = null;
                }
                else return;
            }

            RefreshSlotUI();
            if (PLCInventoryDisplay.Instance != null) PLCInventoryDisplay.Instance.RefreshInventoryUI();
            CheckLogic();
        }
    }

    private void RefreshSlotUI()
    {
        foreach (var slot in slots)
        {
            if (slot.itemDisplayImage != null)
            {
                if (slot.currentItem != null)
                {
                    slot.itemDisplayImage.sprite = slot.currentItem.icon;
                    slot.itemDisplayImage.enabled = true;
                    slot.itemDisplayImage.color = slot.isBroken ? Color.gray : Color.white;
                }
                else
                {
                    slot.itemDisplayImage.sprite = null;
                    slot.itemDisplayImage.enabled = false;
                }
            }
        }
    }

    // ================= WIRING LOGIC =================
    public void OnWireDragStart(PLCWirePoint point)
    {
        currentDraggingPoint = point;

        if (wirePrefabs == null || wirePrefabs.Count == 0) return;

        int selectedIndex = 0;
        foreach (var rule in wireRules)
        {
            if (rule.pointA == point || rule.pointB == point)
            {
                selectedIndex = rule.wireColorIndex;
                break;
            }
        }

        if (selectedIndex < 0 || selectedIndex >= wirePrefabs.Count) selectedIndex = 0;
        GameObject selectedPrefab = wirePrefabs[selectedIndex];

        if (wireCanvasParent == null) wireCanvasParent = transform;
        tempWireLine = Instantiate(selectedPrefab, wireCanvasParent);
        tempWireLine.transform.position = point.transform.position;

        if (tempWireLine.GetComponent<Image>())
            tempWireLine.GetComponent<Image>().raycastTarget = false;

        RectTransform rt = tempWireLine.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.pivot = new Vector2(0, 0.5f);
            rt.localScale = Vector3.one;

            // FIX: Jangan reset anchoredPosition3D ke Zero, karena itu memindahkannya ke tengah canvas
            // Cukup pastikan Z lokal = 0 agar nempel di canvas
            Vector3 localPos = rt.localPosition;
            localPos.z = 0;
            rt.localPosition = localPos;
        }
    }

    // FIX UTAMA: Gunakan ScreenPointToWorldPointInRectangle
    // Ini menjamin posisi mouse diproyeksikan ke Plane Canvas yang benar, anti "Menembus Langit"
    public void OnWireDrag(Vector2 screenMousePos)
    {
        if (tempWireLine != null && currentDraggingPoint != null && wireCanvasParent != null)
        {
            RectTransform parentRect = wireCanvasParent as RectTransform;
            Vector3 worldMousePos;

            // Proyeksikan Mouse (2D) ke World Position (3D) tepat di atas rect
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                parentRect,
                screenMousePos,
                GetCanvasCamera(),
                out worldMousePos))
            {
                // Update visual menggunakan World Position yang sudah valid
                UpdateLineVisualWorld(tempWireLine.transform as RectTransform, currentDraggingPoint.transform.position, worldMousePos);
            }
        }
    }

    private Camera GetCanvasCamera()
    {
        if (rootCanvas == null) rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas == null) return null;
        if (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay) return null;
        return rootCanvas.worldCamera != null ? rootCanvas.worldCamera : Camera.main;
    }

    public void OnWireDragEnd(UnityEngine.EventSystems.PointerEventData eventData)
    {
        GameObject hitObj = eventData.pointerEnter;
        if (hitObj != null)
        {
            PLCWirePoint targetPoint = hitObj.GetComponent<PLCWirePoint>();
            if (targetPoint != null && targetPoint != currentDraggingPoint && targetPoint.connectedTo == null)
            {
                if (tempWireLine != null)
                {
                    ConnectWire(currentDraggingPoint, targetPoint, tempWireLine);
                    tempWireLine = null;
                    CheckLogic();
                    currentDraggingPoint = null;
                    return;
                }
            }
        }

        if (tempWireLine != null) Destroy(tempWireLine);
        currentDraggingPoint = null;
    }

    private void ConnectWire(PLCWirePoint a, PLCWirePoint b, GameObject lineObj)
    {
        if (a == null || b == null || lineObj == null) return;

        a.connectedTo = b;
        b.connectedTo = a;
        a.wireLineObject = lineObj;
        b.wireLineObject = lineObj;

        UpdateLineVisualWorld(lineObj.transform as RectTransform, a.transform.position, b.transform.position);
    }

    public void DisconnectWire(PLCWirePoint a, PLCWirePoint b)
    {
        if (a.wireLineObject != null) Destroy(a.wireLineObject);

        a.connectedTo = null;
        a.wireLineObject = null;
        if (b != null)
        {
            b.connectedTo = null;
            b.wireLineObject = null;
        }
        CheckLogic();
    }

    // Fungsi Update Visual via World Position (Paling Stabil)
    private void UpdateLineVisualWorld(RectTransform lineRect, Vector3 startWorld, Vector3 endWorld)
    {
        if (lineRect == null) return;
        Transform parent = lineRect.parent;
        if (parent == null) return;

        // Konversi ke Lokal Parent
        Vector3 localStart = parent.InverseTransformPoint(startWorld);
        Vector3 localEnd = parent.InverseTransformPoint(endWorld);

        // Flatten Z (Anti-Langit)
        localStart.z = 0;
        localEnd.z = 0;

        UpdateLineVisualLocal(lineRect, localStart, localEnd);
    }

    // Logika Murni Lokal
    private void UpdateLineVisualLocal(RectTransform lineRect, Vector3 localStart, Vector3 localEnd)
    {
        Vector3 dir = localEnd - localStart;
        float dist = dir.magnitude;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        lineRect.sizeDelta = new Vector2(dist, lineRect.sizeDelta.y);
        lineRect.localRotation = Quaternion.Euler(0, 0, angle);
        lineRect.localPosition = localStart;
    }

    private void DisconnectAllWires()
    {
        foreach (var point in allWirePoints)
        {
            if (point.connectedTo != null)
            {
                if (point.wireLineObject != null) Destroy(point.wireLineObject);
                point.connectedTo.connectedTo = null;
                point.connectedTo = null;
            }
        }
    }

    public void ScrambleChaos()
    {
        if (!isSystemReady) return;

        foreach (var sw in switches)
        {
            if (sw.toggleUI != null) sw.toggleUI.isOn = (Random.value > 0.5f);
        }

        foreach (var rule in liquidRules)
        {
            if (rule.tankScript != null) rule.tankScript.ScrambleLevel();
        }

        DisconnectAllWires();

        foreach (var slot in slots)
        {
            if (slot.currentItem != null) slot.isBroken = true;
        }

        RefreshSlotUI();
        CheckLogic();
    }

    // ================= MASTER LOGIC CHECKER =================
    public void CheckLogic()
    {
        if (!isSystemReady) return;

        bool allCorrect = true;
        bool dangerDetected = false;

        // --- 1. UPDATE VISUAL FEEDBACK ---

        // A. Slots
        foreach (var slot in slots)
        {
            bool slotOk = !slot.isBroken;
            if (slot.requiredItem != null)
            {
                if (slot.currentItem == null || slot.currentItem != slot.requiredItem) slotOk = false;
            }
            if (slot.feedback != null) slot.feedback.SetError(!slotOk);
            if (!slotOk) allCorrect = false;
        }

        // B. Switches
        foreach (var sw in switches)
        {
            bool swOk = (sw.toggleUI.isOn == sw.requiredState);
            if (sw.feedback != null) sw.feedback.SetError(!swOk);
            if (!swOk) allCorrect = false;
        }

        // C. Liquid
        foreach (var rule in liquidRules)
        {
            if (rule.tankScript != null)
            {
                float diff = Mathf.Abs(rule.tankScript.currentLevel - rule.targetLevel);
                bool tankOk = diff <= rule.tolerance;
                rule.tankScript.SetErrorState(!tankOk);
                if (!tankOk)
                {
                    allCorrect = false;
                    dangerDetected = true;
                }
            }
        }

        // D. Wiring (UPDATED LOGIC)
        foreach (var pt in allWirePoints) if (pt != null) pt.SetErrorState(false);

        foreach (var rule in wireRules)
        {
            if (rule.pointA != null && rule.pointB != null)
            {
                bool currentlyConnected = (rule.pointA.connectedTo == rule.pointB);
                bool conditionMet = (currentlyConnected == rule.shouldBeConnected);

                if (!conditionMet)
                {
                    rule.pointA.SetErrorState(true);
                    rule.pointB.SetErrorState(true);
                    allCorrect = false;
                }
            }
        }

        // D.3. Strict Mode
        foreach (var pt in allWirePoints)
        {
            if (pt != null && pt.connectedTo != null)
            {
                bool isLegalConnection = false;
                foreach (var rule in wireRules)
                {
                    bool matchRule = (rule.pointA == pt && rule.pointB == pt.connectedTo) ||
                                     (rule.pointB == pt && rule.pointA == pt.connectedTo);

                    if (matchRule && rule.shouldBeConnected)
                    {
                        isLegalConnection = true;
                        break;
                    }
                }

                if (!isLegalConnection)
                {
                    pt.SetErrorState(true);
                    if (pt.connectedTo) pt.connectedTo.SetErrorState(true);
                    allCorrect = false;
                }
            }
        }


        // --- 2. CEK BAHAYA & LOGGING REALTIME ---
        bool mainPowerOn = (switches.Count > 0 && switches[0].toggleUI != null && switches[0].toggleUI.isOn);

        if (mainPowerOn)
        {
            if (explosiveItem != null && slots.Count > explosiveSlotIndex && explosiveSlotIndex >= 0)
            {
                if (slots[explosiveSlotIndex].currentItem == explosiveItem)
                {
                    dangerDetected = true;
                    if (Time.time > lastExplosionTime + 2.0f)
                    {
                        GlobalPuzzleManager.Instance.TriggerExplosion(plcID + " (Item Salah)");
                        lastExplosionTime = Time.time;
                    }
                }
            }

            if (dangerDetected)
            {
                if (Time.time > lastExplosionTime + 2.0f)
                {
                    GlobalPuzzleManager.Instance.TriggerExplosion(plcID + " (Komponen Kritis!)");
                    lastExplosionTime = Time.time;
                }
            }
        }

        // --- 3. HASIL AKHIR ---
        if (dangerDetected) allCorrect = false;

        if (GlobalPuzzleManager.Instance != null)
        {
            GlobalPuzzleManager.Instance.SetDistrictStatus(targetDistrict, allCorrect);
        }

        // --- 4. CHAOS TRIGGER ---
        if (allCorrect && isChaosTrigger && !hasTriggeredChaos)
        {
            hasTriggeredChaos = true;
            foreach (var targetPLC in chaosTargets)
            {
                if (targetPLC != null) targetPLC.ScrambleChaos();
            }
        }
    }
}