using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject ui;
    [SerializeField] private GameObject promptUI;

    public void Interact(GameObject interactor)
    {
        if (ui != null)
            ui.SetActive(true);

        //stop movement while UI open
        interactor.GetComponent<CharacterControl>().enabled = false;
    }

    public void ShowPrompt(bool state)
    {
        if (promptUI != null)
            promptUI.SetActive(state);
    }
}
