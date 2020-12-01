using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.UI.BoundsControl;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArchitectureHololensController : MonoBehaviour
{
    GameObject AppBar;

    // Start is called before the first frame update
    void Start()
    {
        AppBar = GameObject.Find("AppBar");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnMouseOver()
    {
        BoundsControl boundsControl = gameObject.GetComponent<BoundsControl>();
        boundsControl.BoundsControlActivation = Microsoft.MixedReality.Toolkit.UI.BoundsControlTypes.BoundsControlActivationType.ActivateOnStart;

        if(AppBar != null && boundsControl != null) 
        {
            AppBar.GetComponent<AppBar>().Target = boundsControl;
        }
    }
}
