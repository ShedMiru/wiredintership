using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuideActivator : MonoBehaviour
{
    [SerializeField] private GameObject ui;

    private void OnEnable()
    {
        if (ui != null)
            ui.SetActive(true);
    }
}
