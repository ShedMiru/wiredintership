using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Activator : MonoBehaviour
{
    public UnityEvent onActivate;
    [SerializeField] private GameObject ui;

    private void OnEnable()
    {
        onActivate?.Invoke();
        if (ui != null)
            ui.SetActive(true);
    }


}
