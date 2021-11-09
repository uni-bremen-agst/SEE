using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.UI.BoundsControl;
using SEE.GO;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls
{
    /// <summary>
    /// This class allows code cities' AppBars to be activated when the mouse hovers over it.
    /// </summary>
    public class ArchitectureHololensController : MonoBehaviour
    {
        /// <summary>
        /// The AppBar of the attached code city.
        /// </summary>
        private GameObject AppBar;

        private void Start()
        {
            if (PlayerSettings.GetInputType() != PlayerInputType.HoloLensPlayer)
            {
                // AppBar shouldn't exist outside of HoloLens platform
                Destroyer.DestroyComponent(this);
                return;
            }
            AppBar = GameObject.Find("AppBar");
        }

        /// <summary>
        /// When the mouse hovers above a code city, its AppBar is displayed.
        /// </summary>
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
}
