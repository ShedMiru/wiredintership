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
            }
            else
            {
                counter++;
            }
        }
    }
}
