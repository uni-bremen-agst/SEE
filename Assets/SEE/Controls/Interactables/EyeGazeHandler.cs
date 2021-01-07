using System.Reflection;
using Microsoft.MixedReality.Toolkit.Input;
using SEE.Game;
using SEE.GO;
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
        
        private void Awake()
        {
            // TODO The only way to change the delay until OnEyeFocusDwell triggers is by reflection, which is
            // pretty ugly and error-prone. The only way to really fix this, however, would be to submit
            // a pull request to the MRTK upstream which adds a protected (instead of private) setter for this field.
            FieldInfo dwellField = typeof(BaseEyeFocusHandler)
                .GetField("timeToTriggerDwellInSec", BindingFlags.NonPublic | BindingFlags.Instance);
            if (dwellField != null)
            {
                if (SceneQueries.GetCodeCity(gameObject.transform).gameObject.TryGetComponentOrLog(out AbstractSEECity city))
                {
                    eyeStareDelay = city.EyeStareDelay;
                    dwellField.SetValue(this, eyeStareDelay);
                }
            }
            else
            {
                Debug.LogError("Reflection failed: timeToTriggerDwellInSec attribute wasn't found "
                               + "(has probably been renamed).\n");
            }

            gameObject.TryGetComponentOrLog(out interactable);
        }

        protected override void OnEyeFocusDwell()
        {
            interactable.SetHover(true, true);
        }

        protected override void OnEyeFocusStop()
        {
            //TODO: Don't stop when gazing on label
            interactable.SetHover(false, true);
        }
    }
}