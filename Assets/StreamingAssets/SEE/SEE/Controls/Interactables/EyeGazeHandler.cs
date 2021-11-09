using System.Collections;
using System.Reflection;
using Microsoft.MixedReality.Toolkit.Input;
using SEE.GO;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls
{
    /// <summary>
    /// Receives input events from the <see cref="BaseEyeFocusHandler"/> and passes it on to the
    /// <see cref="InteractableObject"/> component of its game objects. 
    /// </summary>
    public class EyeGazeHandler : BaseEyeFocusHandler
    {
        /// <summary>
        /// Seconds to wait before triggering the SetHover event on eye focus.
        /// </summary>
        private float eyeStareDelay;

        /// <summary>
        /// Interactable object which receives SetHover events.
        /// </summary>
        private InteractableObject interactable;

        /// <summary>
        /// If true, the "dehover" action should actually be called after the <see cref="eyeStareDelay"/> has passed.
        /// Used to avoid concurrency shenanigans.
        /// </summary>
        private bool shouldDehover = true;
        
        private void Awake()
        {
            PlayerSettings settings = PlayerSettings.GetPlayerSettings();
            if (settings.playerInputType != PlayerInputType.HoloLensPlayer || !settings.EyeGazeHover)
            {
                Destroyer.DestroyComponent(this);
                return;
            }
            
            // The only way to change the delay until OnEyeFocusDwell triggers is by reflection, which is
            // pretty ugly and error-prone. The only way to really fix this, however, would be to submit
            // a pull request to the MRTK upstream which adds a protected (instead of private) setter for this field.
            // Actually, a second way would be to use the same method as in OnEyeFocusStop().
            FieldInfo dwellField = typeof(BaseEyeFocusHandler)
                .GetField("timeToTriggerDwellInSec", BindingFlags.NonPublic | BindingFlags.Instance);
            if (dwellField != null)
            {
                eyeStareDelay = settings.EyeStareDelay;
                dwellField.SetValue(this, eyeStareDelay);
            }
            else
            {
                // If this error is triggered, an alternative approach as mentioned in the beginning of this
                // method should be implemented instead.
                Debug.LogError("Reflection failed: timeToTriggerDwellInSec attribute wasn't found "
                               + "(has probably been renamed).\n");
            }

            gameObject.TryGetComponentOrLog(out interactable);
        }

        protected override void OnEyeFocusDwell()
        {
            shouldDehover = false;
            interactable.SetHoverFlag(HoverFlag.World, true, true);
            base.OnEyeFocusDwell();
        }

        protected override void OnEyeFocusStop()
        {
            shouldDehover = true;
            StartCoroutine(StopFocusAfterDelay());
        }

        private IEnumerator StopFocusAfterDelay()
        {
            yield return new WaitForSecondsRealtime(eyeStareDelay*1.5f);
            if (shouldDehover)
            {
                interactable.SetHoverFlag(HoverFlag.World, false, true);
            }
        }
    }
}