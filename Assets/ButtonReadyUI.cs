using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SEE.Controls;

public class ButtonReadyUI : MonoBehaviour
{
    // Start is called before the first frame update

    public Button button;
    void Start()
    {
        button.onClick.AddListener(setBool);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void setBool()
    {
        DesktopNewNodeAction.setBool(false);
        Debug.Log("Button pressed");
    }
}
