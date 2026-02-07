using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Starter : MonoBehaviour
{
    [SerializeField] private UnityEvent gameEnabler;
    private bool hasStarted = false;
    private void OnDisable()
    {
        if (hasStarted) return;
        gameEnabler?.Invoke();
        hasStarted = true;
    }
}
