using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangePanel : MonoBehaviour
{
    public GameObject FirstPanel;
    public GameObject SecondPanel;

    public void open()
    {
        if (FirstPanel != null && SecondPanel != null)
        {
            Animator firstAnimator = FirstPanel.GetComponent<Animator>();
            Animator secondAnimator = SecondPanel.GetComponent<Animator>();

            if (firstAnimator != null && secondAnimator!= null)
            {
                bool firstIsOpen = firstAnimator.GetBool("isOpen");
                bool secondIsOpen = secondAnimator.GetBool("isOpen");

                firstAnimator.SetBool("isOpen", !firstIsOpen);
                secondAnimator.SetBool("isOpen", !secondIsOpen);
            }
        }
    }
}
