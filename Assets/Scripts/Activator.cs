using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Activator : MonoBehaviour
{
    public UnityEvent onActivate;
    public UnityEvent panelA;
    private bool activated = false;
    [SerializeField] private GameObject ui;

    private void OnEnable()
    {
        if (!activated)
        {
            panelA?.Invoke();
            activated = true;
        }
        onActivate?.Invoke();
        if (ui != null)
            ui.SetActive(true);
    }


}
