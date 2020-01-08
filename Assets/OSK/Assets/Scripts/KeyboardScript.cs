using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class KeyboardScript : MonoBehaviour
{

    public InputField TextField;
    public GameObject RusLayoutSml, RusLayoutBig, EngLayoutSml, EngLayoutBig, SymbLayout;

    public void alphabetFunction(string alphabet)
    {


        TextField.text=TextField.text + alphabet;

    }

    public void BackSpace()
    {

        if(TextField.text.Length>0) TextField.text= TextField.text.Remove(TextField.text.Length-1);

    }

    public void CloseAllLayouts()
    {

        RusLayoutSml.SetActive(false);
        RusLayoutBig.SetActive(false);
        EngLayoutSml.SetActive(false);
        EngLayoutBig.SetActive(false);
        SymbLayout.SetActive(false);

    }

    public void ShowLayout(GameObject SetLayout)
    {

        CloseAllLayouts();
        SetLayout.SetActive(true);

    }

    // Added this function to original source code to cover all "enter" events in once.
    public void OnEnter()
    {
        TextField.GetComponent<SearchForObject>().OnEnter();
    }

}
