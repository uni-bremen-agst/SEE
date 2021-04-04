using SEE.Controls.Actions;
using System;

namespace SEE.Controls
{
    /// <summary>
    /// This component is attached to the prefab Resources/Prefabs/AddNode.prefab and 
    /// reacts to the user's selection of the OK and Cancel buttons. It forwards those
    /// events to <see cref="AddingNodeCanvasAction"/>.
    /// 
    /// FIXME: This is a clone of EditNodeInteractionButtons.
    /// </summary>
    [Obsolete("This class is just a man in the middle. Likely, we will get rid of it.")]
    class NewNodeInteractionButtons : NodeInteractionButtons
    {
        /// <summary>
        /// Sets <see cref="AddingNodeCanvasAction.AddNode"/> to true.
        /// This method is registered as a callback listening to the <see cref="OKButton"/>
        /// and is called when the OK button is pressed.
        /// </summary>
        protected override void OKButtonPressed()
        {
            // FIXME: AddingNodeCanvasAction.AddNode is static. This will likely not work if
            // we have multiple reversible actions.
            AddingNodeCanvasAction.AddNode = true;
        }

        /// <summary>
        /// Sets <see cref="AddingNodeCanvasAction.Canceled"/> to true.
        /// This method is registered as a callback listening to the <see cref="CancelButton"/>
        /// and is called when the Cancel button is pressed.
        /// </summary>
        protected override void CancelButtonPressed()
        {
            // FIXME: AddingNodeCanvasAction.Canceled is static. This will likely not work if
            // we have multiple reversible actions.
            AddingNodeCanvasAction.Canceled = true;
        }
    }
}
