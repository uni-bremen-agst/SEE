using UnityEngine;
using UnityEngine.UI;

namespace SEE.Controls
{
    /// <summary>
    /// This component is the common abstract super class of components 
    /// reacting to the OK and Cancel buttons for dialogs editing/adding
    /// nodes. Concrete subclasses will be added to prefabs defining
    /// the actual buttons.
    /// </summary>
    public abstract class NodeInteractionButtons : MonoBehaviour
    {
        /// <summary>
        /// The button on the canvas that is finalizing the interaction.
        /// This button is connected in the prefab. It must be public.
        /// </summary>
        public Button OKButton;

        /// <summary>
        /// The button on the canvas that is canceling the interaction.
        /// This button is connected in the prefab. It must be public.
        /// </summary>
        public Button CancelButton;

        /// <summary>
        /// Adds listeners to the OK and Cancel buttons to react when the buttons are pressed.
        /// </summary>   
        private void Start()
        {
            OKButton?.onClick?.AddListener(OKButtonPressed);
            CancelButton?.onClick?.AddListener(CancelButtonPressed);
        }

        /// <summary>
        /// This method is registered as a callback listening to the <see cref="OKButton"/>
        /// and is called when the OK button is pressed.
        /// </summary>
        protected abstract void OKButtonPressed();

        /// <summary>
        /// This method is registered as a callback listening to the <see cref="CancelButton"/>
        /// and is called when the Cancel button is pressed.
        /// </summary>
        protected abstract void CancelButtonPressed();
    }
}