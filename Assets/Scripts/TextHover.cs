using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextHover : MonoBehaviour
{
    [SerializeField] private RectTransform textRect;
    [SerializeField] private float enterScale;
    [SerializeField] private float exitScale;
    [SerializeField] private GameObject warningText;
    [SerializeField] private GameObject warningText2;
    private int counter = 0;


    public void PointerEnter()
    {
        textRect.localScale = new Vector3(enterScale, enterScale, enterScale);
    }

    public void PointerExit()
    {
        textRect.localScale = new Vector3(exitScale, exitScale, exitScale);
    }

    public void ChangeWarning()
    {
        if (warningText.activeInHierarchy)
        {
            if (counter > 0)
            {
                warningText.SetActive(false);
                warningText2.SetActive(true);
                var feedback = warningText2.GetComponent<AutoErrorFeedback>();
                if (feedback == null) feedback = warningText2.AddComponent<AutoErrorFeedback>();
                feedback.SetError(true);
                counter = -1;
            }
            else if (counter != -1)
            {
                counter++;
            }
        }
    }
}
