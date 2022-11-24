using SEE.Controls;
using UnityEngine;

namespace SEE.Game.UI.PropertyDialog.HolisticMetrics
{
    /// <summary>
    /// Base class for most dialogs needed for the menu for the holistic metrics framework.
    /// </summary>
    internal abstract class HolisticMetricsDialog
    {
        /// <summary>
        /// The dialog GameObject.
        /// </summary>
        internal GameObject dialog;
        
        /// <summary>
        /// This method needs to be called when the dialog should be closed. It will close the dialog and reenable the
        /// keyboard shortcuts.
        /// </summary>
        internal void EnableKeyboardShortcuts()
        {
            // Destroy the dialog GameObject
            Object.Destroy(dialog);
            
            SEEInput.KeyboardShortcutsEnabled = true;
        }
    }
}