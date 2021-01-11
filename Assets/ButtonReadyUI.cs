using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SEE.Controls;

public class ButtonReadyUI : MonoBehaviour
{
    // Start is called before the first frame update

    /// <summary>
    /// The button on the adding-node-canvas, which is closing the canvas
    /// </summary>
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
        DesktopNewNodeAction.SetBool(false);
    }
}
