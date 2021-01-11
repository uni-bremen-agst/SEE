using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.UI.BoundsControl;
using UnityEngine;

public class ArchitectureHololensController : MonoBehaviour
{
    private GameObject AppBar;

    private void Start()
    {
        AppBar = GameObject.Find("AppBar");
    }

    private void OnMouseOver()
    {
        BoundsControl boundsControl = gameObject.GetComponent<BoundsControl>();
        boundsControl.BoundsControlActivation = Microsoft.MixedReality.Toolkit.UI.BoundsControlTypes.BoundsControlActivationType.ActivateOnStart;

        if (AppBar != null && boundsControl != null) 
        {
            AppBar.GetComponent<AppBar>().Target = boundsControl;
        }
    }
}
