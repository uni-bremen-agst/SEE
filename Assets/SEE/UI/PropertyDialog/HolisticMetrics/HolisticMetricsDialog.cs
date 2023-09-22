using SEE.Controls;
using SEE.Utils;
using UnityEngine;

namespace SEE.UI.PropertyDialog.HolisticMetrics
{
    /// <summary>
    /// Base class for most dialogs needed for the menu for the holistic metrics framework.
    /// </summary>
    internal abstract class HolisticMetricsDialog
    {
        /// <summary>
        /// Whether this dialog has some complete user input that hasn't yet been fetched.
        /// </summary>
        protected bool GotInput;

        /// <summary>
        /// Whether this dialog was canceled.
        /// </summary>
        private bool wasCanceled;

        /// <summary>
        /// The property dialog.
        /// </summary>
        protected PropertyDialog PropertyDialog;

        /// <summary>
        /// The dialog GameObject.
        /// </summary>
        internal GameObject Dialog;

        /// <summary>
        /// Can be invoked to properly close the dialog.
        /// </summary>
        protected void Close()
        {
            Destroyer.Destroy(Dialog);
            SEEInput.KeyboardShortcutsEnabled = true;
        }

        /// <summary>
        /// Gets called when the dialog is canceled.
        /// </summary>
        protected void Cancel()
        {
            wasCanceled = true;
            Close();
        }

        /// <summary>
        /// Whether this dialog was canceled.
        /// </summary>
        /// <returns>Whether this dialog was canceled</returns>
        internal bool WasCanceled()
        {
            if (wasCanceled)
            {
                wasCanceled = false;
                return true;
            }

            return false;
        }
    }
}
