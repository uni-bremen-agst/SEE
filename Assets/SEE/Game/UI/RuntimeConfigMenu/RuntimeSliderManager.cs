using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SEE.Game.UI.RuntimeConfigMenu
{
    /// <summary>
    /// Handles the editing event of the slider.
    ///
    /// Triggers <see cref="OnEndEdit"/> when the mouse is released
    /// to prevent listening to the change on each frame.
    /// </summary>
    public class RuntimeSliderManager : MonoBehaviour, IPointerUpHandler
    {
        /// <summary>
        /// Calls <see cref="OnEndEdit"/> when the mouse is released.
        /// </summary>
        /// <param name="eventData"></param>
        public void OnPointerUp(PointerEventData eventData)
        {
            OnEndEdit?.Invoke();
        }

        /// <summary>
        /// Triggers when the pointer is released.
        /// </summary>
        public event Action OnEndEdit;
    }
}
