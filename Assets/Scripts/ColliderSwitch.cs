using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColliderSwitch : MonoBehaviour
{
    public GameObject walls;
    public GameObject plcA;
    public GameObject plcB;
    public GameObject plcC;
    public GameObject plcD;
    public GameObject desk;

    void OnEnable()
    {
        SetAllColliders(false);
    }

    public void SetAllColliders(bool state)
    {
        // --- 1. Colliders from wall children ---
        BoxCollider2D[] wallColliders = walls.GetComponentsInChildren<BoxCollider2D>();

        foreach (BoxCollider2D col in wallColliders)
        {
            col.enabled = state;
        }

        // --- 2. Colliders from individual objects ---
        Toggle(plcA, state);
        Toggle(plcB, state);
        Toggle(plcC, state);
        Toggle(plcD, state);
        Toggle(desk, state);
    }

    private void Toggle(GameObject obj, bool state)
    {
        if (obj == null) return;

        BoxCollider2D col = obj.GetComponent<BoxCollider2D>();

        if (col != null)
            col.enabled = state;
    }
}
