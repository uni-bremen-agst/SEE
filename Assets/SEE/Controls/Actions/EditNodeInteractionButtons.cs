using System;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// This component is attached to the prefab Resources/Prefabs/EditNode.prefab and 
    /// reacts to the user's selection of the OK and Cancel buttons. It forwards those
    /// events to <see cref="EditNodeCanvasAction"/>.
    /// 
    /// FIXME: This is a clone of NewNodeInteractionButtons.
    /// </summary>
    [Obsolete("This class is just a man in the middle. Likely, we will get rid of it.")]
    class EditNodeInteractionButtons : NodeInteractionButtons
    {
        /// <summary>
        /// Sets <see cref="EditNodeCanvasAction.EditNode"/> to true.
        /// This method is registered as a callback listening to the <see cref="OKButton"/>
        /// and is called when the OK button is pressed.
        /// </summary>
        protected override void OKButtonPressed()
        {
            // FIXME: EditNodeCanvasAction.EditNode is static. This will likely not work if
            // we have multiple reversible actions.
            EditNodeCanvasAction.EditNode = true;
        }

        /// <summary>
        /// Sets <see cref="EditNodeCanvasAction.Canceled"/> to true.
        /// This method is registered as a callback listening to the <see cref="CancelButton"/>
        /// and is called when the Cancel button is pressed.
        /// </summary>
        protected override void CancelButtonPressed()
        {
            // FIXME: EditNodeCanvasAction.Canceled is static. This will likely not work if
            // we have multiple reversible actions.
            EditNodeCanvasAction.Canceled = true;
        }
    }
}
