using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TabButtonSwitcher : MonoBehaviour, IPointerClickHandler
{
    public GameObject Tab;

    private string debug = "";
    private void OnGUI()
    {
        /*GUI.TextField(new Rect(0, 0, 300, 50), debug);*/
        
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        /*
         * TODO: Maybe get ContentView through attribute?
         * Advantages:
         * + TabButtonSwitcher could be used for other stuff
         * + hierarchy changes don't corrupt the code
         */
        Transform contentView = transform.parent.parent.parent.Find("ContentView");
        // disable all (other) content panels
        foreach (Transform childTransform in contentView)
        {
            childTransform.gameObject.SetActive(false);
        }
            
        // open connected panel
        Tab.SetActive(true);
    }
}